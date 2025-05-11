using FluentResults;
using TechnicalAssignment.BlockchainCollector.Application.Pagination;
using TechnicalAssignment.BlockchainCollector.Domain.Entities;

namespace TechnicalAssignment.BlockchainCollector.Application.Interfaces;

public interface IBlockchainService
{
    Task<Result<Blockchain>> GetLastBlockAsync(string coin, string chain, CancellationToken cancellationToken);
    Task AppendToHistoryAsync(Blockchain block, CancellationToken cancellationToken);
    Task<Result<PagedItems<Blockchain>>> LoadHistoryAsync(string coin, string chain, uint offset, uint limit, CancellationToken cancellationToken);
}
