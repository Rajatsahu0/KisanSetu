using System.Threading.RateLimiting;
using CoreWCF;
using CoreWCF.Channels;
using CoreWCF.Configuration;
using CoreWCF.Description;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using Google.Cloud.Vision.V1;
using KisanMitraAI.API.Swagger;
using KisanMitraAI.Core.AI;
using KisanMitraAI.Core.Authentication;
using KisanMitraAI.Core.Authorization;
using KisanMitraAI.Core.GovernmentIntegration;
using KisanMitraAI.Infrastructure.AI;
using KisanMitraAI.Infrastructure.GovernmentIntegration;
using KisanMitraAI.Infrastructure.Repositories.Firestore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ─── GCP Configuration ───
var projectId = builder.Configuration["GCP:ProjectId"] ?? "kisansetu-501110";
Console.WriteLine($"Running on GCP — Project: {projectId}");

// ─── Firebase Admin SDK ───
if (FirebaseApp.DefaultInstance == null)
{
    FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.GetApplicationDefault(), ProjectId = projectId });
}

// ─── GCP Services ───
builder.Services.AddSingleton(_ => FirestoreDb.Create(projectId));
builder.Services.AddSingleton(_ => StorageClient.Create());
builder.Services.AddSingleton(_ => ImageAnnotatorClient.Create());
builder.Services.AddSingleton<GeminiService>();

// ─── Model Configuration ───
builder.Services.Configure<GeminiModelConfig>(builder.Configuration.GetSection("GeminiModels"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GeminiModelConfig>>().Value);

// ─── Firestore Configuration ───
builder.Services.Configure<FirestoreConfiguration>(builder.Configuration.GetSection("Firestore"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<FirestoreConfiguration>>().Value);

// ─── Memory Cache & HttpClient ───
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// ─── Swagger ───
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "KisanSetu API", Version = "v1" });
    options.OperationFilter<FileUploadOperationFilter>();
    options.MapType<IFormFile>(() => new OpenApiSchema { Type = "string", Format = "binary" });
});

// ─── Controllers ───
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ─── CORS ───
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// ─── CoreWCF SOAP ───
builder.Services.AddServiceModelServices();
builder.Services.AddServiceModelMetadata();
builder.Services.AddSingleton<IServiceBehavior, UseRequestHeadersForMetadataAddressBehavior>();
builder.Services.AddScoped<IGovernmentIntegrationService, GovernmentIntegrationService>();

// ─── Authentication (Firebase JWT) ───
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{projectId}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{projectId}",
            ValidateAudience = true,
            ValidAudience = projectId,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                
                // If JWKS download fails (common on localhost/cold start), allow request through
                // The JwtClaimsExtractionMiddleware will extract claims as fallback
                if (context.Exception.Message.Contains("No security keys") ||
                    context.Exception.Message.Contains("IDX10500") ||
                    context.Exception.Message.Contains("IDX20803"))
                {
                    logger.LogWarning("JWT JWKS unavailable — using fallback claims extraction. Error: {Error}", 
                        context.Exception.Message);
                    context.NoResult(); // Don't fail auth, let middleware handle it
                }
                else
                {
                    logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                }
                return Task.CompletedTask;
            }
        };
    });

// ─── Auth Service (Firebase-backed) ───
builder.Services.AddScoped<ICognitoAuthService, CognitoAuthService>();
builder.Services.AddScoped<JwtClaimsExtractor>();

// ─── Firestore Repositories ───
builder.Services.AddScoped<KisanMitraAI.Infrastructure.Repositories.PostgreSQL.IFarmRepository,
    KisanMitraAI.Infrastructure.Repositories.Firestore.FirestoreFarmRepository>();
builder.Services.AddScoped<KisanMitraAI.Infrastructure.Repositories.PostgreSQL.IAuditLogRepository,
    KisanMitraAI.Infrastructure.Repositories.Firestore.FirestoreAuditLogRepository>();
builder.Services.AddScoped<KisanMitraAI.Infrastructure.Repositories.DynamoDB.IUserProfileRepository,
    KisanMitraAI.Infrastructure.Repositories.Firestore.FirestoreUserProfileRepository>();
builder.Services.AddScoped<KisanMitraAI.Core.VoiceIntelligence.IVoiceQueryHistoryRepository,
    KisanMitraAI.Infrastructure.Repositories.Firestore.FirestoreVoiceQueryHistoryRepository>();
builder.Services.AddScoped<KisanMitraAI.Infrastructure.Repositories.Timestream.ISoilDataRepository,
    KisanMitraAI.Infrastructure.Repositories.Firestore.FirestoreSoilDataRepository>();
builder.Services.AddScoped<KisanMitraAI.Infrastructure.Repositories.Timestream.IGradingHistoryRepository,
    KisanMitraAI.Infrastructure.Repositories.Firestore.FirestoreGradingHistoryRepository>();
builder.Services.AddScoped<KisanMitraAI.Infrastructure.Repositories.Timestream.IMandiPriceRepository,
    KisanMitraAI.Infrastructure.Repositories.Firestore.FirestoreMandiPricesRepository>();

// ─── Storage ───
builder.Services.AddScoped<KisanMitraAI.Infrastructure.Storage.S3.IS3StorageService,
    KisanMitraAI.Infrastructure.Storage.GCS.GcsStorageService>();
builder.Services.AddScoped<KisanMitraAI.Infrastructure.Vision.IS3StorageService>(sp =>
{
    var storage = sp.GetRequiredService<KisanMitraAI.Infrastructure.Storage.S3.IS3StorageService>();
    return new KisanMitraAI.Infrastructure.Vision.GcsVisionStorageAdapter(storage);
});

// ─── AI Advisory ───
builder.Services.AddScoped<KisanMitraAI.Core.Advisory.IKnowledgeBaseService,
    KisanMitraAI.Infrastructure.Advisory.DirectBedrockKnowledgeBaseService>();

// ─── Quality Grading (Vision + Gemini) ───
builder.Services.AddScoped<KisanMitraAI.Core.QualityGrading.IImageUploadHandler,
    KisanMitraAI.Infrastructure.Vision.ImageUploadHandler>();
builder.Services.AddScoped<KisanMitraAI.Infrastructure.AI.LiveMandiPriceService>();
builder.Services.AddScoped<KisanMitraAI.Infrastructure.Vision.BedrockVisionGrader>(sp =>
{
    var gemini = sp.GetRequiredService<GeminiService>();
    var storageService = sp.GetRequiredService<KisanMitraAI.Infrastructure.Storage.S3.IS3StorageService>();
    var modelConfig = sp.GetRequiredService<GeminiModelConfig>();
    var logger = sp.GetRequiredService<ILogger<KisanMitraAI.Infrastructure.Vision.BedrockVisionGrader>>();
    // Cast to Vision.IS3StorageService — GcsStorageService satisfies both interfaces via duck typing
    var visionStorage = new KisanMitraAI.Infrastructure.Vision.GcsVisionStorageAdapter(storageService);
    return new KisanMitraAI.Infrastructure.Vision.BedrockVisionGrader(gemini, visionStorage, modelConfig, logger);
});
builder.Services.AddScoped<KisanMitraAI.Core.QualityGrading.IImageAnalyzer>(sp =>
{
    var visionClient = sp.GetRequiredService<ImageAnnotatorClient>();
    var storageService = sp.GetRequiredService<KisanMitraAI.Infrastructure.Storage.S3.IS3StorageService>();
    var logger = sp.GetRequiredService<ILogger<KisanMitraAI.Infrastructure.Vision.ImageAnalyzer>>();
    var visionGrader = sp.GetRequiredService<KisanMitraAI.Infrastructure.Vision.BedrockVisionGrader>();
    var visionStorage = new KisanMitraAI.Infrastructure.Vision.GcsVisionStorageAdapter(storageService);
    return new KisanMitraAI.Infrastructure.Vision.ImageAnalyzer(visionClient, visionStorage, logger, visionGrader);
});
builder.Services.AddScoped<KisanMitraAI.Core.QualityGrading.IQualityClassifier,
    KisanMitraAI.Infrastructure.Vision.QualityClassifier>();
builder.Services.AddScoped<KisanMitraAI.Core.QualityGrading.IPriceCalculator,
    KisanMitraAI.Infrastructure.Vision.PriceCalculator>();
builder.Services.AddScoped<KisanMitraAI.Core.QualityGrading.IGradingRecordStore,
    KisanMitraAI.Infrastructure.Vision.GradingRecordStore>();
builder.Services.AddScoped<KisanMitraAI.Core.VoiceIntelligence.IPriceRetriever>(sp =>
{
    var liveService = sp.GetRequiredService<KisanMitraAI.Infrastructure.AI.LiveMandiPriceService>();
    var repository = sp.GetRequiredService<KisanMitraAI.Infrastructure.Repositories.Timestream.IMandiPriceRepository>();
    var logger = sp.GetRequiredService<ILogger<KisanMitraAI.Infrastructure.AI.PriceRetriever>>();
    return new KisanMitraAI.Infrastructure.AI.PriceRetriever(liveService, repository, logger);
});

// ─── Soil Analysis (Document AI + Gemini Vision) ───
builder.Services.AddScoped<KisanMitraAI.Core.SoilAnalysis.IDocumentUploadHandler>(sp =>
{
    var storage = sp.GetRequiredService<KisanMitraAI.Infrastructure.Storage.S3.IS3StorageService>();
    var logger = sp.GetRequiredService<ILogger<KisanMitraAI.Infrastructure.SoilAnalysis.DocumentUploadHandler>>();
    return new KisanMitraAI.Infrastructure.SoilAnalysis.DocumentUploadHandler(storage, logger);
});
builder.Services.AddScoped<KisanMitraAI.Infrastructure.SoilAnalysis.BedrockSoilCardExtractor>(sp =>
{
    var gemini = sp.GetRequiredService<GeminiService>();
    var storage = sp.GetRequiredService<KisanMitraAI.Infrastructure.Storage.S3.IS3StorageService>();
    var modelConfig = sp.GetRequiredService<GeminiModelConfig>();
    var logger = sp.GetRequiredService<ILogger<KisanMitraAI.Infrastructure.SoilAnalysis.BedrockSoilCardExtractor>>();
    return new KisanMitraAI.Infrastructure.SoilAnalysis.BedrockSoilCardExtractor(gemini, storage, modelConfig, logger);
});
builder.Services.AddScoped<KisanMitraAI.Core.SoilAnalysis.ITextExtractor>(sp =>
{
    var storage = sp.GetRequiredService<KisanMitraAI.Infrastructure.Storage.S3.IS3StorageService>();
    var geminiExtractor = sp.GetRequiredService<KisanMitraAI.Infrastructure.SoilAnalysis.BedrockSoilCardExtractor>();
    var config = sp.GetRequiredService<IConfiguration>();
    var logger = sp.GetRequiredService<ILogger<KisanMitraAI.Infrastructure.SoilAnalysis.TextExtractor>>();
    return new KisanMitraAI.Infrastructure.SoilAnalysis.TextExtractor(storage, geminiExtractor, config, logger);
});
builder.Services.AddScoped<KisanMitraAI.Core.SoilAnalysis.ISoilDataParser,
    KisanMitraAI.Infrastructure.SoilAnalysis.SoilDataParser>();
builder.Services.AddScoped<KisanMitraAI.Core.SoilAnalysis.IRegenerativePlanGenerator,
    KisanMitraAI.Infrastructure.SoilAnalysis.RegenerativePlanGenerator>();
builder.Services.AddScoped<KisanMitraAI.Core.SoilAnalysis.ICarbonEstimator,
    KisanMitraAI.Infrastructure.SoilAnalysis.CarbonEstimator>();

// ─── Planting Advisory (Gemini + Weather API) ───
builder.Services.AddScoped<KisanMitraAI.Core.PlantingAdvisory.IWeatherDataCollector>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var cache = sp.GetRequiredService<IMemoryCache>();
    var logger = sp.GetRequiredService<ILogger<KisanMitraAI.Infrastructure.PlantingAdvisory.ThreeLayerWeatherCollector>>();
    return new KisanMitraAI.Infrastructure.PlantingAdvisory.ThreeLayerWeatherCollector(
        httpClientFactory.CreateClient(), cache, logger);
});
builder.Services.AddScoped<KisanMitraAI.Core.PlantingAdvisory.ISoilDataRetriever>(sp =>
{
    var soilDataRepo = sp.GetRequiredService<KisanMitraAI.Infrastructure.Repositories.Timestream.ISoilDataRepository>();
    var s3Storage = sp.GetRequiredService<KisanMitraAI.Infrastructure.Storage.S3.IS3StorageService>();
    var logger = sp.GetRequiredService<ILogger<KisanMitraAI.Infrastructure.PlantingAdvisory.SoilDataRetrieverAdapter>>();
    return new KisanMitraAI.Infrastructure.PlantingAdvisory.SoilDataRetrieverAdapter(soilDataRepo, s3Storage, logger);
});
builder.Services.AddScoped<KisanMitraAI.Core.PlantingAdvisory.IPlantingWindowAnalyzer,
    KisanMitraAI.Infrastructure.PlantingAdvisory.PlantingWindowAnalyzer>();
builder.Services.AddScoped<KisanMitraAI.Core.PlantingAdvisory.ISeedVarietyRecommender,
    KisanMitraAI.Infrastructure.PlantingAdvisory.DirectBedrockSeedVarietyRecommender>();
builder.Services.AddScoped<KisanMitraAI.Core.PlantingAdvisory.IConfidenceScorer,
    KisanMitraAI.Infrastructure.PlantingAdvisory.ConfidenceScorer>();

// ─── Voice Intelligence (Google Speech + Gemini) ───
builder.Services.AddScoped<KisanMitraAI.Core.VoiceIntelligence.ITranscriptionService,
    KisanMitraAI.Infrastructure.AI.GoogleSpeechTranscriptionService>();
builder.Services.AddScoped<KisanMitraAI.Core.VoiceIntelligence.IVoiceSynthesizer,
    KisanMitraAI.Infrastructure.AI.NoOpVoiceSynthesizer>();
builder.Services.AddScoped<KisanMitraAI.Core.VoiceIntelligence.IQueryParser>(sp =>
{
    var gemini = sp.GetRequiredService<GeminiService>();
    var modelConfig = sp.GetRequiredService<GeminiModelConfig>();
    var logger = sp.GetRequiredService<ILogger<FastQueryParser>>();
    return new FastQueryParser(gemini, modelConfig, logger);
});
builder.Services.AddScoped<KisanMitraAI.Core.VoiceIntelligence.IResponseGenerator>(sp =>
{
    var gemini = sp.GetRequiredService<GeminiService>();
    var modelConfig = sp.GetRequiredService<GeminiModelConfig>();
    var logger = sp.GetRequiredService<ILogger<FastResponseGenerator>>();
    return new FastResponseGenerator(gemini, modelConfig, logger);
});
builder.Services.AddScoped<KisanMitraAI.Core.VoiceIntelligence.IVoiceQueryHandler>(sp =>
{
    var transcription = sp.GetRequiredService<KisanMitraAI.Core.VoiceIntelligence.ITranscriptionService>();
    var queryParser = sp.GetRequiredService<KisanMitraAI.Core.VoiceIntelligence.IQueryParser>();
    var priceRetriever = sp.GetRequiredService<KisanMitraAI.Core.VoiceIntelligence.IPriceRetriever>();
    var responseGenerator = sp.GetRequiredService<KisanMitraAI.Core.VoiceIntelligence.IResponseGenerator>();
    var voiceSynthesizer = sp.GetRequiredService<KisanMitraAI.Core.VoiceIntelligence.IVoiceSynthesizer>();
    var gemini = sp.GetRequiredService<GeminiService>();
    var modelConfig = sp.GetRequiredService<GeminiModelConfig>();
    var voiceKbLogger = sp.GetRequiredService<ILogger<VoiceKnowledgeBaseService>>();
    var voiceKb = new VoiceKnowledgeBaseService(gemini, modelConfig, voiceKbLogger);
    var logger = sp.GetRequiredService<ILogger<VoiceQueryHandler>>();
    return new VoiceQueryHandler(transcription, queryParser, priceRetriever, responseGenerator,
        voiceSynthesizer, voiceKb, gemini, modelConfig, logger);
});

// ─── Grounded Voice Data Provider ───
builder.Services.AddScoped<KisanMitraAI.Core.VoiceIntelligence.IVoiceDataProvider>(sp =>
{
    var weatherCollector = sp.GetRequiredService<KisanMitraAI.Core.PlantingAdvisory.IWeatherDataCollector>();
    var soilRepo = sp.GetRequiredService<KisanMitraAI.Infrastructure.Repositories.Timestream.ISoilDataRepository>();
    var profileRepo = sp.GetRequiredService<KisanMitraAI.Infrastructure.Repositories.DynamoDB.IUserProfileRepository>();
    var logger = sp.GetRequiredService<ILogger<KisanMitraAI.Infrastructure.AI.GroundedVoiceDataProvider>>();
    return new KisanMitraAI.Infrastructure.AI.GroundedVoiceDataProvider(weatherCollector, soilRepo, profileRepo, logger);
});

// ─── EF Core (InMemory for dev) ───
builder.Services.AddDbContext<KisanMitraAI.Infrastructure.Data.KisanMitraDbContext>(options =>
{
    options.UseInMemoryDatabase("KisanMitraAI");
});

// ─── Authorization ───
builder.Services.AddKisanMitraAuthorization();

// ─── Rate Limiting ───
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("farmer-rate-limit", context =>
    {
        var farmerId = context.User.GetFarmerId();
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: farmerId ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            errorCode = "RATE_LIMIT_EXCEEDED",
            message = "Too many requests. Please try again later.",
            timestamp = DateTimeOffset.UtcNow
        }, cancellationToken: cancellationToken);
    };
});

var app = builder.Build();

// ─── Middleware Pipeline ───
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<KisanMitraAI.API.Middleware.JwtClaimsExtractionMiddleware>();
app.UseAuthorization();
app.MapControllers();

// ─── Health/Test Endpoints ───
app.MapGet("/test", () => new
{
    status = "ok",
    message = "KisanSetu is working!",
    platform = "Google Cloud Run",
    timestamp = DateTime.UtcNow
});

app.MapGet("/api/v1/test", () => new
{
    status = "ok",
    message = "KisanSetu API endpoint is working!",
    timestamp = DateTime.UtcNow
});

// ─── CoreWCF SOAP (government integration) ───
app.UseServiceModel(serviceBuilder =>
{
    serviceBuilder.AddService<GovernmentIntegrationService>(serviceOptions =>
    {
        serviceOptions.DebugBehavior.IncludeExceptionDetailInFaults = app.Environment.IsDevelopment();
    });
    serviceBuilder.AddServiceEndpoint<GovernmentIntegrationService, IGovernmentIntegrationService>(
        new BasicHttpBinding(BasicHttpSecurityMode.None),
        "/GovernmentIntegration.svc");
    var smb = app.Services.GetRequiredService<ServiceMetadataBehavior>();
    smb.HttpGetEnabled = true;
});

app.Run();
