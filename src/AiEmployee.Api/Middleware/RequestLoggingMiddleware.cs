using Microsoft.AspNetCore.Http.Extensions;

namespace AiEmployee.Api.Middleware;

/// <summary>
/// Logs every incoming HTTP request (method, path, query) for integration diagnostics (e.g. Telegram webhooks).
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        _logger.LogInformation(
            "HTTP {Method} {DisplayUrl}",
            request.Method,
            request.GetDisplayUrl());

        await _next(context);
    }
}
