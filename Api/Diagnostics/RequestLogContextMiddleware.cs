using System.Security.Claims;
using Serilog.Context;

namespace Api.Diagnostics;

/// <summary>
/// Pushes the request correlation id (and, when authenticated, the acting
/// store/user ids) into Serilog's ambient log context so every log event
/// emitted while processing the request carries them.
/// </summary>
public sealed class RequestLogContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLogContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        using var correlationId = LogContext.PushProperty("CorrelationId", context.TraceIdentifier);

        var storeId = context.User.FindFirstValue("store_id");
        if (storeId is null)
        {
            await _next(context);
            return;
        }

        using var storeScope = LogContext.PushProperty("StoreId", storeId);
        using var userScope = LogContext.PushProperty("UserId", context.User.FindFirstValue("sub"));

        await _next(context);
    }
}