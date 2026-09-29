namespace StreamCatalog.Functions.Application;

public sealed record OperationResult<T>
{
    internal OperationResult(T? value, ApiError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public ApiError? Error { get; }

    public bool Succeeded => Error is null;
}

public static class OperationResult
{
    public static OperationResult<T> Success<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new OperationResult<T>(value, null);
    }

    public static OperationResult<T> Failure<T>(ApiError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new OperationResult<T>(default, error);
    }
}
