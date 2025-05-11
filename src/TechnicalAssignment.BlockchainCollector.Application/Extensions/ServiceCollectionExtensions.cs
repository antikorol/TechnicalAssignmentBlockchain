using GameFlinker.BK.Payments.Domain.HostedServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using TechnicalAssignment.BlockchainCollector.Application.Configurations;
using TechnicalAssignment.BlockchainCollector.Application.Interfaces;
using TechnicalAssignment.BlockchainCollector.Application.Mappings;
using TechnicalAssignment.BlockchainCollector.Application.Services;
using TechnicalAssignment.BlockchainCollector.Application.Validators;
using TechnicalAssignment.BlockchainCollector.Domain.Events;

namespace TechnicalAssignment.BlockchainCollector.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        return services
           .Configure<BlockchainOptions>(configuration.GetSection("Blockchain"))
           .AddSingleton<IBlockchainValidator, BlockchainValidator>()
           .AddSingleton<IMapper, MapperlyMapper>()
           .AddScoped<IBlockchainService, BlockchainService>()
           .AddHostedService<NewBlocksProcessingHostedService>()
           .AddHostedService<BlocksCollectorHostedService>()
           .AddMediatR(
                cfg =>
                {
                    cfg.RegisterServicesFromAssemblies(
                        Assembly.GetExecutingAssembly(),
                        typeof(BlockReceived).Assembly);
                });
    }
}
