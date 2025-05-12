using TechnicalAssignment.BlockchainCollector.Domain.Errors;

namespace TechnicalAssignment.BlockchainCollector.Application.Errors;

internal static class ValidationErrors
{
    public static DomainError InvalidConfiguration() =>
        DomainError.Problem(
            "Validation.InvalidConfiguration",
            "Validation config is invalid");

    public static DomainError CoinNotSupported(string coinCode) =>
        DomainError.ValidationFailed(
            "Validation.CoinNotSupported",
            $"The coin '{coinCode}' is not supported");

    public static DomainError ChainNotSupported(string chain) =>
        DomainError.ValidationFailed(
            "Validation.ChainNotSupported",
            $"The chain '{chain}' is not supported");
}
