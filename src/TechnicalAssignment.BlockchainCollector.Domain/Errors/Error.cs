using FluentResults;

namespace TechnicalAssignment.BlockchainCollector.Domain.Errors;

public class Error : IError
{
    public ErrorType Type { get; set; }
    public string Code { get; }
    public string Message { get; }
    public List<IError> Reasons { get; } = new List<IError>();
    public Dictionary<string, object> Metadata { get; } = new Dictionary<string, object>();

    public Error(string code, string message, ErrorType type)
    {
        Code = code;
        Message = message;
        Type = type;
    }

    public Error(string code, string message, ErrorType type, Exception exception)
        : this(code, message, ErrorType.Failure)
    {
        Reasons.Add(new ExceptionalError(exception));
    }

    public Error(string code, string message, ErrorType type, IError error)
        : this(code, message, ErrorType.Failure)
    {
        Reasons.Add(error);
    }

    public static Error Failure(string code, string message) =>
       new(code, message, ErrorType.Failure);

    public static Error Failure(string code, string message, Exception exception) =>
       new(code, message, ErrorType.Failure, exception);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Problem(string code, string message) =>
        new(code, message, ErrorType.Problem);

    public static Error ValidationFailed(string code, string message) =>
        new(code, message, ErrorType.Validation);
}
