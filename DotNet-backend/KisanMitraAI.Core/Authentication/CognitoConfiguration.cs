namespace KisanMitraAI.Core.Authentication;

/// <summary>
/// Configuration settings for AWS Cognito User Pool
/// </summary>
public class CognitoConfiguration
{
    /// <summary>
    /// AWS Cognito User Pool ID
    /// </summary>
    public string UserPoolId { get; set; } = string.Empty;

    /// <summary>
    /// AWS Cognito App Client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// AWS Region where the User Pool is hosted (e.g., ap-south-1)
    /// </summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>
    /// Maximum number of retry attempts for token refresh
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Delay in milliseconds between retry attempts
    /// </summary>
    public int RetryDelayMilliseconds { get; set; } = 1000;

}
