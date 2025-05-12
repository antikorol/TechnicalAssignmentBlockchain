using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System.Net;
using System.Net.Http.Json;
using TechnicalAssignment.BlockchainCollector.API.Contracts.Responses;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher.Dtos;
using TechnicalAssignment.BlockchainCollector.Tests.Common;

namespace TechnicalAssignment.BlockchainCollector.API.FunctionalTests;

public sealed class BlockchainCollectorApiFunctionalTests : ContainerizedWebAppTestBase
{
    private readonly WebApplicationFactory<Program> _applicationFactory;

    public BlockchainCollectorApiFunctionalTests()
    {
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(true);
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
          .Setup(s => s.Content)
          .Returns(default(BlockchainDto));
        Mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mocker.Get<IApiResponse<BlockchainDto>>());

        _applicationFactory = CreateApplicationFactory<Program>();
    }

    [Fact]
    public async Task GetLastBlockchain_CoinAndChainSupported_ReturnsResult()
    {
        // Arrange
        var dto = Fixture.Create<BlockchainDto>();

        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(true);
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
          .Setup(s => s.Content)
          .Returns(dto);
        Mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync("btc", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mocker.Get<IApiResponse<BlockchainDto>>());

        using var client = _applicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync("api/public/btc/main/");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var blockchain = await response.Content.ReadFromJsonAsync<BlockchainResponse>();
        blockchain.ShouldNotBeNull();
        blockchain.Hash.ShouldBe(dto.Hash);
    }

    [Fact]
    public async Task GetLastBlockchain_CoinIsNotSupported_ReturnsBadRequest()
    {
        // Arrange
        var invalidCoin = Fixture.Create<string>();
        using var client = _applicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"api/public/{invalidCoin}/main/");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var badRequest = await response.Content.ReadFromJsonAsync<BadRequestResponse>();
        badRequest.ShouldNotBeNull();
        badRequest.Code.ShouldBe("Validation.CoinNotSupported");
    }

    [Fact]
    public async Task GetLastBlockchain_RateLimitExceeded_ReturnsBadRequest()
    {
        // Arrange
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
           .Setup(s => s.StatusCode)
           .Returns(HttpStatusCode.TooManyRequests);
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(false);
        Mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync("btc", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mocker.Get<IApiResponse<BlockchainDto>>());

        using var client = _applicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync("api/public/btc/main/");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var badRequest = await response.Content.ReadFromJsonAsync<BadRequestResponse>();
        badRequest.ShouldNotBeNull();
        badRequest.Code.ShouldBe("Blockchain.RateLimitExceeded");
    }

    [Fact]
    public async Task GetBlockchainHistory_CoinAndChainSupported_ReturnsResult()
    {
        // Arrange
        var dto = Fixture.Create<BlockchainDto>();

        Mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(true);
        Mocker.GetMock<IApiResponse<BlockchainDto>>()
          .Setup(s => s.Content)
          .Returns(dto);
        Mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync("btc", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mocker.Get<IApiResponse<BlockchainDto>>());

        using var client = _applicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync("api/public/btc/main/history?offset=0&limit=1");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var blockchain = await response.Content.ReadFromJsonAsync<BlockchainHistoryResponse>();
        blockchain.ShouldNotBeNull();

    }

    [Fact]
    public async Task GetBlockchainHistory_CoinIsNotSupported_ReturnsBadRequest()
    {
        // Arrange
        var invalidCoin = Fixture.Create<string>();
        using var client = _applicationFactory.CreateClient();

        // Act
        var response = await client.GetAsync($"api/public/{invalidCoin}/main/?offset=0&limit=1");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var badRequest = await response.Content.ReadFromJsonAsync<BadRequestResponse>();
        badRequest.ShouldNotBeNull();
        badRequest.Code.ShouldBe("Validation.CoinNotSupported");
    }

    protected override void ConfigureServices(IServiceCollection services)
    {
        RemoveRegistration(services, typeof(IBlockchainSdk));
        services.AddSingleton<IBlockchainSdk>(Mocker.Get<IBlockchainSdk>());
    }

    protected override void OnDispose()
    {
        _applicationFactory.Dispose();
    }
}