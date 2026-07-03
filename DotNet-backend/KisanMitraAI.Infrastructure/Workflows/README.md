# Workflows

This directory contains AWS Step Functions state machine definitions and orchestration services for coordinating multi-service workflows in the Kisan Mitra AI platform.

## Overview

The workflow orchestration layer provides:
- **State Machine Definitions**: JSON definitions for AWS Step Functions workflows
- **Step Function Service**: Low-level service for executing and monitoring Step Functions
- **Workflow Orchestrator**: High-level service providing module-specific workflow execution

## Workflows

### 1. Krishi-Vani Workflow (Voice Market Intelligence)

**Purpose**: Voice-based market intelligence query processing

**Flow**: 
1. TranscribeAudio - Convert audio to text using Amazon Transcribe
2. ParseQuery - Extract commodity and location using Amazon Bedrock
3. CheckClarificationNeeded - Determine if query is ambiguous
4. RetrievePrices - Get current Mandi prices from Timestream
5. GenerateResponse - Create natural language response using Bedrock
6. SynthesizeVoice - Convert response to audio using Amazon Polly
7. NotifyFarmer - Send completion notification

**Retry Policy**: 3 retries with exponential backoff (1s, 2s, 4s)

**Timeout**: 30 seconds total workflow execution time

**Error Handling**: Fallback responses for transcription, parsing, price retrieval, and synthesis failures

### 2. Quality Grader Workflow (Vision-Based Produce Grading)

**Purpose**: Analyze produce images and assign quality grades with certified prices

**Flow**:
1. UploadImage - Upload and validate image to S3
2. AnalyzeImage - Extract features using Amazon Rekognition
3. CheckImageQuality - Verify image quality is sufficient
4. ClassifyQuality - Assign grade (A, B, C, Reject)
5. CalculatePrice - Compute certified price with grade multiplier
6. StoreGradingRecord - Persist grading record to Timestream
7. NotifyFarmer - Send grading results

**Retry Policy**: 3 retries with exponential backoff (1s, 2s, 4s)

**Timeout**: 30 seconds total workflow execution time

**Error Handling**: Image quality checks with retake guidance, fallback for analysis failures

### 3. Dhara-Analyzer Workflow (Soil Health Card Digitization)

**Purpose**: Digitize Soil Health Cards and generate regenerative farming plans

**Flow**:
1. UploadDocument - Upload Soil Health Card image to S3
2. ExtractText - OCR extraction using Amazon Textract
3. ParseSoilData - Parse nutrient values from extracted text
4. ValidateSoilData - Validate nutrient ranges
5. StoreSoilData - Persist to Timestream
6. GenerateRegenerativePlan - Create 12-month plan using Bedrock Knowledge Base
7. EstimateCarbonSequestration - Calculate carbon sequestration potential
8. StorePlan - Persist plan to PostgreSQL
9. DeliverPlan - Generate text, voice, and PDF formats
10. NotifyFarmer - Send completion notification

**Retry Policy**: 3 retries with exponential backoff (1s, 2s, 4s)

**Timeout**: 30 seconds total workflow execution time

**Error Handling**: Manual verification for invalid extractions, fallback for plan generation failures

### 4. Sowing Oracle Workflow (Predictive Planting Recommendations)

**Purpose**: Generate planting recommendations based on weather and soil data

**Flow**:
1. FetchWeatherData - Get 90-day weather forecast
2. RetrieveSoilData - Get latest soil health data from Timestream
3. AnalyzePlantingWindows - Identify optimal planting windows using Bedrock
4. RecommendSeedVarieties - Match varieties to conditions
5. CalculateConfidenceScores - Score recommendations based on forecast reliability
6. CheckHighRiskWeather - Detect drought/flood risks
7. GenerateAlternatives - Suggest alternatives for high-risk conditions
8. StoreRecommendation - Persist recommendation
9. ScheduleDailyUpdate - Schedule daily recommendation updates
10. NotifyFarmer - Send recommendation

**Retry Policy**: 3 retries with exponential backoff (1s, 2s, 4s)

**Timeout**: 30 seconds total workflow execution time

**Error Handling**: Fallback for weather API failures, alternative suggestions for high-risk conditions

## Services

### IStepFunctionService

Low-level service for AWS Step Functions operations:
- `StartExecutionAsync` - Start a workflow execution
- `GetExecutionStatusAsync` - Get current execution status
- `WaitForCompletionAsync` - Poll until execution completes
- `StopExecutionAsync` - Stop a running execution

### IWorkflowOrchestrator

High-level service for module-specific workflows:
- `ExecuteKrishiVaniWorkflowAsync` - Execute voice query workflow
- `ExecuteQualityGraderWorkflowAsync` - Execute grading workflow
- `ExecuteDharaAnalyzerWorkflowAsync` - Execute soil analysis workflow
- `ExecuteSowingOracleWorkflowAsync` - Execute planting recommendation workflow
- `GetWorkflowStatusAsync` - Get real-time status updates

## Configuration

State machine ARNs should be configured in appsettings.json:

```json
{
  "AWS": {
    "StepFunctions": {
      "KrishiVaniStateMachineArn": "arn:aws:states:region:account:stateMachine:KrishiVani",
      "QualityGraderStateMachineArn": "arn:aws:states:region:account:stateMachine:QualityGrader",
      "DharaAnalyzerStateMachineArn": "arn:aws:states:region:account:stateMachine:DharaAnalyzer",
      "SowingOracleStateMachineArn": "arn:aws:states:region:account:stateMachine:SowingOracle"
    }
  }
}
```

## Usage

### Register Services

```csharp
// In Program.cs
builder.Services.AddWorkflowServices();
```

### Execute Workflow

```csharp
// Inject IWorkflowOrchestrator
public class VoiceQueryController : ControllerBase
{
    private readonly IWorkflowOrchestrator _orchestrator;

    public VoiceQueryController(IWorkflowOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost("query")]
    public async Task<IActionResult> ProcessVoiceQuery(
        [FromForm] IFormFile audioFile,
        [FromForm] string dialect)
    {
        // Upload audio to S3 first
        var audioS3Uri = await UploadAudioToS3(audioFile);

        // Execute workflow
        var input = new KrishiVaniWorkflowInput(
            FarmerId: User.GetFarmerId(),
            AudioS3Uri: audioS3Uri,
            LanguageCode: "hi-IN",
            Dialect: dialect,
            VoiceId: "Aditi");

        var result = await _orchestrator.ExecuteKrishiVaniWorkflowAsync(input);

        if (result.Status == ExecutionState.Succeeded)
        {
            return Ok(JsonSerializer.Deserialize<VoiceQueryResponse>(result.Output));
        }

        return StatusCode(500, new { error = result.Error });
    }
}
```

### Monitor Workflow Status

```csharp
// Get real-time status
var status = await _orchestrator.GetWorkflowStatusAsync(executionArn);

Console.WriteLine($"Status: {status.Status}");
Console.WriteLine($"Progress: {status.ProgressPercentage}%");
Console.WriteLine($"Current Step: {status.CurrentStep}");
```

## Error Handling

All workflows implement:
- **Retry Logic**: 3 retries with exponential backoff (1s, 2s, 4s)
- **Error States**: Dedicated error handling states for each step
- **Fallback Responses**: User-friendly error messages
- **Logging**: Detailed error logging with context

## Testing

Property-based tests validate workflow orchestration properties:
- Property 30: Services are coordinated in correct sequence
- Property 31: Failed services are retried with backoff
- Property 32: Permanent failures are logged and reported
- Property 33: Context is preserved across modules
- Property 34: Workflow status is reported

## Deployment

Workflows are deployed using AWS CDK:
1. Define state machines in CDK stacks
2. Deploy to AWS Step Functions
3. Configure ARNs in application settings
4. Test with sample inputs
