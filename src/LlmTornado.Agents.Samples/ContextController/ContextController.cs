using System;
using System.Collections.Generic;
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
        AgentContext context = new AgentContext();

        this.Container.CurrentTask = await TaskContextService.GetTaskContext();

        if (ModelContextService is not null)
            context.Model = await ModelContextService.GetModelContext();

        if (ToolContextService is not null)
            context.Tools = await ToolContextService.GetToolContext();

        if (InstructionsContextService is not null)
            context.Instructions = await InstructionsContextService.GetInstructionsContext();

        if (MessageContextService is not null)
            context.ChatMessages = await MessageContextService.GetChatContext();

        return context;
    }
}
