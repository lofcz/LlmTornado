namespace LlmTornado.Agents.Samples.DataModels;

public struct TaskRequest
{
    public string Task { get; set; }
    public string Context { get; set; }
    public TaskRequest(string task, string context = "")
    {
        Task = task;
        Context = context;
    }
}

