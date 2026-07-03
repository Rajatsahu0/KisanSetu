using Google.Cloud.AIPlatform.V1;
using Google.Protobuf;
using KisanMitraAI.Core.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KisanMitraAI.Infrastructure.AI;

/// <summary>
/// Central Vertex AI Gemini client wrapper.
/// Replaces all Amazon Bedrock calls with Gemini generateContent API.
/// Supports text-only and multimodal (image + text) requests.
/// </summary>
public class GeminiService
{
    private readonly string _projectId;
    private readonly string _location;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(IConfiguration config, ILogger<GeminiService> logger)
    {
        _projectId = config["GCP:ProjectId"] ?? "kisansetu-501110";
        _location = config["GCP:VertexAI:Location"] ?? "us-central1";
        _logger = logger;
    }

    /// <summary>
    /// Generate text content using Gemini (replaces Bedrock Converse/InvokeModel for text)
    /// </summary>
    public async Task<string> GenerateContentAsync(
        string modelId,
        string prompt,
        float temperature = 0.7f,
        int maxTokens = 2048,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var modelName = $"projects/{_projectId}/locations/{_location}/publishers/google/models/{modelId}";

        var request = new GenerateContentRequest
        {
            Model = modelName,
            Contents =
            {
                new Content
                {
                    Role = "user",
                    Parts = { new Part { Text = prompt } }
                }
            },
            GenerationConfig = new GenerationConfig
            {
                Temperature = temperature,
                MaxOutputTokens = maxTokens
            }
        };

        _logger.LogInformation("Gemini request: model={Model}, promptLen={Len}", modelId, prompt.Length);

        var response = await client.GenerateContentAsync(request, cancellationToken);

        var text = response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";
        _logger.LogInformation("Gemini response: model={Model}, responseLen={Len}", modelId, text.Length);

        return text;
    }

    /// <summary>
    /// Generate content with image input (replaces Bedrock Vision for quality grading, soil card)
    /// </summary>
    public async Task<string> GenerateContentWithImageAsync(
        string modelId,
        string prompt,
        byte[] imageBytes,
        string mimeType = "image/jpeg",
        float temperature = 0.1f,
        int maxTokens = 500,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient();
        var modelName = $"projects/{_projectId}/locations/{_location}/publishers/google/models/{modelId}";

        var request = new GenerateContentRequest
        {
            Model = modelName,
            Contents =
            {
                new Content
                {
                    Role = "user",
                    Parts =
                    {
                        new Part
                        {
                            InlineData = new Blob
                            {
                                MimeType = mimeType,
                                Data = ByteString.CopyFrom(imageBytes)
                            }
                        },
                        new Part { Text = prompt }
                    }
                }
            },
            GenerationConfig = new GenerationConfig
            {
                Temperature = temperature,
                MaxOutputTokens = maxTokens
            }
        };

        _logger.LogInformation("Gemini Vision request: model={Model}, imageSize={Size}B", modelId, imageBytes.Length);

        var response = await client.GenerateContentAsync(request, cancellationToken);

        var text = response.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "";
        _logger.LogInformation("Gemini Vision response: model={Model}, responseLen={Len}", modelId, text.Length);

        return text;
    }

    private PredictionServiceClient CreateClient()
    {
        return new PredictionServiceClientBuilder
        {
            Endpoint = $"{_location}-aiplatform.googleapis.com"
        }.Build();
    }
}
