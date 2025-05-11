namespace TechnicalAssignment.BlockchainCollector.Application.Configurations;

public sealed class BlockchainOptions
{
    public IReadOnlyList<Coin> Coins { get; init; } = Array.Empty<Coin>();
}
