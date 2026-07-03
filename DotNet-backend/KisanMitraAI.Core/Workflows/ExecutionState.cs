namespace KisanMitraAI.Core.Workflows;

/// <summary>
/// Workflow execution states
/// </summary>
public enum ExecutionState
{
    Running,
    Succeeded,
    Failed,
    TimedOut,
    Aborted
}
