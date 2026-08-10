namespace auth_api_login.Application.Common;

public enum ResultErrorType
{
    Conflict,
    Unauthorized,
    NotFound,
    Validation
}

public readonly record struct ResultError(ResultErrorType Type, string Message);

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public ResultError? Error { get; }

    protected Result(bool isSuccess, ResultError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Conflict(string message) => new(false, new ResultError(ResultErrorType.Conflict, message));
    public static Result Unauthorized(string message) => new(false, new ResultError(ResultErrorType.Unauthorized, message));
    public static Result NotFound(string message) => new(false, new ResultError(ResultErrorType.NotFound, message));
    public static Result Validation(string message) => new(false, new ResultError(ResultErrorType.Validation, message));
}

public sealed class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Não é possível acessar o valor de um resultado de falha.");

    private Result(T value) : base(true, null) => _value = value;
    private Result(ResultError error) : base(false, error) => _value = default;

    public static Result<T> Success(T value) => new(value);
    public static new Result<T> Conflict(string message) => new(new ResultError(ResultErrorType.Conflict, message));
    public static new Result<T> Unauthorized(string message) => new(new ResultError(ResultErrorType.Unauthorized, message));
    public static new Result<T> NotFound(string message) => new(new ResultError(ResultErrorType.NotFound, message));
    public static new Result<T> Validation(string message) => new(new ResultError(ResultErrorType.Validation, message));
}
