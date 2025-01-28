namespace LlmTornado.Assistants;

/// <summary>
///     Represents a tool object of type file_search
/// </summary>
public class AssistantToolCodeInterpreter : AssistantTool
{
    /// <summary>
    ///     The code interpreter tool used by assistants with default settings
    /// </summary>
    public static readonly AssistantToolCodeInterpreter Default = new();
    
    /// <summary>
    ///     Creates a new code_interpreter type tool
    /// </summary>
    private AssistantToolCodeInterpreter()
    {
        Type = "code_interpreter";
    }
}