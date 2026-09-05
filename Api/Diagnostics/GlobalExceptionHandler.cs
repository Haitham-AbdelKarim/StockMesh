using Application.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Diagnostics;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException => CreateValidationProblemDetails(
                (ValidationException)exception),
            BadRequestException => new ProblemDetails
            {
                Title = "Bad request",
                Status = StatusCodes.Status400BadRequest,
                Detail = exception.Message
            },
            UnauthorizedException => new ProblemDetails
            {
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = exception.Message
            },
            ConflictException => new ProblemDetails
            {
                Title = "Conflict",
                Status = StatusCodes.Status409Conflict,
                Detail = exception.Message
            },
            _ => new ProblemDetails
            {
                Title = "An error occurred while processing your request.",
                Status = StatusCodes.Status500InternalServerError,
                Detail = "Please try again later."
            }
        };

        var statusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        _logger.Log(
            statusCode >= StatusCodes.Status500InternalServerError
                ? LogLevel.Error
                : LogLevel.Warning,
            exception,
            "{Title}: {Message}",
            problemDetails.Title,
            exception.Message);

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }

    private static ProblemDetails CreateValidationProblemDetails(
        ValidationException exception)
    {
        var details = new ProblemDetails
        {
            Title = "Validation failed",
            Status = StatusCodes.Status422UnprocessableEntity
        };

        details.Extensions["errors"] = exception.Errors
            .GroupBy(f => f.PropertyName ?? string.Empty)
            .ToDictionary(
                group => group.Key,
                group => group.Select(f => f.ErrorMessage).ToArray());

        return details;
    }
}