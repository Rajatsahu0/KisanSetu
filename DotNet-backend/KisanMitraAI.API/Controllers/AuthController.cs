using KisanMitraAI.Core.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace KisanMitraAI.API.Controllers;

/// <summary>
/// Authentication controller for user registration and login
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ICognitoAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ICognitoAuthService authService,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new user with phone number. OTP will be sent via SMS for verification.
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Registration request received for phone: {PhoneNumber}", request.PhoneNumber);

        var result = await _authService.RegisterAsync(
            request.PhoneNumber,
            request.Password,
            request.Name,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new RegisterResponse
        {
            Message = "Registration successful. Please verify your phone number with the OTP sent via SMS.",
            UserId = result.UserId,
            RequiresConfirmation = result.RequiresConfirmation
        });
    }

    /// <summary>
    /// Confirm user registration with OTP code. Auto-logs in on success.
    /// </summary>
    [HttpPost("confirm")]
    [ProducesResponseType(typeof(ConfirmResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmRegistration(
        [FromBody] ConfirmRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Confirmation request received for phone: {PhoneNumber}", request.PhoneNumber);

        var result = await _authService.ConfirmRegistrationAsync(
            request.PhoneNumber,
            request.ConfirmationCode,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        // Auto-login after successful OTP verification
        if (!string.IsNullOrEmpty(request.Password))
        {
            var loginResult = await _authService.LoginAsync(request.PhoneNumber, request.Password, cancellationToken);
            if (loginResult.Success)
            {
                return Ok(new ConfirmResponse
                {
                    Message = "Phone number verified successfully. You are now logged in.",
                    AccessToken = loginResult.AccessToken,
                    RefreshToken = loginResult.RefreshToken,
                    IdToken = loginResult.IdToken,
                    ExpiresIn = loginResult.ExpiresInSeconds
                });
            }
        }

        return Ok(new ConfirmResponse
        {
            Message = "Phone number verified successfully. You can now log in."
        });
    }

    /// <summary>
    /// Resend OTP code to phone number
    /// </summary>
    [HttpPost("resend-otp")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendOtp(
        [FromBody] ResendOtpRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resend OTP request for phone: {PhoneNumber}", request.PhoneNumber);

        var result = await _authService.ResendConfirmationCodeAsync(request.PhoneNumber, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new { message = "OTP sent successfully. Please check your SMS." });
    }

    /// <summary>
    /// Login with phone number and password
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login request received for phone: {PhoneNumber}", request.PhoneNumber);

        var result = await _authService.LoginAsync(
            request.PhoneNumber,
            request.Password,
            cancellationToken);

        if (!result.Success)
        {
            return Unauthorized(new { error = result.ErrorMessage });
        }

        return Ok(new LoginResponse
        {
            AccessToken = result.AccessToken!,
            RefreshToken = result.RefreshToken!,
            IdToken = result.IdToken!,
            ExpiresIn = result.ExpiresInSeconds,
            TokenType = "Bearer"
        });
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Token refresh request received");

        var result = await _authService.RefreshTokenAsync(
            request.RefreshToken,
            cancellationToken);

        if (!result.Success)
        {
            return Unauthorized(new { error = result.ErrorMessage });
        }

        return Ok(new RefreshResponse
        {
            AccessToken = result.AccessToken!,
            IdToken = result.IdToken!,
            ExpiresIn = result.ExpiresInSeconds,
            TokenType = "Bearer"
        });
    }

    /// <summary>
    /// Update user language/dialect preferences in Cognito
    /// </summary>
    [HttpPut("preferences")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdatePreferences(
        [FromHeader(Name = "Authorization")] string? authorization,
        [FromBody] UpdatePreferencesRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer "))
            return Unauthorized(new { error = "No valid authorization header provided" });

        var token = authorization.Replace("Bearer ", "");
        var success = await _authService.UpdateUserPreferencesAsync(
            token, request.PreferredLanguage, request.PreferredDialect, cancellationToken);

        return success ? Ok(new { message = "Preferences updated" }) : StatusCode(500, new { error = "Failed to update preferences" });
    }

    /// <summary>
    /// Validate access token
    /// </summary>
    [HttpGet("validate")]
    [ProducesResponseType(typeof(ValidateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ValidateToken(
        [FromHeader(Name = "Authorization")] string? authorization,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Bearer "))
        {
            return Unauthorized(new { error = "No valid authorization header provided" });
        }

        var token = authorization.Replace("Bearer ", "");

        var result = await _authService.ValidateTokenAsync(token, cancellationToken);

        if (!result.IsValid)
        {
            return Unauthorized(new { error = result.ErrorMessage });
        }

        return Ok(new ValidateResponse
        {
            IsValid = true,
            UserId = result.UserId!,
            PhoneNumber = result.PhoneNumber,
            Claims = result.Claims
        });
    }
}

// Request/Response DTOs
public record RegisterRequest(string PhoneNumber, string Password, string Name);
public record RegisterResponse
{
    public string Message { get; init; } = string.Empty;
    public string? UserId { get; init; }
    public bool RequiresConfirmation { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public string? IdToken { get; init; }
    public int? ExpiresIn { get; init; }
}

public record ConfirmRequest(string PhoneNumber, string ConfirmationCode, string? Password = null);
public record ConfirmResponse
{
    public string Message { get; init; } = string.Empty;
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public string? IdToken { get; init; }
    public int? ExpiresIn { get; init; }
}

public record ResendOtpRequest(string PhoneNumber);

public record LoginRequest(string PhoneNumber, string Password);
public record LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public string IdToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public string TokenType { get; init; } = string.Empty;
}

public record RefreshRequest(string RefreshToken);
public record RefreshResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string IdToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public string TokenType { get; init; } = string.Empty;
}

public record ValidateResponse
{
    public bool IsValid { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public IDictionary<string, string>? Claims { get; init; }
}

public record UpdatePreferencesRequest(string? PreferredLanguage = null, string? PreferredDialect = null);
