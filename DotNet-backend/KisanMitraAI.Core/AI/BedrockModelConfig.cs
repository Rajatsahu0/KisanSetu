namespace KisanMitraAI.Core.AI;

/// <summary>
/// Multi-model configuration for enterprise-grade latency optimization.
/// Maps each feature to the optimal Bedrock model based on:
/// - Latency requirement (real-time vs batch)
/// - Quality requirement (structured JSON vs free text)
/// - Token budget (small parse vs large plan generation)
/// 
/// Model Tiers:
///   Nova Lite  — ~1-2s response, good for parsing/classification, 300K context
///   Nova Pro   — ~3-6s response, best for complex reasoning/planning, 300K context
/// </summary>
public class BedrockModelConfig
{
    /// <summary>
    /// Model for voice query parsing + response (latency-critical, simple task)
    /// </summary>
    public string VoiceParseModel { get; set; } = "us.amazon.nova-lite-v1:0";

    /// <summary>
    /// Model for advisory knowledge base Q&A from web/app (quality-critical, complex reasoning)
    /// </summary>
    public string AdvisoryModel { get; set; } = "us.amazon.nova-pro-v1:0";

    /// <summary>
    /// Model for voice general questions — farming Q&A via text-query endpoint
    /// Uses Pro because text-query skips Transcribe (total latency still 3-5s)
    /// </summary>
    public string VoiceGeneralModel { get; set; } = "us.amazon.nova-lite-v1:0";

    /// <summary>
    /// Model for regenerative plan generation (quality-critical, large output)
    /// </summary>
    public string PlanGenerationModel { get; set; } = "us.amazon.nova-pro-v1:0";

    /// <summary>
    /// Model for planting window analysis (quality-critical, structured JSON)
    /// </summary>
    public string PlantingWindowModel { get; set; } = "us.amazon.nova-pro-v1:0";

    /// <summary>
    /// Model for seed variety recommendations (quality-critical, domain expertise)
    /// </summary>
    public string SeedRecommendationModel { get; set; } = "us.amazon.nova-pro-v1:0";

    /// <summary>
    /// Model for historical insights generation (batch-tolerant, analytical)
    /// </summary>
    public string InsightsModel { get; set; } = "us.amazon.nova-lite-v1:0";

    /// <summary>
    /// Model for query parsing only — commodity/location extraction (latency-critical, simple)
    /// </summary>
    public string QueryParserModel { get; set; } = "us.amazon.nova-lite-v1:0";

    /// <summary>
    /// Model for response text generation (latency-critical, simple formatting)
    /// </summary>
    public string ResponseGeneratorModel { get; set; } = "us.amazon.nova-lite-v1:0";

    /// <summary>
    /// Model for quality grading vision analysis (Nova Lite — vision-capable, fast)
    /// Sends produce image to AI for produce-aware quality assessment
    /// </summary>
    public string QualityGradingModel { get; set; } = "us.amazon.nova-lite-v1:0";

    /// <summary>
    /// Model for soil card extraction fallback (Nova Lite — vision-capable)
    /// Used when Textract confidence is low or PDF has multiple pages
    /// </summary>
    public string SoilCardExtractionModel { get; set; } = "us.amazon.nova-lite-v1:0";
}
