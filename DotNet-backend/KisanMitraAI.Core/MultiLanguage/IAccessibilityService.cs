namespace KisanMitraAI.Core.MultiLanguage;

/// <summary>
/// Service for managing accessibility features
/// </summary>
public interface IAccessibilityService
{
    /// <summary>
    /// Gets accessibility settings for a farmer
    /// </summary>
    /// <param name="farmerId">The farmer's unique identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The farmer's accessibility settings</returns>
    Task<AccessibilitySettings> GetSettingsAsync(
        string farmerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates accessibility settings for a farmer
    /// </summary>
    /// <param name="farmerId">The farmer's unique identifier</param>
    /// <param name="settings">The new accessibility settings</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateSettingsAsync(
        string farmerId,
        AccessibilitySettings settings,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Accessibility settings for a farmer
/// </summary>
public record AccessibilitySettings(
    bool HighContrastMode,
    TextSize TextSize,
    bool ScreenReaderEnabled,
    bool KeyboardNavigationEnabled,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Text size options for visual accessibility
/// </summary>
public enum TextSize
{
    Normal,
    Large,
    ExtraLarge
}
