using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Core.Authorization;

/// <summary>
/// Middleware that logs authorization failures for security auditing.
/// </summary>
public class AuthorizationLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationLoggingMiddleware> _logger;

    public AuthorizationLoggingMiddleware(
        RequestDelegate next,
        ILogger<AuthorizationLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Log authorization failures (403 Forbidden)
        if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
        {
            var farmerId = context.User.GetFarmerId();
            var requestPath = context.Request.Path;
            var requestMethod = context.Request.Method;

            _logger.LogWarning(
                "Authorization failed for FarmerId: {FarmerId}, Path: {Path}, Method: {Method}",
                farmerId ?? "Unknown",
                requestPath,
                requestMethod);
        }
    }
}
