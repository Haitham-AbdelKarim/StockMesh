using Application.Common.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

public static class ControllerBaseResultExtensions
{
    public static ActionResult<T> FromResult<T>(
        this ControllerBase controller,
        Result<T> result,
        int successStatusCode = StatusCodes.Status200OK)
    {
        if (result.IsSuccess)
        {
            return (ActionResult<T>)controller.StatusCode(successStatusCode, result.Value);
        }

        return controller.MatchFailure(result);
    }

    private static ActionResult<T> MatchFailure<T>(
        this ControllerBase controller,
        Result<T> result)
    {
        var problem = CreateProblemDetails(result);

        problem.Extensions["traceId"] = controller.HttpContext?.TraceIdentifier;

        return result.Kind switch
        {
            FailureKind.Unauthorized => (ActionResult<T>)controller.Unauthorized(problem),
            FailureKind.NotFound => (ActionResult<T>)controller.NotFound(problem),
            FailureKind.Validation => (ActionResult<T>)controller.UnprocessableEntity(problem),
            FailureKind.Conflict => (ActionResult<T>)controller.Conflict(problem),
            _ => (ActionResult<T>)controller.BadRequest(problem)
        };
    }

    private static ProblemDetails CreateProblemDetails<T>(Result<T> result)
    {
        return result.Kind switch
        {
            FailureKind.Unauthorized => new ProblemDetails
            {
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = result.Error ?? "The request is not authorized."
            },
            FailureKind.NotFound => new ProblemDetails
            {
                Title = "Not Found",
                Status = StatusCodes.Status404NotFound,
                Detail = result.Error ?? "The requested resource was not found."
            },
            FailureKind.Conflict => new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = result.Error ?? "The operation conflicts with an existing resource."
            },
            FailureKind.Validation => new ValidationProblemDetails(
                ToDictionary(result.ValidationErrors))
            {
                Title = "Validation failed",
                Status = StatusCodes.Status422UnprocessableEntity
            },
            _ => new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = result.Error ?? "The request is invalid."
            }
        };
    }

    private static IDictionary<string, string[]> ToDictionary(
        IReadOnlyDictionary<string, string[]>? errors)
    {
        return errors is null
            ? new Dictionary<string, string[]>()
            : errors.ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}