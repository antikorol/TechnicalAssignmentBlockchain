namespace TechnicalAssignment.BlockchainCollector.API.Contracts;

public class Blockchain
{
    public string Name { get; init; } = null!;
    public int Height { get; init; }
    public string Hash { get; init; } = null!;
    public string Time { get; init; } = null!;
    public string LatestUrl { get; init; } = null!;
    public string PreviousHash { get; init; } = null!;
    public string PreviousUrl { get; init; } = null!;
    public int PeerCount { get; init; }
    public int UnconfirmedCount { get; init; }
    public int HighFeePerKb { get; init; }
    public int MediumFeePerKb { get; init; }
    public int LowFeePerKb { get; init; }
    public int LastForkHeight { get; init; }
    public string LastForkHash { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
};
