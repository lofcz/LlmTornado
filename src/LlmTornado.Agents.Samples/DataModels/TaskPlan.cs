namespace LlmTornado.Agents.Samples.DataModels;

public struct TaskPlan
{
    public string OriginalTask { get; set; }
    public string[] RequiredAgents { get; set; }
    public string ExecutionPlan { get; set; }
    public string[] StepDescriptions { get; set; }
    public TaskPlan(string originalTask, string[] requiredAgents, string executionPlan, string[] stepDescriptions)
    {
        OriginalTask = originalTask;
        RequiredAgents = requiredAgents;
        ExecutionPlan = executionPlan;
        StepDescriptions = stepDescriptions;
    }
}

