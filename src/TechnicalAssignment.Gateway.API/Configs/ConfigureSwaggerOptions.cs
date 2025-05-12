using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using TechnicalAssignment.Gateway.API.Extensions;
using Yarp.ReverseProxy.Swagger;

namespace TechnicalAssignment.Gateway.API.Configs;

public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly ReverseProxyDocumentFilterConfig _reverseProxyDocumentFilterConfig;

    public ConfigureSwaggerOptions(IOptionsMonitor<ReverseProxyDocumentFilterConfig> reverseProxyDocumentFilterConfigOptions)
    {
        _reverseProxyDocumentFilterConfig = reverseProxyDocumentFilterConfigOptions.CurrentValue;
    }

    public void Configure(SwaggerGenOptions options)
    {
        var filterDescriptors = new List<FilterDescriptor>();

        options.ConfigureSwaggerDocs(_reverseProxyDocumentFilterConfig);

        filterDescriptors.Add(new FilterDescriptor
        {
            Type = typeof(ReverseProxyDocumentFilter),
            Arguments = []
        });

        options.DocumentFilterDescriptors = filterDescriptors;
    }
}
