namespace LlmTornado.Agents.Samples.DataModels;

public class ExecutableOutputResult
{
    private bool executionCompleted = false;

    public string Output { get; set; }
    public string Error { get; set; }
    public bool ExecutionCompleted { get => executionCompleted; set => executionCompleted = value; }

    public ExecutableOutputResult() { }

    public ExecutableOutputResult(string output, string error, bool completed)
    {
        Output = output;
        Error = error;
        ExecutionCompleted = completed;
    }
}

