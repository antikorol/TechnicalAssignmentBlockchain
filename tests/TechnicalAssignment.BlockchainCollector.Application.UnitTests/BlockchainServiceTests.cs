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
using TechnicalAssignment.BlockchainCollector.Tests.Common;

namespace TechnicalAssignment.BlockchainCollector.Application.UnitTests;

public class BlockchainServiceTests : BaseFixture
{
    private readonly Lazy<BlockchainService> _subjectLazy;
    private BlockchainService Subject => _subjectLazy.Value;

    public BlockchainServiceTests()
    {
        _subjectLazy = new Lazy<BlockchainService>(() => GetSubject<BlockchainService>());

        Mocker.Use<IMapper>(new MapperlyMapper());
    }

    [Fact]
    public async Task GetLastBlockAsync_CoinValidationFails_ReturnsFailedResult()
    {
        // Arrange
        var coin = Fixture.Create<string>();
        var chain = Fixture.Create<string>();
        var error = ValidationErrors.CoinNotSupported(coin);
        
        Mocker.GetMock<IBlockchainValidator>()
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
        var coin = Fixture.Create<string>();
        var chain = Fixture.Create<string>();
        var error = ValidationErrors.CoinNotSupported(chain);

        Mocker.GetMock<IBlockchainValidator>()
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
        var coin = Fixture.Create<string>();
        var chain = Fixture.Create<string>();
        
        Mocker.GetMock<IBlockchainValidator>()
            .Setup(v => v.Validate(coin, chain))
            .Returns(Result.Ok());
        
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.StatusCode)
            .Returns(HttpStatusCode.TooManyRequests);
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(false);
        Mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync(coin, chain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mocker.Get<IApiResponse<BlockchainDto>>());

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
        var coin = Fixture.Create<string>();
        var chain = Fixture.Create<string>();
        
        Mocker.GetMock<IBlockchainValidator>()
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Result.Ok());
        
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.StatusCode)
            .Returns(HttpStatusCode.BadGateway);
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(false);
        Mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync(coin, chain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mocker.Get<IApiResponse<BlockchainDto>>());

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
        var coin = Fixture.Create<string>();
        var chain = Fixture.Create<string>();
        
        Mocker.GetMock<IBlockchainValidator>()
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Result.Ok());
        
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.StatusCode)
            .Returns(HttpStatusCode.OK);
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(true);
        Mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync(coin, chain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mocker.Get<IApiResponse<BlockchainDto>>());

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
        var coin = Fixture.Create<string>();
        var chain = Fixture.Create<string>();
        var dto = Fixture.Create<BlockchainDto>();
        
        Mocker.GetMock<IBlockchainValidator>()
            .Setup(v => v.Validate(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Result.Ok());
        
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.StatusCode)
            .Returns(HttpStatusCode.OK);
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(true);
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
          .Setup(s => s.Content)
          .Returns(dto);
        Mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync(coin, chain, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mocker.Get<IApiResponse<BlockchainDto>>());

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
        var block = Fixture.Create<Blockchain>();
        Mocker.GetMock<IBlockchainRepository>()
            .Setup(r => r.FindByHashAsync(block.Hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(block);

        // Act
        await Subject.AppendToHistoryAsync(block, CancellationToken.None);

        // Assert
        Mocker.GetMock<IBlockchainRepository>()
            .Verify(r => r.AddAsync(It.IsAny<Blockchain>(), It.IsAny<CancellationToken>()), Times.Never);
        Mocker.GetMock<IUnitOfWork>()
            .Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AppendToHistoryAsync_NewBlock_AddsAndSaves()
    {
        // Arrange
        var block = Fixture.Create<Blockchain>();
        Mocker.GetMock<IBlockchainRepository>()
            .Setup(r => r.FindByHashAsync(block.Hash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(default(Blockchain));

        // Act
        await Subject.AppendToHistoryAsync(block, CancellationToken.None);

        // Assert
        Mocker.GetMock<IBlockchainRepository>()
            .Verify(r => r.AddAsync(It.IsAny<Blockchain>(), It.IsAny<CancellationToken>()), Times.Once);
        Mocker.GetMock<IUnitOfWork>()
            .Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoadHistoryAsync_ValidationFails_ReturnsFailedResult()
    {
        // Arrange
        var coin = Fixture.Create<string>();
        var chain = Fixture.Create<string>();

        var error = ValidationErrors.CoinNotSupported(coin);
        Mocker.GetMock<IBlockchainValidator>()
               .Setup(v => v.Validate(coin, chain))
               .Returns(Result.Fail(error));

        // Act
        var result = await Subject.LoadHistoryAsync(coin, chain, Fixture.Create<uint>(), Fixture.Create<uint>(), CancellationToken.None);

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
        var coin = Fixture.Create<string>();
        var chain = Fixture.Create<string>();
        var offset = Fixture.Create<uint>();
        var limit = Fixture.Create<uint>();
        var block = Fixture.Create<Blockchain>();
        var paged = new PagedItems<Blockchain>(
            new[] { block },
            offset,
            limit,
            true
        );

        Mocker.GetMock<IBlockchainValidator>()
            .Setup(v => v.Validate(coin, chain))
            .Returns(Result.Ok());

        Mocker.GetMock<IBlockchainRepository>()
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