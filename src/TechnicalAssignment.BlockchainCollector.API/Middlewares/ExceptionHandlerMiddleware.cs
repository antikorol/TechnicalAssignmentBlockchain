using System.Diagnostics.CodeAnalysis;

namespace TechnicalAssignment.BlockchainCollector.API.Middlewares;

[ExcludeFromCodeCoverage]
public class ExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public ExceptionHandlerMiddleware(RequestDelegate next, ILogger<ExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            _logger.LogError(ex, "An unhandled exception occurred during request processing");
        }
    }
}
