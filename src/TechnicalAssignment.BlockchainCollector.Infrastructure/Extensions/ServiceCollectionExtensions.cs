using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;
using TechnicalAssignment.BlockchainCollector.Application.Blockcypher;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;
using TechnicalAssignment.BlockchainCollector.Infrastructure.ServiceBus;

namespace TechnicalAssignment.BlockchainCollector.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddSingleton(typeof(IInternalServiceBus<>), typeof(InternalServiceBus<>))
            .AddRefitClient<IBlockchainSdk>()
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(configuration["Blockcypher:Host"]!));
        
        return services;
    }
}
