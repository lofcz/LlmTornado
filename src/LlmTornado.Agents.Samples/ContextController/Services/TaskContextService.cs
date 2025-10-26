using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using System.Collections.Generic;
using System.ComponentModel;

namespace LlmTornado.Agents.Samples.ContextController;

public struct TaskList
{
    [Description("A list of tasks to be completed.")]
    public string[] Tasks { get; set; }
}

public struct ToDoMarkDown
{
    [Description("A markdown file with a todo list")]
    public string Markdown { get; set; }
}

public class TaskContextService 
{
    private TornadoApi _client { get; set; }
    private ContextContainer _contextContainer { get; set; }
    public string CurrentTask { get; set; } = "";
    public List<List<string>> TaskHistory { get; set; } = new List<List<string>>();
    public List<string> TaskQueue { get; set; } = new List<string>();

    public string ToDoMd { get; set; }

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
        else
        {
            await UpdateTaskList();
        }

        _contextContainer.CurrentTask = TaskQueue.FirstOrDefault() ?? "";

        return _contextContainer.CurrentTask;
    }

    private async Task CreateTaskList()
    {
        TornadoAgent contextAgent = new TornadoAgent(_client, ChatModel.OpenAi.Gpt5.V5Mini, outputSchema: typeof(TaskList));
        contextAgent.Instructions = $@"You are an expert Task Orchestrator for an Agentic system. Use the provided information to create the best list of Task to complete the goal 5-8 task.";
        _contextContainer.Goal = _contextContainer.ChatMessages.FindLast(m=>m.Role == ChatMessageRoles.User)?.GetMessageContent();
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

        TornadoAgent contextAgent = new TornadoAgent(_client ,ChatModel.OpenAi.Gpt5.V5Mini, outputSchema:typeof(TaskList));
        contextAgent.Instructions = $@"You are an expert Task Orchestrator for an Agentic system. Use the goal and the provided information to create the best list of Task to complete the goal.
The current goal is: {_contextContainer.Goal ?? "N/A"} Given the incomplete Task Queue, The completed Task, and the state of the current goal,
sort, and if required, add to the task list to help prioritize completing the goal.";

        string taskContext = $@"
Current Task Queue: {string.Join(", ", TaskQueue)}
Completed Tasks: {string.Join(", ", TaskHistory)}
New User Message: {userPrompt}

TODO.md:
{ToDoMd}
";

        Conversation conv = await contextAgent.RunAsync(taskContext, appendMessages: _contextContainer.ChatMessages);

        TaskList result = conv.Messages.Last().Content.ParseJson<TaskList>();

        TaskHistory.Add(TaskQueue);

        TaskQueue.Clear();

        TaskQueue.AddRange(result.Tasks);

        await UpdateToDoMd();
    }

    private async Task CreateToDoMd()
    {
        TornadoAgent contextAgent = new TornadoAgent(_client, ChatModel.OpenAi.Gpt5.V5Mini, outputSchema: typeof(ToDoMarkDown));
        contextAgent.Instructions = $@"Produce Markdown text with all the current task in a check off list. Also, Include a Description, Notes, and examples of how each step should be completed.";

        string taskContext = $"Current Task Queue: {string.Join(", ", TaskQueue)}\n  Current goal: {_contextContainer.Goal}";

        Conversation conv = await contextAgent.RunAsync(taskContext);

        ToDoMarkDown result = conv.Messages.Last().Content.ParseJson<ToDoMarkDown>();

        if(result.Markdown != null)
        {
            ToDoMd = result.Markdown;
            File.WriteAllText("TODO.md", ToDoMd);
        }
    }

    private async Task UpdateToDoMd()
    {
        if(string.IsNullOrEmpty(ToDoMd))
        {
            await CreateToDoMd();
            return;
        }
        TornadoAgent contextAgent = new TornadoAgent(_client, ChatModel.OpenAi.Gpt5.V5Mini, outputSchema: typeof(ToDoMarkDown));
        contextAgent.Instructions = $@"Produce Updated Markdown text for the current ToDo.md text that is given. Update and check off task that are completed and update/add notes and task accordingly. ";

        string taskContext = $"Current Task Queue: {string.Join(", ", TaskQueue)}\n Completed Tasks: {string.Join(", ", TaskHistory)}\n Current goal: {_contextContainer.Goal}";

        Conversation conv = await contextAgent.RunAsync(taskContext);

        ToDoMarkDown result = conv.Messages.Last().Content.ParseJson<ToDoMarkDown>();

        if (result.Markdown != null)
        {
            ToDoMd = result.Markdown;
            File.WriteAllText("TODO.md", ToDoMd);
        }
    }
}
