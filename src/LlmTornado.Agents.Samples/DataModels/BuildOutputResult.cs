namespace LlmTornado.Agents.Samples.DataModels;
public class BuildOutputResult
{
    public string Output { get; set; }
    public string Error { get; set; }
    public bool BuildCompleted { get; set; }

    public BuildOutputResult() { }

    public BuildOutputResult(string output, string error, bool completed)
    {
        Output = output;
        Error = error;
        BuildCompleted = completed;
    }
}