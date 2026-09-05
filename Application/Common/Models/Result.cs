using System.Reflection;

namespace Application.Common.Models;

public enum FailureKind
{
    BadRequest,
    Unauthorized,
    NotFound,
    Validation,
    Conflict
}

public interface IResult
{
    bool IsSuccess { get; }

    string? Error { get; }

    FailureKind? Kind { get; }

    IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }
}

public class Result : IResult
{
    protected Result(bool isSuccess, string? error, FailureKind? kind, IReadOnlyDictionary<string, string[]>? validationErrors)
    {
        IsSuccess = isSuccess;
        Error = error;
        Kind = kind;
        ValidationErrors = validationErrors;
    }

    public bool IsSuccess { get; }

    public string? Error { get; }

    public FailureKind? Kind { get; }

    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }

    public static Result Success()
    {
        return new Result(true, null, null, null);
    }

    public static Result From(IResult result)
    {
        return new Result(result.IsSuccess, result.Error, result.Kind, result.ValidationErrors);
    }

    public static Result BadRequest(string error)
    {
        return new Result(false, error, FailureKind.BadRequest, null);
    }

    public static Result Unauthorized(string error)
    {
        return new Result(false, error, FailureKind.Unauthorized, null);
    }

    public static Result NotFound(string error)
    {
        return new Result(false, error, FailureKind.NotFound, null);
    }

    public static Result Validation(IReadOnlyDictionary<string, string[]> errors)
    {
        return new Result(false, null, FailureKind.Validation, errors);
    }

    public static Result Conflict(string error)
    {
        return new Result(false, error, FailureKind.Conflict, null);
    }
}

public class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, string? error, FailureKind? kind, IReadOnlyDictionary<string, string[]>? validationErrors)
        : base(isSuccess, error, kind, validationErrors)
    {
        _value = value;
    }

    public T? Value
    {
        get
        {
            return IsSuccess ? _value : default;
        }
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(value, true, null, null, null);
    }

    public static Result<T> Failure(string error)
    {
        return new Result<T>(default, false, error, null, null);
    }

    public static new Result<T> BadRequest(string error)
    {
        return new Result<T>(default, false, error, FailureKind.BadRequest, null);
    }

    public static new Result<T> Unauthorized(string error)
    {
        return new Result<T>(default, false, error, FailureKind.Unauthorized, null);
    }

    public static new Result<T> NotFound(string error)
    {
        return new Result<T>(default, false, error, FailureKind.NotFound, null);
    }

    public static new Result<T> Validation(IReadOnlyDictionary<string, string[]> errors)
    {
        return new Result<T>(default, false, null, FailureKind.Validation, errors);
    }

    public static new Result<T> Conflict(string error)
    {
        return new Result<T>(default, false, error, FailureKind.Conflict, null);
    }
}

internal static class ResultFactory
{
    private static readonly MethodInfo NonGenericValidationMethod =
        typeof(Result).GetMethod(nameof(Result.Validation), BindingFlags.Public | BindingFlags.Static)
        ?? throw new MissingMethodException(nameof(Result), nameof(Result.Validation));

    public static object? CreateValidationFailure(Type responseType, IReadOnlyDictionary<string, string[]> errors)
    {
        if (!typeof(IResult).IsAssignableFrom(responseType))
        {
            return null;
        }

        if (responseType.IsGenericType)
        {
            var closedType = typeof(Result<>).MakeGenericType(responseType.GetGenericArguments()[0]);
            var method = closedType.GetMethod(nameof(Result.Validation), BindingFlags.Public | BindingFlags.Static)
                ?? throw new MissingMethodException(closedType.FullName, nameof(Result.Validation));

            return method.Invoke(null, new object[] { errors });
        }

        return NonGenericValidationMethod.Invoke(null, new object[] { errors });
    }
}