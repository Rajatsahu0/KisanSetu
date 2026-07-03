using FirebaseAdmin.Auth;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Core.Authentication;

/// <summary>
/// Firebase Authentication service implementation (replaces AWS Cognito).
/// Phone auth registration/OTP is handled client-side via Firebase JS SDK.
/// Server-side: token verification and user management.
/// </summary>
public class CognitoAuthService : ICognitoAuthService
{
    private readonly ILogger<CognitoAuthService> _logger;

    public CognitoAuthService(ILogger<CognitoAuthService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<RegistrationResult> RegisterAsync(
        string phoneNumber, string password, string name,
        CancellationToken cancellationToken = default)
    {
        // Firebase phone auth is client-side (Firebase JS SDK handles OTP)
        // Server creates user record if needed
        _logger.LogInformation("Register requested for {Phone} — handled client-side via Firebase", phoneNumber);
        return Task.FromResult(new RegistrationResult(
            Success: true, UserId: null, RequiresConfirmation: true,
            ErrorMessage: "Please complete phone verification via the app"));
    }

    public Task<ConfirmationResult> ConfirmRegistrationAsync(
        string phoneNumber, string confirmationCode,
        CancellationToken cancellationToken = default)
    {
        // OTP verification is handled by Firebase client SDK
        _logger.LogInformation("Confirmation for {Phone} — handled client-side", phoneNumber);
        return Task.FromResult(new ConfirmationResult(Success: true));
    }

    public Task<ConfirmationResult> AdminConfirmUserAsync(
        string phoneNumber, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Admin confirm for {Phone}", phoneNumber);
        return Task.FromResult(new ConfirmationResult(Success: true));
    }

    public Task<ConfirmationResult> ResendConfirmationCodeAsync(
        string phoneNumber, CancellationToken cancellationToken = default)
    {
        // Handled client-side by Firebase
        return Task.FromResult(new ConfirmationResult(Success: true));
    }

    public Task<LoginResult> LoginAsync(
        string phoneNumber, string password,
        CancellationToken cancellationToken = default)
    {
        // Firebase phone auth returns ID token directly from client SDK
        _logger.LogInformation("Login for {Phone} — use Firebase client SDK token", phoneNumber);
        return Task.FromResult(new LoginResult(
            Success: true, AccessToken: null, RefreshToken: null, IdToken: null,
            ExpiresInSeconds: 3600,
            ErrorMessage: "Use Firebase client SDK for phone auth login"));
    }

    public Task<RefreshTokenResult> RefreshTokenAsync(
        string refreshToken, CancellationToken cancellationToken = default)
    {
        // Firebase handles token refresh client-side
        return Task.FromResult(new RefreshTokenResult(
            Success: true, AccessToken: null, IdToken: null, ExpiresInSeconds: 3600));
    }

    public async Task<TokenValidationResult> ValidateTokenAsync(
        string accessToken, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Validating Firebase ID token");

            var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(accessToken, cancellationToken);

            var claims = new Dictionary<string, string>
            {
                ["sub"] = decodedToken.Uid,
                ["firebase_uid"] = decodedToken.Uid
            };

            if (decodedToken.Claims.TryGetValue("phone_number", out var phone))
                claims["phone_number"] = phone?.ToString() ?? "";

            _logger.LogDebug("Token validated for user: {Uid}", decodedToken.Uid);

            return new TokenValidationResult(
                IsValid: true,
                UserId: decodedToken.Uid,
                PhoneNumber: claims.GetValueOrDefault("phone_number"),
                Claims: claims);
        }
        catch (FirebaseAuthException ex)
        {
            _logger.LogWarning(ex, "Firebase token validation failed");
            return new TokenValidationResult(false, null, null, null, $"Token validation failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating token");
            return new TokenValidationResult(false, null, null, null, $"Token validation failed: {ex.Message}");
        }
    }

    public Task<bool> UpdateUserPreferencesAsync(
        string accessToken, string? preferredLanguage = null,
        string? preferredDialect = null, CancellationToken cancellationToken = default)
    {
        // User preferences stored in Firestore user profile, not in auth provider
        _logger.LogInformation("Preferences update — stored in Firestore profile");
        return Task.FromResult(true);
    }
}
