using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples.ContextController;

public class ContextController : IContextController
{
    public ContextContainer Container { get; set; } = new ContextContainer();
    public IInstructionsContextService? InstructionsContextService { get; set; }
    public IToolContextService? ToolContextService { get; set; }
    public IModelContextService? ModelContextService { get; set; }
    public IMessageContextService? MessageContextService { get; set; }

    public TaskContextService TaskContextService { get; set; }
    public AgentContext CurrentContext = new AgentContext();
    public object LockObject { get; } = new object();

    public ContextController(
        TaskContextService taskContextService,
        ContextContainer contextContainer,
        IInstructionsContextService? instructionsContextService = null,
        IToolContextService? toolContextService = null,
        IModelContextService? modelContextService = null,
        IMessageContextService? messageContextService = null)
    {
        TaskContextService = taskContextService;
        Container = contextContainer;
        InstructionsContextService = instructionsContextService;
        ToolContextService = toolContextService;
        ModelContextService = modelContextService;
        MessageContextService = messageContextService;
    }

    public void SetGoal(string goal)
    {
        this.Container.Goal = goal;
    }

    public async Task<AgentContext> GetAgentContext()
    {


        List<Task> contextTasks = new List<Task>();
        await TaskContextService.GetTaskContext();
        contextTasks.Add(Task.Run(async () => await GetAgentModel()));
        contextTasks.Add(Task.Run(async () => await GetToolsContext()));
        contextTasks.Add(Task.Run(async () => await GetInstructionsContext()));
        contextTasks.Add(Task.Run(async () => await GetMessagesContext()));

        await Task.WhenAll(contextTasks);

        return CurrentContext;
    }

    public async Task GetAgentModel()
    {
        if (ModelContextService is not null)
        {
            var model = await ModelContextService.GetModelContext();
            lock (LockObject)
            {
                this.CurrentContext.Model = model;
            }
        }
    }

    public async Task GetToolsContext()
    {
        if (ToolContextService is not null)
        {
            var tools = await ToolContextService.GetToolContext();
            lock (LockObject)
            {
                this.CurrentContext.Tools = tools;
            }
        }
    }

    public async Task GetInstructionsContext()
    {
        if (InstructionsContextService is not null)
        {
            var instructions = await InstructionsContextService.GetInstructionsContext();
            lock (LockObject)
            {
                this.CurrentContext.Instructions = instructions;
            }
        }
    }

    public async Task GetMessagesContext()
    {
        if (MessageContextService is not null)
        {
            var messages = await MessageContextService.GetChatContext();
            lock (LockObject)
            {
                this.CurrentContext.ChatMessages = messages;
            }
        }
    }
}
