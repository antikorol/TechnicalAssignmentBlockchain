using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Refit;
using StackExchange.Redis.Extensions.Core.Abstractions;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Core.Implementations;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;
using TechnicalAssignment.BlockchainCollector.Infrastructure.RateLimitTrackers;
using TechnicalAssignment.BlockchainCollector.Infrastructure.Repositories;
using TechnicalAssignment.BlockchainCollector.Infrastructure.ServiceBus;

namespace TechnicalAssignment.BlockchainCollector.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .Configure<BlockcypherRateLimitOptions>(configuration.GetSection("Blockcypher:RateLimits"))
            .AddDbContext<BlockchainDbContext>((sp, options) =>
            {
                options.UseNpgsql(configuration["Postgres:ConnectionString"]!);
                options.UseLoggerFactory(sp.GetRequiredService<ILoggerFactory>());
            })
           .AddScoped<IBlockchainRepository, BlockchainRepository>()
           .AddScoped<IUnitOfWork, UnitOfWork>()
           .AddSingleton(typeof(IInternalServiceBus<>), typeof(InternalServiceBus<>))
           .AddSingleton<IBlockcypherRateLimitTracker, BlockcypherRateLimitTracker>()
           .AddRefitClient<IBlockchainSdk>()
           .ConfigureHttpClient(c => c.BaseAddress = new Uri(configuration["Blockcypher:Host"]!))
           .Services
           .AddSingleton<IRedisConnectionPoolManager>(
               sp =>
               {
                   return new RedisConnectionPoolManager(
                       new RedisConfiguration { ConnectionString = configuration["Redis:ConnectionString"]! },
                       sp.GetRequiredService<ILogger<RedisConnectionPoolManager>>());
               });

        return services;
    }
}
