using AccessRequestSystem.Worker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AccessRequestSystem.Worker;

/// <summary>
/// Background worker service executing periodic cleanup of expired access grants.
/// </summary>
public class Worker : BackgroundService
{
    private readonly IExpiredGrantRevoker _revoker;
    private readonly IConfiguration _configuration;
    private readonly ILogger<Worker> _logger;

    public Worker(
        IExpiredGrantRevoker revoker,
        IConfiguration configuration,
        ILogger<Worker> logger)
    {
        _revoker = revoker;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalSeconds = _configuration.GetValue<int>("Worker:IntervalSeconds", 30);
        _logger.LogInformation(
            "Access Request Expiration Worker started. Checking every {Seconds} seconds.",
            intervalSeconds);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(intervalSeconds));

        // Initial cycle immediately upon startup
        await ProcessCycleAsync(stoppingToken);

        // Subsequent periodic cycles
        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessCycleAsync(stoppingToken);
        }

        _logger.LogInformation("Access Request Expiration Worker is stopping gracefully.");
    }

    private async Task ProcessCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogDebug("Starting grant expiration scan cycle at: {Time:u}", DateTimeOffset.UtcNow);
            await _revoker.RevokeExpiredGrantsAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during host shutdown
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled error occurred during the grant revocation cycle.");
        }
    }
}
