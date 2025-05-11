namespace TechnicalAssignment.BlockchainCollector.API.Contracts.Responses;

public record BlockchainHistoryResponse
(
    IReadOnlyList<Blockchain> Items,
    uint Offset,
    uint Limit,
    bool HasNext
);

