namespace TechnicalAssignment.BlockchainCollector.Application.Pagination;

public record PagedItems<TItem>
(
    IReadOnlyList<TItem> Items,
    uint Offset,
    uint Limit,
    bool HasNext
);