using FluentResults;
using Microsoft.Extensions.Logging;
using System.Net;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;
using TechnicalAssignment.BlockchainCollector.Domain.Entities;
using TechnicalAssignment.BlockchainCollector.Application.Validators;
using TechnicalAssignment.BlockchainCollector.Application.Mappings;
using MediatR;
using TechnicalAssignment.BlockchainCollector.Domain.Events;
using System.Xml.Linq;
using TechnicalAssignment.BlockchainCollector.Application.Pagination;

namespace TechnicalAssignment.BlockchainCollector.Application.Services;

internal sealed class BlockchainService : IBlockchainService
{
    private readonly TimeProvider _timeProvider;
    private readonly IBlockchainRepository _blockchainRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBlockchainValidator _blockchainValidator;
    private readonly IBlockchainSdk _blockchainSdk;
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;
    private readonly ILogger<BlockchainService> _logger;

    public BlockchainService(
        TimeProvider timeProvider,
        IBlockchainRepository blockchainRepository,
        IUnitOfWork unitOfWork,
        IBlockchainValidator blockchainValidator,
        IBlockchainSdk blockchainSdk,
        IMediator mediator,
        IMapper mapper,
        ILogger<BlockchainService> logger)
    {
        _timeProvider = timeProvider;
        _blockchainRepository = blockchainRepository;
        _unitOfWork = unitOfWork;
        _blockchainValidator = blockchainValidator;
        _blockchainSdk = blockchainSdk;
        _mediator = mediator;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<Blockchain>> GetLastBlockAsync(string coin, string chain, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Querying blockchain for the {Coin}.{Chain}", coin, chain);

        var validationResult = _blockchainValidator.Validate(coin, chain);

        if (validationResult.IsFailed)
            return validationResult;

        var response = await _blockchainSdk.GetBlockchainAsync(coin, chain, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Rate limit exceeded for the {Coin}.{Chain}", coin, chain);

                return Result.Fail(BlockchainErrors.RateLimitExceeded());
            }
            else
            {
                _logger.LogWarning("External request for {Coin}.{Chain} failed with status code {StatusCode}", coin, chain, response.StatusCode);

                return Result.Fail(BlockchainErrors.ApiError(response.StatusCode));
            }
        }

        var block = response.Content;

        if (block is null)
            return Result.Fail(BlockchainErrors.NotFound(coin, chain));

        _logger.LogInformation("Received block for the {Name}. Hash: {Hash}; Height: {Height}", block.Name, block.Hash, block.Height);

        var blockchain = _mapper.Map(block, _timeProvider.GetUtcNow().UtcDateTime);

        await _mediator.Publish(new BlockReceived(blockchain), cancellationToken);

        return blockchain;
    }

    public async Task AppendToHistoryAsync(Blockchain block, CancellationToken cancellationToken)
    {
        var existedBlock = await _blockchainRepository.FindByHashAsync(block.Hash, cancellationToken);

        if (existedBlock is not null)
        {
            _logger.LogInformation("Skipping block addition: the block {Name} with hash {Hash} and height {Height} already exists",
                block.Name, block.Hash, block.Height);

            return;
        }

        await _blockchainRepository.AddAsync(block, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("The block {Name} with hash {Hash} and height {Height} was append to history",
               block.Name, block.Hash, block.Height);
    }

    public async Task<Result<PagedItems<Blockchain>>> LoadHistoryAsync(string coin, string chain, uint offset, uint limit, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading blockchain history for the {Coin}.{Chain}", coin, chain);

        var validationResult = _blockchainValidator.Validate(coin, chain);

        if (validationResult.IsFailed)
            return validationResult;

        var name = "{coin}.{chain}";

        return await _blockchainRepository.LoadHistoryAsync(name, offset, limit, cancellationToken);
    }
}
