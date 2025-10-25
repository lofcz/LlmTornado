using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples.ContextController;

public class AgentContext
{
    public List<Tool>? Tools { get; set; } = new List<Tool>();
    public ChatModel? Model { get; set; }
    public List<ChatMessage>? ChatMessages { get; set; } = new List<ChatMessage>();
    public string? Instructions { get; set; } = string.Empty;
}

public class ContextService : IContextService
{   
    public IInstructionsContextService? InstructionsContextService { get; set; }
    public IToolContextService? ToolContextService { get; set; }
    public IModelContextService? ModelContextService { get; set; }
    public IMessageContextService? MessageContextService { get; set; }

    public ContextService(
        IInstructionsContextService? instructionsContextService = null, 
        IToolContextService? toolContextService = null, 
        IModelContextService? modelContextService = null, 
        IMessageContextService? messageContextService = null)
    {
        InstructionsContextService = instructionsContextService;
        ToolContextService = toolContextService;
        ModelContextService = modelContextService;
        MessageContextService = messageContextService;
    }

    public async Task<AgentContext> GetAgentContext()
    {
        AgentContext context = new AgentContext();

        if(ModelContextService is not null)
            context.Model = await ModelContextService.GetModelContext();

        if(ToolContextService is not null)
            context.Tools = await ToolContextService.GetToolContext();

        if(InstructionsContextService is not null)
            context.Instructions = await InstructionsContextService.GetInstructionsContext();

        if(MessageContextService is not null)
            context.ChatMessages = await MessageContextService.GetChatContext();

        return context;
    }
}
