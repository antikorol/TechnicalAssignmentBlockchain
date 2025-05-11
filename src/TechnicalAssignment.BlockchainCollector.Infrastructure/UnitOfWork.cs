using Microsoft.Extensions.Logging;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;

namespace TechnicalAssignment.BlockchainCollector.Infrastructure;

internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly BlockchainDbContext _blockchainDbContext;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(
        BlockchainDbContext blockchainDbContext,
        ILogger<UnitOfWork> logger)
    {
        _blockchainDbContext = blockchainDbContext;
        _logger = logger;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Attempting to save changes to the database. Context: {ContextName}", nameof(BlockchainDbContext));

        try
        {
            await _blockchainDbContext.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Changes were successfully saved to the database. Context: {ContextName}", nameof(BlockchainDbContext));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save changes to the database. Context: {ContextName}", nameof(BlockchainDbContext));

            throw;
        }
    }
}
