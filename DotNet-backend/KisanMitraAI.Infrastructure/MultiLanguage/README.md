# Multi-Language and Accessibility Module

This module provides multi-language support and accessibility features for the Kisan Mitra AI platform.

## Features

### Language Preference Service
- Supports 15 Indian languages (Hindi, Tamil, Telugu, Bengali, Marathi, Gujarati, Kannada, Malayalam, Odia, Punjabi, Assamese, Urdu, Kashmiri, Konkani, Sindhi)
- Supports 10 regional dialects (Bundelkhandi, Bhojpuri, Marwari, Awadhi, Braj, Magahi, Maithili, Chhattisgarhi, Haryanvi, Rajasthani)
- Persists preferences in DynamoDB
- Applies preferences across all sessions

### Voice Interface Service
- Voice input for all core features:
  - Market intelligence (Krishi-Vani)
  - Quality grading
  - Soil analysis
  - Planting advisory
  - Advisory questions
- Voice output for all responses
- Natural-sounding voices appropriate to selected language and dialect
- Seamless switching between voice and text interfaces

### Accessibility Service
- High-contrast mode for visual impairments
- Adjustable text sizes (Normal, Large, Extra Large)
- Screen reader support
- Keyboard navigation
- Settings persisted per farmer

## Architecture

### Components

1. **LanguagePreferenceService**: Manages language and dialect preferences in DynamoDB
2. **VoiceInterfaceService**: Integrates transcription and synthesis services for voice I/O
3. **AccessibilityService**: Manages accessibility settings in DynamoDB

### Data Storage

All preferences and settings are stored in DynamoDB tables:
- `KisanMitra-LanguagePreferences`: Language and dialect preferences
- `KisanMitra-AccessibilitySettings`: Accessibility settings

### Integration

The voice interface service integrates with:
- `ITranscriptionService`: For speech-to-text (Amazon Transcribe)
- `IVoiceSynthesizer`: For text-to-speech (Amazon Polly)
- `ILanguagePreferenceService`: For retrieving farmer preferences

## Usage

### Setting Language Preference

```csharp
await languagePreferenceService.SavePreferenceAsync(
    farmerId: "farmer123",
    language: Language.Hindi,
    dialect: Dialect.Bundelkhandi,
    cancellationToken);
```

### Getting Language Preference

```csharp
var preference = await languagePreferenceService.GetPreferenceAsync(
    farmerId: "farmer123",
    cancellationToken);
```

### Processing Voice Input

```csharp
var result = await voiceInterfaceService.ProcessVoiceInputAsync(
    audioStream: audioStream,
    farmerId: "farmer123",
    feature: "market_intelligence",
    cancellationToken);
```

### Generating Voice Output

```csharp
var audioStream = await voiceInterfaceService.GenerateVoiceOutputAsync(
    text: "आज गेहूं का भाव 2000 रुपये प्रति क्विंटल है",
    farmerId: "farmer123",
    cancellationToken);
```

### Managing Accessibility Settings

```csharp
var settings = new AccessibilitySettings(
    HighContrastMode: true,
    TextSize: TextSize.Large,
    ScreenReaderEnabled: true,
    KeyboardNavigationEnabled: true,
    UpdatedAt: DateTimeOffset.UtcNow);

await accessibilityService.UpdateSettingsAsync(
    farmerId: "farmer123",
    settings: settings,
    cancellationToken);
```

## Requirements Validation

This module validates the following requirements:

- **Requirement 12.1**: Voice input and output for all core features
- **Requirement 12.2**: Support for at least 15 Indian languages
- **Requirement 12.3**: Language preference persistence across sessions
- **Requirement 12.4**: Accessibility features for visual impairments
- **Requirement 12.5**: Natural-sounding voices appropriate to language/dialect
- **Requirement 12.6**: Switching between voice and text interfaces

## Testing

Property-based tests validate:
- **Property 57**: Language preference is persisted
- **Property 58**: Interface mode switching is supported

See `tests/KisanMitraAI.Tests/MultiLanguage/MultiLanguagePropertyTests.cs` for test implementation.
