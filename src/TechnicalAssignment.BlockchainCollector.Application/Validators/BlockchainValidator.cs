using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;
using TechnicalAssignment.BlockchainCollector.Application.Configurations;
using TechnicalAssignment.BlockchainCollector.Application.Errors;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;

namespace TechnicalAssignment.BlockchainCollector.Application.Validators;

internal sealed class BlockchainValidator : IBlockchainValidator
{
    private readonly IReadOnlyDictionary<string, IReadOnlySet<string>> _supportedCoinsChainMap;
    private readonly ILogger<BlockchainValidator> _logger;

    public BlockchainValidator(
        IOptions<BlockchainOptions> blockchainOptions,
        ILogger<BlockchainValidator> logger)
    {
        _supportedCoinsChainMap = blockchainOptions.Value.Coins
            .GroupBy(c => c.Code)
            .ToImmutableDictionary(g => g.Key, g => (IReadOnlySet<string>)g.Select(c => c.Chain).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);

        _logger = logger;
    }

    public Result Validate(string coin, string chain)
    {
        if (_supportedCoinsChainMap.Count == 0)
        {
            _logger.LogError("Invalid blockchain configuration detected: missing Coins");

            return Result.Fail(ValidationErrors.InvalidConfiguration());
        }

        if (!_supportedCoinsChainMap.TryGetValue(coin, out var chains))
        {
            return Result.Fail(ValidationErrors.CoinNotSupported(coin));
        }

        if (!chains.Contains(chain))
        {
            return Result.Fail(ValidationErrors.ChainNotSupported(chain));
        }

        return Result.Ok();
    }
}
