namespace AccessRequestSystem.Worker.Services;

public interface IExpiredGrantRevoker
{
    /// <summary>
    /// Identifies all expired grants and revokes them in parallel.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    /// <returns>Number of grants successfully transitioned to Expired.</returns>
    Task<int> RevokeExpiredGrantsAsync(CancellationToken cancellationToken);
}
