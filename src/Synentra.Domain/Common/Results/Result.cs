using Synentra.Domain.Common.Diagnostics;

namespace Synentra.Domain.Common.Results;

public class Result
{
    public bool IsSuccess { get; }
    public Diagnostic? Diagnostic { get; }

    protected Result(bool isSuccess, Diagnostic? diagnostic)
    {
        IsSuccess = isSuccess;
        Diagnostic = diagnostic;
    }

    public static Result Success() => new(true, null);
    public static Task<Result> SuccessAsync() => Task.FromResult(Success());
    public static Result Failure(Diagnostic diagnostic) => new(false, diagnostic);
    public static Task<Result> FailureAsync(Diagnostic diagnostic) => Task.FromResult(Failure(diagnostic));
}

public class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true, null)
    {
        Value = value;
    }

    private Result(Diagnostic diagnostic) : base(false, diagnostic)
    {
    }

    public static Result<T> Success(T value) => new(value);
    public static Task<Result<T>> SuccessAsync(T value) => Task.FromResult(Success(value));
    public static new Result<T> Failure(Diagnostic diagnostic) => new(diagnostic);
    public static new Task<Result<T>> FailureAsync(Diagnostic diagnostic) => Task.FromResult(Failure(diagnostic));

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Diagnostic diagnostic) => Failure(diagnostic);
}