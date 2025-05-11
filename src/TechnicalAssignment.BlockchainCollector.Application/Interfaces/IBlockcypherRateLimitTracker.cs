namespace TechnicalAssignment.BlockchainCollector.Application.Interfaces;

public interface IBlockcypherRateLimitTracker
{
    Task<bool> AcquireTokenAsync(CancellationToken cancellationToken);

    Task<TimeSpan> GetCooldownTimeAsync(CancellationToken cancellationToken);
}
