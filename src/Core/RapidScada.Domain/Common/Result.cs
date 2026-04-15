namespace RapidScada.Domain.Common;

/// <summary>
/// Represents the result of an operation that can fail
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException("Success result cannot have an error");
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException("Failure result must have an error");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
    public static Result<TValue> Failure<TValue>(Error error) => new(default!, false, error);

    public static Result Create(bool condition, Error error) =>
        condition ? Success() : Failure(error);
}

/// <summary>
/// Represents the result of an operation that returns a value
/// </summary>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access value of a failed result");

    public static implicit operator Result<TValue>(TValue value) =>
        Success(value);
}

/// <summary>
/// Represents an error with a code and message
/// </summary>
public sealed record Error(string Code, string Message)
{
    public static Error None => new(string.Empty, string.Empty);

    public static Error NullValue(string propertyName) =>
        new("Error.NullValue", $"{propertyName} cannot be null");

    public static Error InvalidValue(string propertyName, string? reason = null) =>
        new("Error.InvalidValue",
            reason is null ? $"{propertyName} is invalid" : $"{propertyName} is invalid: {reason}");

    public static Error NotFound(string entityName, object key) =>
        new("Error.NotFound", $"{entityName} with key '{key}' was not found");

    public static Error Conflict(string message) =>
        new("Error.Conflict", message);

    public static Error Validation(string message) =>
        new("Error.Validation", message);

    public static Error Failure(string message) => new("Error.Failed", message);
}
