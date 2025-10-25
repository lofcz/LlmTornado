using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using System.Collections.Generic;
using System.ComponentModel;

namespace LlmTornado.Agents.Samples.ContextController;

public struct TaskList
{
    [Description("A list of tasks to be completed.")]
    public string[] Tasks { get; set; }
}

public class TaskContextService 
{
    private TornadoApi _client { get; set; }
    private ContextContainer _contextContainer { get; set; }
    public string CurrentTask { get; set; } = "";
    public List<List<string>> TaskHistory { get; set; } = new List<List<string>>();
    public List<string> TaskQueue { get; set; } = new List<string>();

    public TaskContextService(TornadoApi api, ContextContainer contextContainer)
    {
        _client = api;
        _contextContainer = contextContainer;
    }

    public async Task<string> GetTaskContext()
    {
        if (TaskQueue.Count == 0)
        {
            await CreateTaskList();
        }
        _contextContainer.CurrentTask = TaskQueue.FirstOrDefault() ?? "";
        return await Task.FromResult(_contextContainer.CurrentTask);
    }

    private async Task CreateTaskList()
    {
        TornadoAgent contextAgent = new TornadoAgent(_client, ChatModel.OpenAi.Gpt5.V5Mini, outputSchema: typeof(TaskList));
        contextAgent.Instructions = $@"You are an expert Task Orchestrator for an Agentic system. Use the provided information to create the best list of Task to complete the goal.";

        string taskContext = _contextContainer.Goal ?? throw new InvalidOperationException("No Goal Defined");

        Conversation conv = await contextAgent.RunAsync(taskContext);

        TaskList result = conv.Messages.Last().Content.ParseJson<TaskList>();

        TaskHistory.Add(TaskQueue);

        TaskQueue.Clear();

        TaskQueue.AddRange(result.Tasks);
    }

    private async Task UpdateTaskList()
    {
        var newMessage = _contextContainer.ChatMessages.LastOrDefault();
        string? userPrompt = newMessage?.GetMessageContent();
        if (string.IsNullOrEmpty(userPrompt))
            return;

        TornadoAgent contextAgent = new TornadoAgent(_client, ChatModel.OpenAi.Gpt5.V5Mini, outputSchema:typeof(TaskList));
        contextAgent.Instructions = $@"You are an expert Task Orchestrator for an Agentic system. Use the goal and the provided information to create the best list of Task to complete the goal.
The current goal is: {_contextContainer.Goal ?? "N/A"} Given the incomplete Task Queue, The completed Task, and the state of the current goal,
sort, and if required, add to the task list to help prioritize completing the goal.";

        string taskContext = $"Current Task Queue: {string.Join(", ", TaskQueue)}\n Completed Tasks: {string.Join(", ", TaskHistory)}\n New User Message: {userPrompt}";

        Conversation conv = await contextAgent.RunAsync(taskContext);

        TaskList result = conv.Messages.Last().Content.ParseJson<TaskList>();

        TaskHistory.Add(TaskQueue);

        TaskQueue.Clear();

        TaskQueue.AddRange(result.Tasks);
    }
}
