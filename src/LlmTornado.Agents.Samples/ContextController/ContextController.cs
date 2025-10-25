using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples.ContextController;

public class ContextController : IContextController
{
    public ContextContainer ContextContainer { get; set; } = new ContextContainer();
    public IInstructionsContextService? InstructionsContextService { get; set; }
    public IToolContextService? ToolContextService { get; set; }
    public IModelContextService? ModelContextService { get; set; }
    public IMessageContextService? MessageContextService { get; set; }

    public TaskContextService TaskContextService { get; set; }

    public ContextController(
        TaskContextService taskContextService,
        IInstructionsContextService? instructionsContextService = null,
        IToolContextService? toolContextService = null,
        IModelContextService? modelContextService = null,
        IMessageContextService? messageContextService = null)
    {
        TaskContextService = taskContextService;
        InstructionsContextService = instructionsContextService;
        ToolContextService = toolContextService;
        ModelContextService = modelContextService;
        MessageContextService = messageContextService;
    }

    public void SetGoal(string goal)
    {
        this.ContextContainer.Goal = goal;
    }

    public async Task<AgentContext> GetAgentContext()
    {
        AgentContext context = new AgentContext();

        this.ContextContainer.CurrentTask = await TaskContextService.GetTaskContext();

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
