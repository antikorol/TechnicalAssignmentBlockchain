using HealthChecks.UI.Client;
using Serilog;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TechnicalAssignment.BlockchainCollector.API.Extensions;
using TechnicalAssignment.BlockchainCollector.API.Middlewares;
using TechnicalAssignment.BlockchainCollector.Application.Extensions;
using TechnicalAssignment.BlockchainCollector.Infrastructure.Extensions;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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

builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddHealthChecks()
    .AddCheck("liveness", () => HealthCheckResult.Healthy())
    .AddNpgSql(builder.Configuration["Postgres:ConnectionString"]!, name: "blockchain-db")
    .AddRedis(builder.Configuration["Redis:ConnectionString"]!, "redis-rate-limits")
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

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration)
    .AddPresentation(builder.Configuration);

var app = builder.Build();

await app.Services.InitialiseDatabaseAsync();

app.UseMiddleware<ExceptionHandlerMiddleware>();

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.UseSerilogRequestLogging();

app.MapEndpoints();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecks("/liveness", new HealthCheckOptions
{
    Predicate = r => r.Name.Contains("liveness"),
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();
