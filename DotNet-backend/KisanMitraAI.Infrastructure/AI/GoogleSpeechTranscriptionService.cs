using Google.Cloud.Speech.V2;
using Google.Protobuf;
using KisanMitraAI.Core.VoiceIntelligence;
using KisanMitraAI.Core.VoiceIntelligence.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.AI;

/// <summary>
/// Google Cloud Speech-to-Text V2 implementation (replaces AWS Transcribe).
/// Key advantage: accepts audio bytes directly — no S3 upload + polling cycle.
/// </summary>
public class GoogleSpeechTranscriptionService : ITranscriptionService
{
    private readonly string _projectId;
    private readonly ILogger<GoogleSpeechTranscriptionService> _logger;

    public GoogleSpeechTranscriptionService(
        IConfiguration config,
        ILogger<GoogleSpeechTranscriptionService> logger)
    {
        _projectId = config["GCP:ProjectId"] ?? "kisansetu-501110";
        _logger = logger;
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        Stream audioStream, string languageCode, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Transcribing audio with language {Language}", languageCode);

        var client = await SpeechClient.CreateAsync(cancellationToken);
        var audioBytes = await ReadStreamAsync(audioStream);

        var request = new RecognizeRequest
        {
            Recognizer = $"projects/{_projectId}/locations/global/recognizers/_",
            Content = ByteString.CopyFrom(audioBytes),
            Config = new RecognitionConfig
            {
                AutoDecodingConfig = new AutoDetectDecodingConfig(),
                LanguageCodes = { languageCode },
                Model = "latest_long",
                Features = new RecognitionFeatures
                {
                    EnableAutomaticPunctuation = true
                }
            }
        };

        var response = await client.RecognizeAsync(request, cancellationToken);

        var transcript = string.Join(" ",
            response.Results
                .SelectMany(r => r.Alternatives)
                .Select(a => a.Transcript));

        var confidence = response.Results
            .SelectMany(r => r.Alternatives)
            .Select(a => a.Confidence)
            .DefaultIfEmpty(0.9f)
            .Average();

        _logger.LogInformation("Transcribed: {Text} (confidence: {Confidence:F2})",
            transcript.Length > 100 ? transcript[..100] + "..." : transcript, confidence);

        return new TranscriptionResult(transcript, confidence, languageCode, DateTimeOffset.UtcNow);
    }

    private static async Task<byte[]> ReadStreamAsync(Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}
