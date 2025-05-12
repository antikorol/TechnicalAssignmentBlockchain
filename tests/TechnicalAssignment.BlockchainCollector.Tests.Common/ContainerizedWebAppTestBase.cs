
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

namespace TechnicalAssignment.BlockchainCollector.Tests.Common;

public abstract class ContainerizedWebAppTestBase : BaseFixture, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
       .WithImage("postgres:17")
       .WithDatabase("blockchain-db")
       .WithUsername("postgres")
       .WithPassword("postgres-pwd")
       .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithImage("redis:7.2")
        .Build();

    protected WebApplicationFactory<TEntryPoint> CreateApplicationFactory<TEntryPoint>()
        where TEntryPoint : class => new WebApplicationFactory<TEntryPoint>()
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
                    ConfigureServices(services);
                });
            });

    protected abstract void ConfigureServices(IServiceCollection services);
    protected abstract void OnDispose();

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.StopAsync();
        await _redisContainer.StopAsync();

        OnDispose();
    }

    protected static void RemoveRegistration(IServiceCollection services, Type type)
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
