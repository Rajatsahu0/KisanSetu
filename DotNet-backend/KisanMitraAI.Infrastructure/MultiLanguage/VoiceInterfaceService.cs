namespace KisanMitraAI.Infrastructure.MultiLanguage;

using KisanMitraAI.Core.MultiLanguage;
using KisanMitraAI.Core.VoiceIntelligence;
using KisanMitraAI.Core.VoiceIntelligence.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of voice interface service that integrates with existing voice services
/// </summary>
public class VoiceInterfaceService : IVoiceInterfaceService
{
    private readonly ITranscriptionService _transcriptionService;
    private readonly IVoiceSynthesizer _voiceSynthesizer;
    private readonly ILanguagePreferenceService _languagePreferenceService;
    private readonly ILogger<VoiceInterfaceService> _logger;

    private static readonly HashSet<string> SupportedFeatures = new()
    {
        "market_intelligence",
        "quality_grading",
        "soil_analysis",
        "planting_advisory",
        "advisory_questions"
    };

    public VoiceInterfaceService(
        ITranscriptionService transcriptionService,
        IVoiceSynthesizer voiceSynthesizer,
        ILanguagePreferenceService languagePreferenceService,
        ILogger<VoiceInterfaceService> logger)
    {
        _transcriptionService = transcriptionService ?? throw new ArgumentNullException(nameof(transcriptionService));
        _voiceSynthesizer = voiceSynthesizer ?? throw new ArgumentNullException(nameof(voiceSynthesizer));
        _languagePreferenceService = languagePreferenceService ?? throw new ArgumentNullException(nameof(languagePreferenceService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VoiceInputResult> ProcessVoiceInputAsync(
        Stream audioStream,
        string farmerId,
        string feature,
        CancellationToken cancellationToken = default)
    {
        if (audioStream == null)
            throw new ArgumentNullException(nameof(audioStream));
        if (string.IsNullOrWhiteSpace(farmerId))
            throw new ArgumentException("Farmer ID cannot be null or empty", nameof(farmerId));
        if (!IsVoiceSupported(feature))
            throw new ArgumentException($"Voice interface not supported for feature: {feature}", nameof(feature));

        _logger.LogInformation(
            "Processing voice input for farmer {FarmerId}, feature {Feature}",
            farmerId, feature);

        // Get farmer's language preference
        var preference = await _languagePreferenceService.GetPreferenceAsync(farmerId, cancellationToken);
        var languageCode = GetLanguageCode(preference?.Language ?? Core.Models.Language.Hindi);

        // Transcribe audio
        var transcriptionResult = await _transcriptionService.TranscribeAsync(
            audioStream,
            languageCode,
            cancellationToken);

        // Parse intent based on feature
        var intent = DetermineIntent(feature, transcriptionResult.TranscribedText);
        var parameters = ExtractParameters(transcriptionResult.TranscribedText, feature);

        var result = new VoiceInputResult(
            transcriptionResult.TranscribedText,
            intent,
            parameters,
            transcriptionResult.Confidence);

        _logger.LogInformation(
            "Voice input processed for farmer {FarmerId}: Intent={Intent}, Confidence={Confidence}",
            farmerId, intent, transcriptionResult.Confidence);

        return result;
    }

    public async Task<Stream> GenerateVoiceOutputAsync(
        string text,
        string farmerId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Text cannot be null or empty", nameof(text));
        if (string.IsNullOrWhiteSpace(farmerId))
            throw new ArgumentException("Farmer ID cannot be null or empty", nameof(farmerId));

        _logger.LogInformation("Generating voice output for farmer {FarmerId}", farmerId);

        // Get farmer's language preference
        var preference = await _languagePreferenceService.GetPreferenceAsync(farmerId, cancellationToken);
        var language = preference?.Language ?? Core.Models.Language.Hindi;
        var languageCode = GetLanguageCode(language);
        var voiceId = GetVoiceId(language, preference?.Dialect);

        // Synthesize speech
        var (audioStream, s3Url) = await _voiceSynthesizer.SynthesizeAsync(
            text,
            voiceId,
            languageCode,
            cancellationToken);

        _logger.LogInformation(
            "Voice output generated for farmer {FarmerId} in language {Language}, S3 URL: {S3Url}",
            farmerId, language, s3Url);

        return audioStream;
    }

    public bool IsVoiceSupported(string feature)
    {
        return SupportedFeatures.Contains(feature?.ToLowerInvariant() ?? string.Empty);
    }

    private string GetLanguageCode(Core.Models.Language language)
    {
        return language switch
        {
            Core.Models.Language.Hindi => "hi-IN",
            Core.Models.Language.Tamil => "ta-IN",
            Core.Models.Language.Telugu => "te-IN",
            Core.Models.Language.Bengali => "bn-IN",
            Core.Models.Language.Marathi => "mr-IN",
            Core.Models.Language.Gujarati => "gu-IN",
            Core.Models.Language.Kannada => "kn-IN",
            Core.Models.Language.Malayalam => "ml-IN",
            Core.Models.Language.Odia => "or-IN",
            Core.Models.Language.Punjabi => "pa-IN",
            Core.Models.Language.Assamese => "as-IN",
            Core.Models.Language.Urdu => "ur-IN",
            _ => "hi-IN" // Default to Hindi
        };
    }

    private string GetVoiceId(Core.Models.Language language, Core.Models.Dialect? dialect)
    {
        // Map language and dialect to appropriate Polly voice IDs
        // This is a simplified mapping - in production, you'd have more sophisticated voice selection
        return language switch
        {
            Core.Models.Language.Hindi => "Aditi",
            Core.Models.Language.Tamil => "Aditi", // Polly doesn't have all Indian languages, fallback to Hindi
            Core.Models.Language.Telugu => "Aditi",
            Core.Models.Language.Bengali => "Aditi",
            Core.Models.Language.Marathi => "Aditi",
            Core.Models.Language.Gujarati => "Aditi",
            Core.Models.Language.Kannada => "Aditi",
            Core.Models.Language.Malayalam => "Aditi",
            _ => "Aditi" // Default to Aditi (Hindi voice)
        };
    }

    private string DetermineIntent(string feature, string transcribedText)
    {
        // Simple intent determination based on feature
        return feature.ToLowerInvariant() switch
        {
            "market_intelligence" => "query_price",
            "quality_grading" => "grade_produce",
            "soil_analysis" => "analyze_soil",
            "planting_advisory" => "get_recommendation",
            "advisory_questions" => "ask_question",
            _ => "unknown"
        };
    }

    private Dictionary<string, string> ExtractParameters(string transcribedText, string feature)
    {
        // Simplified parameter extraction
        // In production, this would use NLP/LLM to extract structured parameters
        var parameters = new Dictionary<string, string>
        {
            ["raw_text"] = transcribedText,
            ["feature"] = feature
        };

        return parameters;
    }
}
