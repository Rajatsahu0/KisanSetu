namespace KisanMitraAI.Core.AI;

/// <summary>
/// Multi-model configuration for Vertex AI Gemini.
/// Maps each feature to the optimal model based on:
/// - Latency requirement (real-time vs batch)
/// - Quality requirement (structured JSON vs free text)
/// - Token budget (small parse vs large plan generation)
/// 
/// Model Tiers:
///   Gemini 2.5 Flash — fast, cheap, good for parsing/classification
///   Gemini 2.5 Pro   — best reasoning/planning, higher quality
/// </summary>
public class GeminiModelConfig
{
    public string VoiceParseModel { get; set; } = "gemini-2.5-flash";
    public string AdvisoryModel { get; set; } = "gemini-2.5-pro";
    public string VoiceGeneralModel { get; set; } = "gemini-2.5-flash";
    public string PlanGenerationModel { get; set; } = "gemini-2.5-pro";
    public string PlantingWindowModel { get; set; } = "gemini-2.5-pro";
    public string SeedRecommendationModel { get; set; } = "gemini-2.5-pro";
    public string InsightsModel { get; set; } = "gemini-2.5-flash";
    public string QueryParserModel { get; set; } = "gemini-2.5-flash";
    public string ResponseGeneratorModel { get; set; } = "gemini-2.5-flash";
    public string QualityGradingModel { get; set; } = "gemini-2.5-flash";
    public string SoilCardExtractionModel { get; set; } = "gemini-2.5-flash";
}
