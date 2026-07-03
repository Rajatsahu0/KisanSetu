namespace KisanMitraAI.Core.Workflows;

/// <summary>
/// High-level orchestrator for module-specific workflows
/// </summary>
public interface IWorkflowOrchestrator
{
    /// <summary>
    /// Executes the Krishi-Vani voice query workflow
    /// </summary>
    Task<WorkflowResult> ExecuteKrishiVaniWorkflowAsync(
        KrishiVaniWorkflowInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the Quality Grader workflow
    /// </summary>
    Task<WorkflowResult> ExecuteQualityGraderWorkflowAsync(
        QualityGraderWorkflowInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the Dhara-Analyzer soil analysis workflow
    /// </summary>
    Task<WorkflowResult> ExecuteDharaAnalyzerWorkflowAsync(
        DharaAnalyzerWorkflowInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the Sowing Oracle planting recommendation workflow
    /// </summary>
    Task<WorkflowResult> ExecuteSowingOracleWorkflowAsync(
        SowingOracleWorkflowInput input,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets real-time status updates for a workflow execution
    /// </summary>
    Task<WorkflowStatusUpdate> GetWorkflowStatusAsync(
        string executionArn,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Input for Krishi-Vani workflow
/// </summary>
public record KrishiVaniWorkflowInput(
    string FarmerId,
    string AudioS3Uri,
    string LanguageCode,
    string Dialect,
    string VoiceId);

/// <summary>
/// Input for Quality Grader workflow
/// </summary>
public record QualityGraderWorkflowInput(
    string FarmerId,
    string ImageData,
    string ProduceType,
    string Location);

/// <summary>
/// Input for Dhara-Analyzer workflow
/// </summary>
public record DharaAnalyzerWorkflowInput(
    string FarmerId,
    string DocumentData,
    string PreferredLanguage);

/// <summary>
/// Input for Sowing Oracle workflow
/// </summary>
public record SowingOracleWorkflowInput(
    string FarmerId,
    string Location,
    string CropType);

/// <summary>
/// Result of a workflow execution
/// </summary>
public record WorkflowResult(
    string ExecutionArn,
    ExecutionState Status,
    string? Output,
    string? Error,
    TimeSpan Duration);

/// <summary>
/// Real-time status update for a workflow
/// </summary>
public record WorkflowStatusUpdate(
    string ExecutionArn,
    ExecutionState Status,
    string CurrentStep,
    int ProgressPercentage,
    string? Message);
