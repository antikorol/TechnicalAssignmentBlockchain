using Microsoft.EntityFrameworkCore;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;
using TechnicalAssignment.BlockchainCollector.Application.Pagination;
using TechnicalAssignment.BlockchainCollector.Domain.Entities;

namespace TechnicalAssignment.BlockchainCollector.Infrastructure.Repositories;

internal sealed class BlockchainRepository : IBlockchainRepository
{
    private readonly BlockchainDbContext _blockchainDbContext;

    public BlockchainRepository(
        BlockchainDbContext blockchainDbContext)
    {
        _blockchainDbContext = blockchainDbContext;
    }

    public Task AddAsync(Blockchain blockchain, CancellationToken cancellationToken) =>
        _blockchainDbContext.Blockchains
            .AddAsync(blockchain, cancellationToken)
            .AsTask();

    public Task<Blockchain?> FindByHashAsync(string hash, CancellationToken cancellationToken) =>
        _blockchainDbContext.Blockchains
            .FirstOrDefaultAsync(b => b.Hash == hash);

    public async Task<PagedItems<Blockchain>> LoadHistoryAsync(string name, uint offset, uint limit, CancellationToken cancellationToken)
    {
        var items = await _blockchainDbContext.Blockchains
            .AsNoTracking()
            .Where(b => b.Name.ToLower() == name.ToLower())
            .OrderByDescending(b => b.CreatedAt)
            .Skip((int)offset)
            .Take((int)limit + 1)
            .ToListAsync();

        var hasNext = items.Count > limit;

        if (hasNext)
            items.RemoveAt(items.Count - 1);

        return new PagedItems<Blockchain>(items.AsReadOnly(), offset, limit, hasNext);
    }
}
