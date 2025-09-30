namespace LlmTornado.Agents.Samples.DataModels;

public struct AgentExecutionResults
{
    public string OriginalTask { get; set; }
    public string WebSearchResults { get; set; }
    public string FileOperationResults { get; set; }
    public string CodeExecutionResults { get; set; }
    public string TerminalResults { get; set; }
    public string[] ActionsPerformed { get; set; }
    public AgentExecutionResults(string originalTask, string webResults, string fileResults, string codeResults, string terminalResults, string[] actionsPerformed)
    {
        OriginalTask = originalTask;
        WebSearchResults = webResults;
        FileOperationResults = fileResults;
        CodeExecutionResults = codeResults;
        TerminalResults = terminalResults;
        ActionsPerformed = actionsPerformed;
    }
}

