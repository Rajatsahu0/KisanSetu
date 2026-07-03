using KisanMitraAI.Core.VoiceIntelligence;
using KisanMitraAI.Core.VoiceIntelligence.Models;
using KisanMitraAI.Core.Advisory;
using KisanMitraAI.Core.AI;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace KisanMitraAI.Infrastructure.AI;

/// <summary>
/// FAST voice query pipeline:
///   Transcribe (aggressive poll) → Dictionary parse (&lt;10ms) → DynamoDB prices → Template response (&lt;1ms) → Direct Polly (200ms)
/// 
/// Previous: 20-30s total (batch Transcribe + 2 Bedrock calls + S3 round-trips)
/// Now:      3-8s total (fast Transcribe + dictionary + template + direct Polly)
/// 
/// Falls back to Bedrock for general questions (not price queries).
/// </summary>
public class VoiceQueryHandler : IVoiceQueryHandler
{
    private readonly ITranscriptionService _transcriptionService;
    private readonly IQueryParser _queryParser;
    private readonly IPriceRetriever _priceRetriever;
    private readonly IResponseGenerator _responseGenerator;
    private readonly IVoiceSynthesizer _voiceSynthesizer;
    private readonly IKnowledgeBaseService _knowledgeBaseService;
    private readonly ILogger<VoiceQueryHandler> _logger;
    private readonly Dictionary<string, string> _dialectToLanguageMap;

    public VoiceQueryHandler(
        ITranscriptionService transcriptionService,
        IQueryParser queryParser,
        IPriceRetriever priceRetriever,
        IResponseGenerator responseGenerator,
        IVoiceSynthesizer voiceSynthesizer,
        IKnowledgeBaseService knowledgeBaseService,
        GeminiService geminiService,
        GeminiModelConfig modelConfig,
        ILogger<VoiceQueryHandler> logger)
    {
        _transcriptionService = transcriptionService;
        _queryParser = queryParser;
        _priceRetriever = priceRetriever;
        _responseGenerator = responseGenerator;
        _voiceSynthesizer = voiceSynthesizer;
        _knowledgeBaseService = knowledgeBaseService;
        _logger = logger;

        _dialectToLanguageMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Bundelkhandi", "hi-IN" }, { "Bhojpuri", "hi-IN" }, { "Marwari", "hi-IN" },
            { "Awadhi", "hi-IN" }, { "Braj", "hi-IN" }, { "Magahi", "hi-IN" },
            { "Maithili", "hi-IN" }, { "Chhattisgarhi", "hi-IN" }, { "Haryanvi", "hi-IN" },
            { "Rajasthani", "hi-IN" }, { "Hindi", "hi-IN" }, { "hi-IN", "hi-IN" },
            { "English", "en-IN" }, { "en-IN", "en-IN" },
            { "Tamil", "ta-IN" }, { "ta-IN", "ta-IN" },
            { "Telugu", "te-IN" }, { "te-IN", "te-IN" },
            { "Bengali", "bn-IN" }, { "bn-IN", "bn-IN" },
            { "Marathi", "mr-IN" }, { "mr-IN", "mr-IN" },
            { "Gujarati", "gu-IN" }, { "gu-IN", "gu-IN" },
            { "Kannada", "kn-IN" }, { "kn-IN", "kn-IN" },
            { "Malayalam", "ml-IN" }, { "ml-IN", "ml-IN" },
            { "Punjabi", "pa-IN" }, { "pa-IN", "pa-IN" }
        };
    }

    public async Task<VoiceQueryResponse> ProcessVoiceQueryAsync(
        Stream audioStream, string dialect, string farmerId, CancellationToken cancellationToken)
    {
        if (audioStream == null) throw new ArgumentNullException(nameof(audioStream));
        if (string.IsNullOrWhiteSpace(dialect)) throw new ArgumentException("Dialect required", nameof(dialect));
        if (string.IsNullOrWhiteSpace(farmerId)) throw new ArgumentException("Farmer ID required", nameof(farmerId));

        var sw = System.Diagnostics.Stopwatch.StartNew();
        _logger.LogInformation("FAST voice pipeline started for farmer {FarmerId}, dialect {Dialect}", farmerId, dialect);

        try
        {
            ValidateAudioStream(audioStream);
            var languageCode = GetLanguageCode(dialect);

            // Step 1: Transcribe (aggressive polling — 3-8s)
            var t1 = sw.ElapsedMilliseconds;
            var transcription = await _transcriptionService.TranscribeAsync(audioStream, languageCode, cancellationToken);
            _logger.LogInformation("PERF: Transcribe={Ms}ms, Text={Text}", sw.ElapsedMilliseconds - t1, transcription.TranscribedText);

            // Step 2: Parse query — dictionary lookup (<10ms) or Bedrock fallback (2-5s)
            var t2 = sw.ElapsedMilliseconds;
            var parsed = await _queryParser.ParseQueryAsync(transcription.TranscribedText,
                $"Farmer: {farmerId}, Dialect: {dialect}", cancellationToken);
            _logger.LogInformation("PERF: Parse={Ms}ms, Commodity={Commodity}, Location={Location}",
                sw.ElapsedMilliseconds - t2, parsed.Commodity, parsed.Location);

            // Handle general questions (not price queries) via knowledge base
            if (parsed.RequiresClarification || string.IsNullOrEmpty(parsed.Commodity))
            {
                return await HandleGeneralQuestionAsync(transcription, parsed, dialect, languageCode, farmerId, sw, cancellationToken);
            }

            // Step 3: Get prices from DynamoDB (<50ms)
            var t3 = sw.ElapsedMilliseconds;
            var prices = await _priceRetriever.GetCurrentPricesAsync(parsed.Commodity, parsed.Location, cancellationToken);
            var priceList = prices.ToArray();
            _logger.LogInformation("PERF: Prices={Ms}ms, Count={Count}", sw.ElapsedMilliseconds - t3, priceList.Length);

            // Step 4: Generate response — template (<1ms) or Bedrock fallback (2-5s)
            var t4 = sw.ElapsedMilliseconds;
            var responseText = await _responseGenerator.GenerateResponseAsync(parsed, priceList, dialect, cancellationToken);
            _logger.LogInformation("PERF: Response={Ms}ms", sw.ElapsedMilliseconds - t4);

            // Step 5: Synthesize voice — direct Polly (200-500ms)
            var t5 = sw.ElapsedMilliseconds;
            string audioUrl = "";
            try
            {
                var (_, url) = await _voiceSynthesizer.SynthesizeAsync(responseText, "", languageCode, cancellationToken);
                audioUrl = url;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Voice synthesis failed, returning text-only");
            }
            _logger.LogInformation("PERF: Polly={Ms}ms", sw.ElapsedMilliseconds - t5);

            _logger.LogInformation("FAST voice pipeline COMPLETE: Total={Ms}ms for farmer {FarmerId}",
                sw.ElapsedMilliseconds, farmerId);

            return new VoiceQueryResponse(
                transcription.TranscribedText, priceList, audioUrl, responseText,
                transcription.Confidence, dialect, DateTimeOffset.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Voice pipeline error after {Ms}ms for farmer {FarmerId}", sw.ElapsedMilliseconds, farmerId);
            throw;
        }
    }

    private async Task<VoiceQueryResponse> HandleGeneralQuestionAsync(
        TranscriptionResult transcription, ParsedQuery parsed, string dialect, string languageCode,
        string farmerId, System.Diagnostics.Stopwatch sw, CancellationToken cancellationToken)
    {
        _logger.LogInformation("General question detected, using knowledge base");

        var kbResponse = await _knowledgeBaseService.QueryKnowledgeBaseAsync(
            transcription.TranscribedText, $"Farmer: {farmerId}, Dialect: {dialect}", 5, cancellationToken);

        string audioUrl = "";
        try
        {
            var (_, url) = await _voiceSynthesizer.SynthesizeAsync(kbResponse.Answer, "", languageCode, cancellationToken);
            audioUrl = url;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Voice synthesis failed for KB response");
        }

        _logger.LogInformation("General question pipeline COMPLETE: Total={Ms}ms", sw.ElapsedMilliseconds);

        return new VoiceQueryResponse(
            transcription.TranscribedText, Array.Empty<Core.Models.MandiPrice>(),
            audioUrl, kbResponse.Answer, transcription.Confidence, dialect, DateTimeOffset.UtcNow);
    }

    private void ValidateAudioStream(Stream audioStream)
    {
        if (!audioStream.CanRead) throw new ArgumentException("Audio stream must be readable");
        if (audioStream.Length == 0) throw new ArgumentException("Audio stream cannot be empty");
        if (audioStream.Length > 10 * 1024 * 1024) throw new ArgumentException("Audio file exceeds 10 MB limit");
    }

    private string GetLanguageCode(string dialect)
    {
        return _dialectToLanguageMap.GetValueOrDefault(dialect, "hi-IN");
    }
}
