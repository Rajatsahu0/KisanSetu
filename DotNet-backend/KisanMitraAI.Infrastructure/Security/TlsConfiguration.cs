using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.DependencyInjection;

namespace KisanMitraAI.Infrastructure.Security;

/// <summary>
/// Configuration for TLS 1.3 and HTTPS enforcement
/// </summary>
public static class TlsConfiguration
{
    /// <summary>
    /// Configures TLS 1.3 and HTTPS enforcement for the application
    /// </summary>
    public static IServiceCollection AddTlsConfiguration(this IServiceCollection services)
    {
        // Configure HTTPS redirection
        services.AddHttpsRedirection(options =>
        {
            options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
            options.HttpsPort = 443;
        });

        // Configure HSTS (HTTP Strict Transport Security)
        services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
            options.MaxAge = TimeSpan.FromDays(365); // 1 year
        });

        return services;
    }

    /// <summary>
    /// Configures the application to use TLS 1.3 and HTTPS enforcement
    /// </summary>
    public static IApplicationBuilder UseTlsConfiguration(this IApplicationBuilder app)
    {
        // Use HSTS in production
        app.UseHsts();

        // Enforce HTTPS redirection
        app.UseHttpsRedirection();

        // Add security headers
        app.Use(async (context, next) =>
        {
            // HSTS header (already added by UseHsts, but explicit for clarity)
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains; preload";
            
            // Prevent MIME type sniffing
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            
            // Prevent clickjacking
            context.Response.Headers["X-Frame-Options"] = "DENY";
            
            // XSS protection
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            
            // Content Security Policy - includes Salesforce Agentforce Embedded Messaging domains
            context.Response.Headers["Content-Security-Policy"] = 
                "default-src 'self'; " +
                "frame-src 'self' https://*.my.site.com https://*.salesforce-scrt.com; " +
                "connect-src 'self' https://*.my.site.com https://*.salesforce-scrt.com; " +
                "script-src 'self' 'unsafe-inline' https://*.my.site.com; " +
                "style-src 'self' 'unsafe-inline' https://*.my.site.com; " +
                "frame-ancestors 'none'";
            
            // Referrer policy
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            
            // Permissions policy
            context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

            await next();
        });

        return app;
    }
}
