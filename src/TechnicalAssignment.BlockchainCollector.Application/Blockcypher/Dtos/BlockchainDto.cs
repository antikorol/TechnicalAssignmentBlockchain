using System.Text.Json.Serialization;

namespace TechnicalAssignment.BlockchainCollector.Application.Blockcypher.Dtos;

public sealed class BlockchainDto
{
    public string Name { get; init; } = null!;
    public int Height { get; init; }
    public string Hash { get; init; } = null!;
    public string Time { get; init; } = null!;

    [JsonPropertyName("latest_url")]
    public string LatestUrl { get; init; } = null!;

    [JsonPropertyName("previous_hash")]
    public string PreviousHash { get; init; } = null!;

    [JsonPropertyName("previous_url")]
    public string PreviousUrl { get; init; } = null!;

    [JsonPropertyName("peer_count")]
    public int PeerCount { get; init; }

    [JsonPropertyName("unconfirmed_count")]
    public int UnconfirmedCount { get; init; }

    [JsonPropertyName("high_fee_per_kb")]
    public int HighFeePerKb { get; init; }

    [JsonPropertyName("medium_fee_per_kb")]
    public int MediumFeePerKb { get; init; }

    [JsonPropertyName("low_fee_per_kb")]
    public int LowFeePerKb { get; init; }

    [JsonPropertyName("last_fork_height")]
    public int LastForkHeight { get; init; }

    [JsonPropertyName("last_fork_hash")]
    public string LastForkHash { get; init; } = null!;
}