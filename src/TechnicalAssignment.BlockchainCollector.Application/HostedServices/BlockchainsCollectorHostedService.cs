using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using TechnicalAssignment.BlockchainCollector.Application.Configurations;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;

namespace GameFlinker.BK.Payments.Domain.HostedServices;

public sealed class BlockchainsCollectorHostedService : BackgroundService
{
    private readonly BlockchainOptions _blockchainOptions;
    private readonly IServiceProvider _serviceProvider;
    private readonly IBlockcypherRateLimitTracker _rateLimitTracker;
    private readonly ILogger<BlockchainsCollectorHostedService> _logger;

    public BlockchainsCollectorHostedService(
        IServiceProvider serviceProvider,
        IOptions<BlockchainOptions> blockchainOptions,
        IBlockcypherRateLimitTracker rateLimitTracker,
        ILogger<BlockchainsCollectorHostedService> logger)
    {
        _blockchainOptions = blockchainOptions.Value;
        _serviceProvider = serviceProvider;
        _rateLimitTracker = rateLimitTracker;
        _logger = logger;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Factory.StartNew(() => CollectBlockchainsAsync(stoppingToken), TaskCreationOptions.LongRunning);
    }

    private async Task CollectBlockchainsAsync(CancellationToken cancellationToken)
    {
        if (_blockchainOptions.Coins.Count == 0)
        {
            _logger.LogWarning("Invalid blockchain configuration detected. Collector will be stopped");

            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                foreach (var coin in _blockchainOptions.Coins)
                {
                    var isAcuredToken = false;

                    do
                    {
                        var sw = Stopwatch.StartNew();

                        isAcuredToken = await _rateLimitTracker.AcquireTokenAsync(cancellationToken);

                        sw.Stop();

                        if (!isAcuredToken)
                        {
                            _logger.LogDebug("Token not available for request within {Elapsed}", sw.Elapsed);
                            
                            var cooldownTime = await _rateLimitTracker.GetCooldownTimeAsync(cancellationToken);

                            if (cooldownTime > TimeSpan.Zero)
                                await Task.Delay(cooldownTime, cancellationToken);
                            
                            continue;
                        }

                        using var scope = _serviceProvider.CreateScope();

                        var blockchainService = scope.ServiceProvider.GetRequiredService<IBlockchainService>();

                        var result = await blockchainService.GetLastBlockAsync(coin.Code, coin.Chain, cancellationToken);

                        if (result.IsFailed)
                        {
                            if (result.HasError<TechnicalAssignment.BlockchainCollector.Domain.Errors.Error>(out var errors))
                            {
                                var error = errors.First();
                                _logger.LogError("Failed to collect last blockchain for the {Coin}.{Chain}. Reason: {ErrorCode} ({Message})", coin.Code, coin.Chain, error.Code, error.Message);
                            }
                            else
                            {
                                _logger.LogError("Failed to collect last blockchain. Unknown error");
                            }
                        }
                    }
                    while (isAcuredToken && !cancellationToken.IsCancellationRequested);
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogInformation(ex, "Collecting blockchains was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect blockchain");
            }
        }
    }
}
