namespace LlmTornado.Agents.ChatRuntime.Orchestration;

/// <summary>
/// Configuration options for orchestration behaviour.
/// </summary>
public class OrchestrationOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether debug logging is enabled for orchestration processes.
    /// When enabled, detailed diagnostic information about advancement checking and state transitions will be logged to the console.
    /// </summary>
    public bool Debug { get; set; }
}

