namespace Owlet.Core.Results;

/// <summary>
/// Represents the result of an operation that can either succeed or fail.
/// Implements the Result pattern for functional error handling.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1000:Do not declare static members on generic types",
    Justification = "Static factory methods are the idiomatic way to create Result instances in functional programming")]
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly string? _error;

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = null;
    }

    private Result(string error)
    {
        IsSuccess = false;
        _value = default;
        _error = error ?? throw new ArgumentNullException(nameof(error));
    }

    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value. Throws if the result represents a failure.
    /// </summary>
    public T Value
    {
        get
        {
            if (!IsSuccess)
                throw new InvalidOperationException($"Cannot access Value on a failed result. Error: {_error}");

            return _value!;
        }
    }

    /// <summary>
    /// Gets the error message. Returns empty string if the result represents success.
    /// </summary>
    public string Error => _error ?? string.Empty;

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    public static Result<T> Failure(string error) => new(error);

    /// <summary>
    /// Maps the result value if successful, otherwise propagates the failure.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
        return IsSuccess
            ? Result<TOut>.Success(mapper(Value))
            : Result<TOut>.Failure(Error);
    }

    /// <summary>
    /// Binds to another result-producing operation if successful, otherwise propagates the failure.
    /// </summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
    {
        return IsSuccess
            ? binder(Value)
            : Result<TOut>.Failure(Error);
    }

    /// <summary>
    /// Executes an action on the value if successful, returns this result.
    /// </summary>
    public Result<T> Tap(Action<T> action)
    {
        if (IsSuccess)
            action(Value);

        return this;
    }

    /// <summary>
    /// Returns the value if successful, otherwise returns the specified default value.
    /// </summary>
    public T GetValueOrDefault(T defaultValue) => IsSuccess ? Value : defaultValue;

    /// <summary>
    /// Returns the value if successful, otherwise invokes the specified function to produce a default value.
    /// </summary>
    public T GetValueOrDefault(Func<string, T> defaultProvider) =>
        IsSuccess ? Value : defaultProvider(Error);

    /// <summary>
    /// Matches on the result, executing either the success or failure function.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, TOut> onFailure) =>
        IsSuccess ? onSuccess(Value) : onFailure(Error);

    /// <summary>
    /// Implicitly converts a value to a successful result.
    /// </summary>
    public static implicit operator Result<T>(T value) => Success(value);

    public override string ToString() =>
        IsSuccess ? $"Success({Value})" : $"Failure({Error})";
}

/// <summary>
/// Non-generic result for operations that don't return a value.
/// </summary>
public sealed class Result
{
    private readonly string? _error;

    private Result(bool isSuccess, string? error = null)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error => _error ?? string.Empty;

    public static Result Success() => new(true);
    public static Result Failure(string error) => new(false, error ?? throw new ArgumentNullException(nameof(error)));

    public Result Tap(Action action)
    {
        if (IsSuccess)
            action();

        return this;
    }

    public TOut Match<TOut>(Func<TOut> onSuccess, Func<string, TOut> onFailure) =>
        IsSuccess ? onSuccess() : onFailure(Error);

    public override string ToString() =>
        IsSuccess ? "Success" : $"Failure({Error})";
}
