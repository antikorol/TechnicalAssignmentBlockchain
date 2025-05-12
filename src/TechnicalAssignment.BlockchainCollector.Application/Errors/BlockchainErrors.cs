using System.Net;
using TechnicalAssignment.BlockchainCollector.Domain.Errors;

namespace TechnicalAssignment.BlockchainCollector.Application.Errors;

internal static class BlockchainErrors
{
    public static DomainError RateLimitExceeded() =>
        DomainError.Problem(
            "Blockchain.RateLimitExceeded",
            "Rate Limit Exceeded");

    public static DomainError ApiError(HttpStatusCode statusCode) =>
        DomainError.Problem(
            "Blockchain.UnexpectedResponse",
            $"External request failed with status code {statusCode}");

    public static DomainError NotFound(string coin, string chain) =>
       DomainError.NotFound(
            "Blockchain.NotFound",
            $"The coin {coin}.{chain}' was not found");
}
