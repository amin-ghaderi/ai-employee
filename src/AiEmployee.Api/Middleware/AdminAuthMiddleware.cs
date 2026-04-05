using System.Security.Cryptography;
using System.Text;

namespace AiEmployee.Api.Middleware;

public sealed class AdminAuthMiddleware
{
    private const string AdminPathPrefix = "/admin";
    private const string HeaderName = "X-Admin-Key";
    private const string ConfigKey = "Admin:ApiKey";

    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public AdminAuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path;
        if (!path.StartsWithSegments(AdminPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var expected = _configuration[ConfigKey];
        if (string.IsNullOrEmpty(expected))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        if (!context.Request.Headers.TryGetValue(HeaderName, out var providedValues))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        var provided = providedValues.ToString();
        if (!FixedTimeEqualsUtf8(expected, provided))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await _next(context);
    }

    private static bool FixedTimeEqualsUtf8(string expected, string provided)
    {
        var a = Encoding.UTF8.GetBytes(expected);
        var b = Encoding.UTF8.GetBytes(provided);
        if (a.Length != b.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(a, b);
    }
}
