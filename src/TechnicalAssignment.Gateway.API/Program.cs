using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddYamlFile("appsettings.yml", optional: true, reloadOnChange: true);
builder.Configuration.AddYamlFile($"appsettings.{builder.Environment.EnvironmentName}.yml", optional: true);

builder.Host
    .UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resurce => resurce.AddService(builder.Configuration["Application"]!))
    .WithTracing(tracing =>
    {
        tracing
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();

        tracing.AddOtlpExporter(options =>
        {
            builder.Configuration.Bind("OpenTelemetry:Otlp", options);
        });
    });

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services
    .AddHealthChecks()
    .AddCheck("liveness", () => HealthCheckResult.Healthy())
    .Services
    .AddCors(options =>
    {
        options.AddDefaultPolicy(builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .WithMethods("GET");
        });
    });

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors();

app.UseSerilogRequestLogging();

app.MapReverseProxy();

app.MapHealthChecks("/liveness", new HealthCheckOptions
{
    Predicate = r => r.Name.Contains("liveness"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
