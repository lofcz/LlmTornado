namespace LlmTornado.Interactions;

/// <summary>
/// Status of a Gemini Interactions API interaction.
/// </summary>
public enum InteractionStatus
{
    /// <summary>Unknown or unrecognized status.</summary>
    Unknown,

    /// <summary>Interaction is still running.</summary>
    InProgress,

    /// <summary>Interaction requires client action (e.g. function results).</summary>
    RequiresAction,

    /// <summary>Interaction finished successfully.</summary>
    Completed,

    /// <summary>Interaction failed.</summary>
    Failed,

    /// <summary>Interaction was cancelled.</summary>
    Cancelled,

    /// <summary>Interaction ended without completing.</summary>
    Incomplete,

    /// <summary>Interaction exceeded its budget.</summary>
    BudgetExceeded
}
