using TechnicalAssignment.BlockchainCollector.Domain.Errors;

namespace TechnicalAssignment.BlockchainCollector.Application.Errors;

internal static class ValidationErrors
{
    public static Error InvalidConfiguration() =>
        Error.Problem(
            "Validation.InvalidConfiguration",
            "Validation config is invalid");

    public static Error CoinNotSupported(string coinCode) =>
        Error.ValidationFailed(
            "Validation.CoinNotSupported",
            $"The coin '{coinCode}' is not supported");

    public static Error ChainNotSupported(string chain) =>
        Error.ValidationFailed(
            "Validation.ChainNotSupported",
            $"The chain '{chain}' is not supported");
}
