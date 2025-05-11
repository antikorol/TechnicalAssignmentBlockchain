namespace TechnicalAssignment.BlockchainCollector.Infrastructure.RateLimitTrackers;

public sealed class BlockcypherRateLimitOptions
{
    public RateLimitRule[] Rules { get; init; } = Array.Empty<RateLimitRule>();
}
