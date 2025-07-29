namespace LlmTornado.Docs.Console;

public class ConsoleOutputViewModel
{
    /// <summary>
    /// Time the output was received
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Severity of the output
    /// </summary>
    public ConsoleSeverity Severity { get; set; }
    
    /// <summary>
    /// Console output
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

public enum ConsoleSeverity
{
    Debug,
    Info,
    Warning,
    Error,
}