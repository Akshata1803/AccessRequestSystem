using AccessRequestSystem.Api.DTOs.Audit;
using AccessRequestSystem.Api.DTOs.Common;
using AccessRequestSystem.Data;
using AccessRequestSystem.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AccessRequestSystem.Api.Services;

/// <summary>
/// Records and queries immutable audit trail entries.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;

    public AuditLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(int? actorUserId, string action, string entityType, int entityId, string details)
    {
        var log = new AuditLog
        {
            Timestamp = DateTime.UtcNow,
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResultDto<AuditLogDto>> GetAuditLogsAsync(
        string? action,
        int? actorUserId,
        int page,
        int pageSize)
    {
        // Enforce valid paging bounds
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.AuditLogs
            .Include(l => l.ActorUser)
            .AsNoTracking()
            .AsQueryable();

        // Optional filter: Action (case-insensitive)
        if (!string.IsNullOrWhiteSpace(action))
        {
            var trimmedAction = action.Trim().ToLower();
            query = query.Where(l => l.Action.ToLower() == trimmedAction);
        }

        // Optional filter: ActorUserId
        if (actorUserId.HasValue)
        {
            query = query.Where(l => l.ActorUserId == actorUserId.Value);
        }

        var totalCount = await query.CountAsync();

        // Order newest first and apply paging
        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogDto
            {
                Id = l.Id,
                Timestamp = l.Timestamp,
                ActorUserId = l.ActorUserId,
                ActorUsername = l.ActorUser != null ? l.ActorUser.Username : null,
                Action = l.Action,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                Details = l.Details
            })
            .ToListAsync();

        return new PagedResultDto<AuditLogDto>(items, totalCount, page, pageSize);
    }
}
