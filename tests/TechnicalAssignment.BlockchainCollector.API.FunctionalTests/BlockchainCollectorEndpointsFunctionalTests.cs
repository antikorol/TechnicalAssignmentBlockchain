using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Refit;
using System.Net;
using System.Net.Http.Json;
using TechnicalAssignment.BlockchainCollector.API.Contracts.Responses;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher.Dtos;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace TechnicalAssignment.BlockchainCollector.API.FunctionalTests;

public class BlockchainCollectorEndpointsFunctionalTests : IAsyncLifetime
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

    public BlockchainCollectorEndpointsFunctionalTests()
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
                    Remove(services, typeof(IBlockchainSdk));

                    services.AddSingleton<IBlockchainSdk>(_mocker.Get<IBlockchainSdk>());
                });
            });
    }

    [Fact]
    public async Task GetLastBlockChain_ReturnsResult()
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

    private static void Remove(IServiceCollection services, Type type)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == type);

        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
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
}