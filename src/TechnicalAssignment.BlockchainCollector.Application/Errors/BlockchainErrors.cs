using System.Net;
using TechnicalAssignment.BlockchainCollector.Domain.Errors;

namespace TechnicalAssignment.BlockchainCollector.Application.Validators;

internal static class BlockchainErrors
{
    public static Error RateLimitExceeded() =>
        Error.Problem(
            "Blockchain.RateLimitExceeded",
            "Rate Limit Exceeded");

    public static Error ApiError(HttpStatusCode statusCode) =>
        Error.Problem(
            "Blockchain.UnexpectedResponse",
            $"External request failed with status code {statusCode}");

    public static Error NotFound(string coin, string chain) =>
       Error.NotFound(
            "Blockchain.NotFound",
            $"The coin {coin}.{chain}' was not found");
}
