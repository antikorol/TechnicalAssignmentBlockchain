using TechnicalAssignment.BlockchainCollector.API.Mappings;

namespace TechnicalAssignment.BlockchainCollector.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddSingleton<IMapper, MapperlyMapper>()
            .AddSingleton(TimeProvider.System);
    }
}
