using FluentResults;
using Refit;
using System.Net;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher.Dtos;
using TechnicalAssignment.BlockchainCollector.Application.Errors;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;
using TechnicalAssignment.BlockchainCollector.Application.Mappings;
using TechnicalAssignment.BlockchainCollector.Application.Pagination;
using TechnicalAssignment.BlockchainCollector.Application.Services;
using TechnicalAssignment.BlockchainCollector.Domain.Entities;
using TechnicalAssignment.BlockchainCollector.Domain.Errors;

namespace TechnicalAssignment.BlockchainCollector.Application.UnitTests;

public class BlockchainServiceTests
{
    private readonly AutoMocker _mocker;
    private readonly Fixture _fixture;
    private readonly Lazy<BlockchainService> _subjectLazy;

    private BlockchainService Subject => _subjectLazy.Value;

    public BlockchainServiceTests()
    {
        _mocker = new AutoMocker();
        _fixture = new Fixture();
        _subjectLazy = new Lazy<BlockchainService>(() => _mocker.CreateInstance<BlockchainService>());

        _mocker.Use<IMapper>(new MapperlyMapper());
    }

    [Fact]
    public async Task GetLastBlockAsync_CoinValidationFails_ReturnsFailedResult()
    {
        // Arrange
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();
        var error = ValidationErrors.CoinNotSupported(coin);
        
        _mocker.GetMock<IBlockchainValidator>()
               .Setup(v => v.Validate(coin, chain))
               .Returns(Result.Fail(error));

        // Act
        var result = await Subject.GetLastBlockAsync(coin, chain, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();

        var resultError = result.Errors[0].ShouldBeOfType<DomainError>();
        resultError.Message.ShouldBe(error.Message);
        resultError.Code.ShouldBe(error.Code);
    }

    [Fact]
    public async Task GetLastBlockAsync_ChainValidationFails_ReturnsFailedResult()
    {
        // Arrange
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();
        var error = ValidationErrors.CoinNotSupported(chain);

        _mocker.GetMock<IBlockchainValidator>()
               .Setup(v => v.Validate(coin, chain))
               .Returns(Result.Fail(error));

        // Act
        var result = await Subject.GetLastBlockAsync(coin, chain, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
        
        var resultError = result.Errors[0].ShouldBeOfType<DomainError>();
        resultError.Message.ShouldBe(error.Message);
        resultError.Code.ShouldBe(error.Code);
    }

    [Fact]
    public async Task GetLastBlockAsync_TooManyRequests_ReturnsRateLimitError()
    {
        // Arrange
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();
        
        _mocker.GetMock<IBlockchainValidator>()
            .Setup(v => v.Validate(coin, chain))
            .Returns(Result.Ok());
        
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.StatusCode)
            .Returns(HttpStatusCode.TooManyRequests);
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(false);
        _mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync(coin, chain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mocker.Get<IApiResponse<BlockchainDto>>());

        // Act
        var result = await Subject.GetLastBlockAsync(coin, chain, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();

        var resultError = result.Errors[0].ShouldBeOfType<DomainError>();
        resultError.Code.ShouldBe("Blockchain.RateLimitExceeded");
        resultError.Message.ShouldBe("Rate Limit Exceeded");
    }

    [Fact]
    public async Task GetLastBlockAsync_BadResponse_ReturnsApiError()
    {
        // Arrange
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();
        
        _mocker.GetMock<IBlockchainValidator>()
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Result.Ok());
        
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.StatusCode)
            .Returns(HttpStatusCode.BadGateway);
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(false);
        _mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync(coin, chain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mocker.Get<IApiResponse<BlockchainDto>>());

        // Act
        var result = await Subject.GetLastBlockAsync(coin, chain, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();

        var resultError = result.Errors[0].ShouldBeOfType<DomainError>();
        resultError.Code.ShouldBe("Blockchain.UnexpectedResponse");
        resultError.Message.ShouldBe($"External request failed with status code {HttpStatusCode.BadGateway}");
    }

    [Fact]
    public async Task GetLastBlockAsync_ContentIsNull_ReturnsNotFound()
    {
        // Arrange
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();
        
        _mocker.GetMock<IBlockchainValidator>()
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Result.Ok());
        
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.StatusCode)
            .Returns(HttpStatusCode.OK);
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(true);
        _mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync(coin, chain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mocker.Get<IApiResponse<BlockchainDto>>());

        // Act
        var result = await Subject.GetLastBlockAsync(coin, chain, CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
        var resultError = result.Errors[0].ShouldBeOfType<DomainError>();
        resultError.Code.ShouldBe("Blockchain.NotFound");
        resultError.Message.ShouldBe($"The coin {coin}.{chain}' was not found");
    }

    [Fact]
    public async Task GetLastBlockAsync_Successful_ReturnsBlockchain()
    {
        // Arrange
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();
        var dto = _fixture.Create<BlockchainDto>();
        
        _mocker.GetMock<IBlockchainValidator>()
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Result.Ok());
        
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.StatusCode)
            .Returns(HttpStatusCode.OK);
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(true);
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
          .Setup(s => s.Content)
          .Returns(dto);
        _mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync(coin, chain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mocker.Get<IApiResponse<BlockchainDto>>());

        // Act
        var result = await Subject.GetLastBlockAsync(coin, chain, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.Name.ShouldBe(dto.Name);
        result.Value.Height.ShouldBe(dto.Height);
        result.Value.Hash.ShouldBe(dto.Hash);
    }

    [Fact]
    public async Task AppendToHistoryAsync_ExistingBlock_SkipsAdd()
    {
        // Arrange
        var block = _fixture.Create<Blockchain>();
        _mocker.GetMock<IBlockchainRepository>()
            .Setup(r => r.FindByHashAsync(block.Hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(block);

        // Act
        await Subject.AppendToHistoryAsync(block, CancellationToken.None);

        // Assert
        _mocker.GetMock<IBlockchainRepository>()
            .Verify(r => r.AddAsync(It.IsAny<Blockchain>(), It.IsAny<CancellationToken>()), Times.Never);
        _mocker.GetMock<IUnitOfWork>()
            .Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AppendToHistoryAsync_NewBlock_AddsAndSaves()
    {
        // Arrange
        var block = _fixture.Create<Blockchain>();
        _mocker.GetMock<IBlockchainRepository>()
            .Setup(r => r.FindByHashAsync(block.Hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(Blockchain));

        // Act
        await Subject.AppendToHistoryAsync(block, CancellationToken.None);

        // Assert
        _mocker.GetMock<IBlockchainRepository>()
            .Verify(r => r.AddAsync(It.IsAny<Blockchain>(), It.IsAny<CancellationToken>()), Times.Once);
        _mocker.GetMock<IUnitOfWork>()
            .Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadHistoryAsync_ValidationFails_ReturnsFailedResult()
    {
        // Arrange
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();

        var error = ValidationErrors.CoinNotSupported(coin);
        _mocker.GetMock<IBlockchainValidator>()
               .Setup(v => v.Validate(coin, chain))
               .Returns(Result.Fail(error));

        // Act
        var result = await Subject.LoadHistoryAsync(coin, chain, _fixture.Create<uint>(), _fixture.Create<uint>(), CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();

        var resultError = result.Errors[0].ShouldBeOfType<DomainError>();
        resultError.Message.ShouldBe(error.Message);
        resultError.Code.ShouldBe(error.Code);
    }

    [Fact]
    public async Task LoadHistoryAsync_Success_ReturnsPagedItems()
    {
        // Arrange
        var coin = _fixture.Create<string>();
        var chain = _fixture.Create<string>();
        var offset = _fixture.Create<uint>();
        var limit = _fixture.Create<uint>();
        var block = _fixture.Create<Blockchain>();
        var paged = new PagedItems<Blockchain>(
            new[] { block },
            offset,
            limit,
            true
        );

        _mocker.GetMock<IBlockchainValidator>()
            .Setup(v => v.Validate(coin, chain))
            .Returns(Result.Ok());

        _mocker.GetMock<IBlockchainRepository>()
               .Setup(r => r.LoadHistoryAsync($"{coin}.{chain}", offset, limit, It.IsAny<CancellationToken>()))
               .ReturnsAsync(paged);

        // Act
        var result = await Subject.LoadHistoryAsync(coin, chain, offset, limit, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var item = result.Value.Items.ShouldHaveSingleItem();
        item.Hash.ShouldBe(block.Hash);
    }
}