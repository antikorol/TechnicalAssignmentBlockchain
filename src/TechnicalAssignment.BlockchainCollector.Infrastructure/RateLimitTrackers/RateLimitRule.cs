namespace TechnicalAssignment.BlockchainCollector.Infrastructure.RateLimitTrackers;

public sealed class RateLimitRule
{
    public string Name { get; init; } = null!;
    public Period Period { get; init; }
    public int Requests { get; init; }
}

public enum Period
{
    Hour,
    Second
}