namespace LlmTornado.Agents.Samples.DataModels;

public struct TerminalCommandResult
{
    public string Command { get; set; }
    public string Output { get; set; }
    public int ExitCode { get; set; }
    public bool Success { get; set; }
    public TerminalCommandResult(string command, string output, int exitCode, bool success)
    {
        Command = command;
        Output = output;
        ExitCode = exitCode;
        Success = success;
    }
}

