using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using System.Net;
using System.Net.Http.Json;
using TechnicalAssignment.BlockchainCollector.API.Contracts.Responses;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher.Dtos;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace TechnicalAssignment.BlockchainCollector.API.FunctionalTests;

public class BlockchainCollectorApiFunctionalTests : IAsyncLifetime
{
    private readonly AutoMocker _mocker;
    private readonly Fixture _fixture;
    private readonly WebApplicationFactory<Program> _applicationFactory;

    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithDatabase("blockchain-db")
        .WithUsername("postgres")
        .WithPassword("postgres-pwd")
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7.2")
        .Build();

    public BlockchainCollectorApiFunctionalTests()
    {
        _mocker = new AutoMocker();
        _fixture = new Fixture();

        _mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(true);
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
          .Setup(s => s.Content)
          .Returns(default(BlockchainDto));
        _mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mocker.Get<IApiResponse<BlockchainDto>>());

        _applicationFactory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    var overrides = new Dictionary<string, string>
                    {
                        { "Postgres:ConnectionString", _postgresContainer.GetConnectionString() },
                        { "Redis:ConnectionString", _redisContainer.GetConnectionString() }
                    };

                    configBuilder.AddInMemoryCollection(overrides!);
                });

                builder.ConfigureServices(services =>
                {
                    RemoveRegistration(services, typeof(IBlockchainSdk));
                    services.AddSingleton<IBlockchainSdk>(_mocker.Get<IBlockchainSdk>());
                });
            });
    }

    [Fact]
    public async Task GetLastBlockchain_CoinAndChainSupported_ReturnsResult()
    {
        var dto = _fixture.Create<BlockchainDto>();
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(true);
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
          .Setup(s => s.Content)
          .Returns(dto);
        _mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync("btc", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mocker.Get<IApiResponse<BlockchainDto>>());
        using var client = _applicationFactory.CreateClient();

        var response = await client.GetAsync("api/public/btc/main/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var blockchain = await response.Content.ReadFromJsonAsync<BlockchainResponse>();
        blockchain.ShouldNotBeNull();
        blockchain.Hash.ShouldBe(dto.Hash);
    }

    [Fact]
    public async Task GetLastBlockchain_CoinIsNotSupported_ReturnsBadRequest()
    {
        var invalidCoin = _fixture.Create<string>();
        using var client = _applicationFactory.CreateClient();

        var response = await client.GetAsync($"api/public/{invalidCoin}/main/");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var badRequest = await response.Content.ReadFromJsonAsync<BadRequestResponse>();
        badRequest.ShouldNotBeNull();
        badRequest.Code.ShouldBe("Validation.CoinNotSupported");
    }

    [Fact]
    public async Task GetLastBlockchain_RateLimitExceeded_ReturnsBadRequest()
    {
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
           .Setup(s => s.StatusCode)
           .Returns(HttpStatusCode.TooManyRequests);
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(false);
        _mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync("btc", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mocker.Get<IApiResponse<BlockchainDto>>());
        using var client = _applicationFactory.CreateClient();

        var response = await client.GetAsync("api/public/btc/main/");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var badRequest = await response.Content.ReadFromJsonAsync<BadRequestResponse>();
        badRequest.ShouldNotBeNull();
        badRequest.Code.ShouldBe("Blockchain.RateLimitExceeded");
    }

    [Fact]
    public async Task GetBlockchainHistory_CoinAndChainSupported_ReturnsResult()
    {
        var dto = _fixture.Create<BlockchainDto>();
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
            .Setup(s => s.IsSuccessStatusCode)
            .Returns(true);
        _mocker.GetMock<IApiResponse<BlockchainDto>>()
          .Setup(s => s.Content)
          .Returns(dto);
        _mocker.GetMock<IBlockchainSdk>()
            .Setup(s => s.GetBlockchainAsync("btc", "main", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_mocker.Get<IApiResponse<BlockchainDto>>());
        using var client = _applicationFactory.CreateClient();

        var response = await client.GetAsync("api/public/btc/main/history?offset=0&limit=1");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var blockchain = await response.Content.ReadFromJsonAsync<BlockchainHistoryResponse>();
        blockchain.ShouldNotBeNull();

    }

    [Fact]
    public async Task GetBlockchainHistory_CoinIsNotSupported_ReturnsBadRequest()
    {
        var invalidCoin = _fixture.Create<string>();
        using var client = _applicationFactory.CreateClient();

        var response = await client.GetAsync($"api/public/{invalidCoin}/main/?offset=0&limit=1");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var badRequest = await response.Content.ReadFromJsonAsync<BadRequestResponse>();
        badRequest.ShouldNotBeNull();
        badRequest.Code.ShouldBe("Validation.CoinNotSupported");
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.StopAsync();
        await _redisContainer.StopAsync();
        _applicationFactory.Dispose();
    }

    private static void RemoveRegistration(IServiceCollection services, Type type)
    {
        var descriptors = services
            .Where(d => d.ServiceType == type)
            .ToArray();

        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }
    }
}