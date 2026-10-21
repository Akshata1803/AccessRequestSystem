using AccessRequestSystem.Data;
using AccessRequestSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AccessRequestSystem.Worker.Services;

/// <summary>
/// Domain service responsible for identifying and revoking expired access grants.
/// Separated from BackgroundService so it can be independently unit tested.
/// </summary>
public class ExpiredGrantRevoker : IExpiredGrantRevoker
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ExpiredGrantRevoker> _logger;

    public ExpiredGrantRevoker(
        IDbContextFactory<AppDbContext> contextFactory,
        IConfiguration configuration,
        ILogger<ExpiredGrantRevoker> logger)
    {
        _contextFactory = contextFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<int> RevokeExpiredGrantsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // 1. Fetch IDs of all approved requests that have surpassed their expiration time.
        // Using an initial short-lived DbContext just to query candidate IDs.
        List<int> expiredRequestIds;
        await using (var queryContext = await _contextFactory.CreateDbContextAsync(cancellationToken))
        {
            expiredRequestIds = await queryContext.AccessRequests
                .Where(r => r.Status == AccessRequestStatus.Approved && r.ExpiresAt <= now)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);
        }

        if (expiredRequestIds.Count == 0)
        {
            return 0;
        }

        _logger.LogInformation("Found {Count} expired access grant(s) to revoke.", expiredRequestIds.Count);

        var maxDegreeOfParallelism = _configuration.GetValue<int>("Worker:MaxDegreeOfParallelism", 4);
        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism,
            CancellationToken = cancellationToken
        };

        var revokedCount = 0;

        // 2. Revoke expired grants in parallel.
        //
        // THREAD-SAFETY NOTE FOR INTERVIEWS:
        // EF Core's DbContext is NOT thread-safe. A single DbContext instance cannot execute
        // multiple concurrent operations. Therefore, each parallel task/iteration MUST create
        // and dispose its own independent DbContext instance via IDbContextFactory.
        await Parallel.ForEachAsync(expiredRequestIds, parallelOptions, async (requestId, ct) =>
        {
            try
            {
                await using var itemContext = await _contextFactory.CreateDbContextAsync(ct);

                // Use an explicit database transaction if supported (relational providers like SQL Server)
                // to ensure atomicity and prevent race conditions.
                var isRelational = itemContext.Database.IsRelational();
                await using var transaction = isRelational
                    ? await itemContext.Database.BeginTransactionAsync(ct)
                    : null;

                var request = await itemContext.AccessRequests
                    .FirstOrDefaultAsync(r => r.Id == requestId && r.Status == AccessRequestStatus.Approved, ct);

                if (request == null)
                {
                    // Concurrency safe: request was already processed or revoked by another worker instance
                    if (transaction != null)
                    {
                        await transaction.RollbackAsync(ct);
                    }
                    return;
                }

                // Update request state
                request.Status = AccessRequestStatus.Expired;
                request.GrantTokenHash = null; // Clear hash on expiration for privacy and hygiene

                // Create system audit trail entry (ActorUserId = null indicates automated system action)
                var audit = new AuditLog
                {
                    Timestamp = DateTime.UtcNow,
                    ActorUserId = null,
                    Action = "GrantExpired",
                    EntityType = nameof(AccessRequest),
                    EntityId = request.Id,
                    Details = $"Access grant expired automatically. Revoked by background worker."
                };

                itemContext.AuditLogs.Add(audit);

                await itemContext.SaveChangesAsync(ct);
                if (transaction != null)
                {
                    await transaction.CommitAsync(ct);
                }

                Interlocked.Increment(ref revokedCount);
                _logger.LogInformation("Successfully revoked expired grant for AccessRequest ID {RequestId}.", requestId);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                // Graceful cancellation requested during shutdown
                _logger.LogWarning("Revocation cancelled for AccessRequest ID {RequestId} due to service shutdown.", requestId);
            }
            catch (Exception ex)
            {
                // Catch per-item so a single row failure does not stop the entire batch
                _logger.LogError(ex, "Failed to revoke expired AccessRequest ID {RequestId}.", requestId);
            }
        });

        _logger.LogInformation("Batch completed. Successfully revoked {Count} expired grant(s).", revokedCount);
        return revokedCount;
    }
}
