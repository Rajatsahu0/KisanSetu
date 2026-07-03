using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KisanMitraAI.Core.Authentication;

/// <summary>
/// Extension methods for registering authentication services (Firebase-based).
/// Note: Firebase Admin SDK is initialized in Program.cs.
/// This extension is kept for backward compatibility with existing code that calls AddCognitoAuthentication.
/// </summary>
public static class AuthenticationServiceExtensions
{
    /// <summary>
    /// Registers Firebase-based authentication services (replaces Cognito).
    /// </summary>
    public static IServiceCollection AddCognitoAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Firebase Auth is initialized in Program.cs via FirebaseApp.Create()
        // Register the auth service implementation
        services.AddScoped<ICognitoAuthService, CognitoAuthService>();
        return services;
    }
}
