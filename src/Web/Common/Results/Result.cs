namespace Web.Common.Results;

public readonly struct Result
{
    private readonly AppError? _error;

    public bool IsSuccess => _error is null;
    public bool IsFailure => _error is not null;
    public AppError Error => _error ?? throw new InvalidOperationException("Success result has no error.");

    private Result(AppError? error) => _error = error;

    public static Result Success() => new(null);

    public static Result Failure(AppError error) => new(error);

    public static implicit operator Result(AppError error) => Failure(error);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<AppError, TResult> onFailure)
        => IsSuccess ? onSuccess() : onFailure(Error);
}

public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly AppError? _error;

    public bool IsSuccess => _error is null;
    public bool IsFailure => _error is not null;
    public T Value => _value ?? throw new InvalidOperationException("Failure result has no value.");
    public AppError Error => _error ?? throw new InvalidOperationException("Success result has no error.");

    private Result(T? value, AppError? error)
    {
        _value = value;
        _error = error;
    }

    public static Result<T> Success(T value) => new(value, null);

    public static Result<T> Failure(AppError error) => new(default, error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(AppError error) => Failure(error);

    public Result<TOut> Map<TOut>(Func<T, TOut> selector)
        => IsSuccess ? Result<TOut>.Success(selector(Value)) : Result<TOut>.Failure(Error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<AppError, TResult> onFailure)
        => IsSuccess ? onSuccess(Value) : onFailure(Error);
}
