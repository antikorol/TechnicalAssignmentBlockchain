using TechnicalAssignment.BlockchainCollector.Application.Pagination;
using TechnicalAssignment.BlockchainCollector.Domain.Entities;

namespace TechnicalAssignment.BlockchainCollector.Application.Interfaces;

public interface IBlockchainRepository
{
    Task AddAsync(Blockchain blockchain, CancellationToken cancellationToken);
    Task<Blockchain?> FindByHashAsync(string hash, CancellationToken cancellationToken);
    Task<PagedItems<Blockchain>> LoadHistoryAsync(string name, uint offset, uint limit, CancellationToken cancellationToken);
}
