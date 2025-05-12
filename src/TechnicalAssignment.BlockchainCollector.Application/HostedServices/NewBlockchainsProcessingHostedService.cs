using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;
using TechnicalAssignment.BlockchainCollector.Domain.Events;

namespace GameFlinker.BK.Payments.Domain.HostedServices;

public sealed class NewBlockchainsProcessingHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IInternalServiceBus<BlockReceived> _newBlocksServiceBus;
    private readonly ILogger<NewBlockchainsProcessingHostedService> _logger;

    public NewBlockchainsProcessingHostedService(
        IServiceProvider serviceProvider,
        IInternalServiceBus<BlockReceived> newBlocksServiceBus,
        ILogger<NewBlockchainsProcessingHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _newBlocksServiceBus = newBlocksServiceBus;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Factory.StartNew(() => ConsumeBlockReceivedEventsAsync(stoppingToken), TaskCreationOptions.LongRunning);
    }

    private async Task ConsumeBlockReceivedEventsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await foreach (var newBlockEvent in _newBlocksServiceBus.ReadAllAsync(cancellationToken))
                {
                    using var scope = _serviceProvider.CreateScope();

                    var blockchainService = scope.ServiceProvider.GetRequiredService<IBlockchainService>();

                    await blockchainService.AppendToHistoryAsync(newBlockEvent.Block, cancellationToken);
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Processing block received events was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process block received event");
            }
        }
    }
}
