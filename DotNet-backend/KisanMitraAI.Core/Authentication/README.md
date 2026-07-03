# AWS Cognito Authentication Service

This module provides AWS Cognito integration for user authentication and authorization in the Kisan Mitra AI platform.

## Features

- User registration with phone number verification (OTP)
- JWT token generation and validation using Cognito User Pools
- Token refresh with automatic retry on expiration
- Secure password-based authentication
- Phone number as username (E.164 format)

## Configuration

Add the following configuration to your `appsettings.json`:

```json
{
  "Cognito": {
    "UserPoolId": "us-east-1_XXXXXXXXX",
    "ClientId": "XXXXXXXXXXXXXXXXXXXXXXXXXX",
    "Region": "us-east-1",
    "MaxRetryAttempts": 3,
    "RetryDelayMilliseconds": 1000
  }
}
```

### Configuration Properties

- **UserPoolId**: AWS Cognito User Pool ID
- **ClientId**: AWS Cognito App Client ID
- **Region**: AWS region where the User Pool is hosted (e.g., `ap-south-1` for India)
- **MaxRetryAttempts**: Maximum number of retry attempts for token refresh (default: 3)
- **RetryDelayMilliseconds**: Delay between retry attempts in milliseconds (default: 1000)

## Setup

### 1. Register Services

In your `Program.cs` or `Startup.cs`:

```csharp
using KisanMitraAI.Core.Authentication;

// Add Cognito authentication services
builder.Services.AddCognitoAuthentication(builder.Configuration);
```

### 2. AWS Cognito User Pool Setup

Create a Cognito User Pool with the following settings:

1. **Sign-in options**: Phone number
2. **Password policy**: Configure according to security requirements
3. **MFA**: Optional (recommended for production)
4. **Phone number verification**: Required
5. **App client settings**:
   - Enable `USER_PASSWORD_AUTH` flow
   - No client secret (for mobile/web apps)

## Usage

### Registration Flow

```csharp
public class AuthController : ControllerBase
{
    private readonly ICognitoAuthService _authService;

    public AuthController(ICognitoAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        // Step 1: Register user
        var result = await _authService.RegisterAsync(
            request.PhoneNumber,
            request.Password,
            request.Name,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage);
        }

        // User will receive OTP via SMS
        return Ok(new { 
            message = "Registration successful. Please verify your phone number.",
            requiresConfirmation = result.RequiresConfirmation 
        });
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmRegistration(
        [FromBody] ConfirmRequest request,
        CancellationToken cancellationToken)
    {
        // Step 2: Confirm registration with OTP
        var result = await _authService.ConfirmRegistrationAsync(
            request.PhoneNumber,
            request.ConfirmationCode,
            cancellationToken);

        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage);
        }

        return Ok(new { message = "Phone number verified successfully" });
    }
}
```

### Login Flow

```csharp
[HttpPost("login")]
public async Task<IActionResult> Login(
    [FromBody] LoginRequest request,
    CancellationToken cancellationToken)
{
    var result = await _authService.LoginAsync(
        request.PhoneNumber,
        request.Password,
        cancellationToken);

    if (!result.Success)
    {
        return Unauthorized(result.ErrorMessage);
    }

    return Ok(new
    {
        accessToken = result.AccessToken,
        refreshToken = result.RefreshToken,
        idToken = result.IdToken,
        expiresIn = result.ExpiresInSeconds
    });
}
```

### Token Refresh

```csharp
[HttpPost("refresh")]
public async Task<IActionResult> RefreshToken(
    [FromBody] RefreshRequest request,
    CancellationToken cancellationToken)
{
    var result = await _authService.RefreshTokenAsync(
        request.RefreshToken,
        cancellationToken);

    if (!result.Success)
    {
        return Unauthorized(result.ErrorMessage);
    }

    return Ok(new
    {
        accessToken = result.AccessToken,
        idToken = result.IdToken,
        expiresIn = result.ExpiresInSeconds
    });
}
```

### Token Validation

```csharp
[HttpGet("validate")]
public async Task<IActionResult> ValidateToken(
    [FromHeader(Name = "Authorization")] string authorization,
    CancellationToken cancellationToken)
{
    // Extract token from "Bearer {token}"
    var token = authorization?.Replace("Bearer ", "");
    
    if (string.IsNullOrEmpty(token))
    {
        return Unauthorized("No token provided");
    }

    var result = await _authService.ValidateTokenAsync(token, cancellationToken);

    if (!result.IsValid)
    {
        return Unauthorized(result.ErrorMessage);
    }

    return Ok(new
    {
        userId = result.UserId,
        phoneNumber = result.PhoneNumber,
        claims = result.Claims
    });
}
```

## Phone Number Format

Phone numbers must be in E.164 format:
- India: `+919876543210`
- Format: `+[country code][number]`

## Security Considerations

1. **HTTPS Only**: Always use HTTPS in production
2. **Token Storage**: Store tokens securely on the client side
3. **Token Expiration**: Access tokens typically expire in 1 hour
4. **Refresh Tokens**: Use refresh tokens to obtain new access tokens
5. **Password Policy**: Enforce strong password requirements in Cognito
6. **Rate Limiting**: Implement rate limiting to prevent brute force attacks

## Error Handling

The service returns detailed error messages for common scenarios:
- User already exists
- Invalid password format
- Invalid confirmation code
- Expired confirmation code
- Invalid credentials
- User not confirmed
- Token expired
- Invalid token

## Testing

For testing purposes, you can use the development configuration with a test Cognito User Pool. Ensure you have valid AWS credentials configured.

## Requirements Satisfied

- **Requirement 9.3**: Backend service authentication within 500ms
- **Requirement 10.3**: Role-based access control and data isolation
