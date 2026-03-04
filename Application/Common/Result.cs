namespace AiCFO.Application.Common;

/// <summary>
/// Result pattern for command/query handlers.
/// Never throw exceptions for business logic - always return Result.
/// </summary>
public abstract record Result
{
    public sealed record Success(object? Data = null) : Result;
    public sealed record Failure(string Message, string? Code = null, Dictionary<string, string[]>? Errors = null) : Result;

    public static Result Ok(object? data = null) => new Success(data);
    public static Result Fail(string message, string? code = null, Dictionary<string, string[]>? errors = null) 
        => new Failure(message, code, errors);
}

/// <summary>
/// Generic Result pattern for type-safe responses.
/// </summary>
public abstract record Result<T>
{
    public sealed record Success(T Data) : Result<T>;
    public sealed record Failure(string Message, string? Code = null, Dictionary<string, string[]>? Errors = null) : Result<T>;

    public bool IsSuccess => this is Success;
    public bool IsFailure => this is Failure;

    public T? Value => this is Success success ? success.Data : default;
    public string Error => this is Failure failure ? failure.Message : string.Empty;

    public static Result<T> Ok(T data) => new Success(data);
    public static Result<T> Fail(string message, string? code = null, Dictionary<string, string[]>? errors = null) 
        => new Failure(message, code, errors);

    /// <summary>
    /// Maps success result to a different type, passes through failures unchanged.
    /// </summary>
    public Result<TNew> Map<TNew>(Func<T, TNew> mapper) =>
        this switch
        {
            Success success => Result<TNew>.Ok(mapper(success.Data)),
            Failure failure => Result<TNew>.Fail(failure.Message, failure.Code, failure.Errors),
            _ => throw new InvalidOperationException("Unknown result type")
        };

    /// <summary>
    /// Binds (flatMaps) to another Result<T>, allowing chaining of operations.
    /// </summary>
    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder) =>
        this switch
        {
            Success success => binder(success.Data),
            Failure failure => Result<TNew>.Fail(failure.Message, failure.Code, failure.Errors),
            _ => throw new InvalidOperationException("Unknown result type")
        };

    /// <summary>
    /// Pattern match on the result.
    /// </summary>
    public TReturn Match<TReturn>(
        Func<T, TReturn> onSuccess,
        Func<string, string?, Dictionary<string, string[]>?, TReturn> onFailure) =>
        this switch
        {
            Success success => onSuccess(success.Data),
            Failure failure => onFailure(failure.Message, failure.Code, failure.Errors),
            _ => throw new InvalidOperationException("Unknown result type")
        };
}
