namespace KisanMitraAI.Core.Authentication;

/// <summary>
/// Service interface for AWS Cognito authentication operations
/// </summary>
public interface ICognitoAuthService
{
    /// <summary>
    /// Registers a new user with phone number verification
    /// </summary>
    /// <param name="phoneNumber">User's phone number in E.164 format (e.g., +919876543210)</param>
    /// <param name="password">User's password</param>
    /// <param name="name">User's full name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Registration result containing user ID and verification status</returns>
    Task<RegistrationResult> RegisterAsync(
        string phoneNumber,
        string password,
        string name,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Confirms user registration with OTP code sent to phone
    /// </summary>
    /// <param name="phoneNumber">User's phone number</param>
    /// <param name="confirmationCode">OTP code received via SMS</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Confirmation result</returns>
    Task<ConfirmationResult> ConfirmRegistrationAsync(
        string phoneNumber,
        string confirmationCode,
        CancellationToken cancellationToken = default);

    Task<ConfirmationResult> AdminConfirmUserAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resends the OTP confirmation code to the user's phone
    /// </summary>
    Task<ConfirmationResult> ResendConfirmationCodeAsync(
        string phoneNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Authenticates a user and returns JWT tokens
    /// </summary>
    /// <param name="phoneNumber">User's phone number</param>
    /// <param name="password">User's password</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Login result containing access token, refresh token, and expiration</returns>
    Task<LoginResult> LoginAsync(
        string phoneNumber,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an expired access token using a refresh token
    /// </summary>
    /// <param name="refreshToken">Valid refresh token</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>New access token and expiration</returns>
    Task<RefreshTokenResult> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a JWT access token
    /// </summary>
    /// <param name="accessToken">JWT access token to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result containing claims if valid</returns>
    Task<TokenValidationResult> ValidateTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user custom attributes in Cognito (language, dialect)
    /// </summary>
    Task<bool> UpdateUserPreferencesAsync(
        string accessToken,
        string? preferredLanguage = null,
        string? preferredDialect = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of user registration
/// </summary>
public record RegistrationResult(
    bool Success,
    string? UserId,
    bool RequiresConfirmation,
    string? ErrorMessage = null);

/// <summary>
/// Result of registration confirmation
/// </summary>
public record ConfirmationResult(
    bool Success,
    string? ErrorMessage = null);

/// <summary>
/// Result of user login
/// </summary>
public record LoginResult(
    bool Success,
    string? AccessToken,
    string? RefreshToken,
    string? IdToken,
    int ExpiresInSeconds,
    string? ErrorMessage = null);

/// <summary>
/// Result of token refresh
/// </summary>
public record RefreshTokenResult(
    bool Success,
    string? AccessToken,
    string? IdToken,
    int ExpiresInSeconds,
    string? ErrorMessage = null);

/// <summary>
/// Result of token validation
/// </summary>
public record TokenValidationResult(
    bool IsValid,
    string? UserId,
    string? PhoneNumber,
    IDictionary<string, string>? Claims,
    string? ErrorMessage = null);
