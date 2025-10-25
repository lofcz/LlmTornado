using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.VectorDatabases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples.ContextController;

public interface IToolContextService
{
    public Task<List<Tool>> GetToolContext();
}

public interface IModelContextService
{
    public Task<ChatModel> GetModelContext();
}

public interface IMessageContextService
{
    public Task<List<ChatMessage>> GetChatContext();
}

public interface IInstructionsContextService
{
    public Task<string> GetInstructionsContext();
}

public interface IContextService
{
    public Task<AgentContext> GetAgentContext();
}


public class ToolContextService : IToolContextService
{
    public IVectorDatabase ToolStore { get; set; } // Will need this when tools exceed 50
    public List<Tool> AvailableTools { get; set; } = new List<Tool>();

    public async Task<List<Tool>> GetToolContext()
    {
        throw new NotImplementedException();
    }
}

public class ModelContextService : IModelContextService
{
    private Dictionary<string, ChatModel> modelLibrary { get; set; } = new Dictionary<string, ChatModel>();
    /// <summary>
    /// Set the name and the description of the model to be used in the context
    /// </summary>
    private Dictionary<string, string> modelDescriptions { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Adds a model to the internal library for selection
    /// </summary>
    /// <param name="name"> Maybe use a unique identifier for the model so model isnt bias towards its owns API </param>
    /// <param name="model"> Chat model to add</param>
    /// <param name="description">Description on why to select this model</param>
    public void AddModelToLibrary(string name, ChatModel model, string description)
    {
        modelLibrary[name] = model;
        modelDescriptions[name] = description;
    }

    public async Task<ChatModel> GetModelContext()
    {
        throw new NotImplementedException();
    }
}

public class MessageContextService : IMessageContextService
{
    public List<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    private List<ChatMessage> unsavedMessages = new List<ChatMessage>();
    public IVectorDatabase LongTermMemory { get; set; }
    public CompressedContextStore CompressedContextStore { get; set; } = new CompressedContextStore();

    public async Task<List<ChatMessage>> GetChatContext()
    {
        throw new NotImplementedException();
    }

    private async Task StoreNewMessages(List<ChatMessage> newMessages)
    {
        unsavedMessages.AddRange(newMessages);
    }

}

public class InstructionContextService : IInstructionsContextService
{
    public TornadoApi Client { get; set; }
    public List<string> GoalHistory { get; set; } = new List<string>();
    public string? Goal => GoalHistory.LastOrDefault();

    public InstructionContextService(TornadoApi api)
    {
        Client = api;
    }

    public async Task<string> GetInstructionsContext()
    {
        throw new NotImplementedException();
    }
    private struct GoalMessage
    {
        public string Goal { get; set; }
    }

    private async Task UpdateGoal(ChatMessage newMessage, List<ChatMessage> chatHistory)
    {
        string? userPrompt = newMessage.GetMessageContent();
        if (string.IsNullOrEmpty(userPrompt))
            return;

        TornadoAgent contextAgent = new TornadoAgent(Client, ChatModel.OpenAi.Gpt5.V5);
        contextAgent.Instructions = $@"The current goal is: {Goal ?? "N/A"}  The new user prompt is {userPrompt}. Given the Latest Message Stream Determine a new goal message. 
If the message contains any information about the user's intent or desired outcome, incorporate that into the new goal.";
        contextAgent.UpdateOutputSchema(typeof(GoalMessage));

        Conversation conv = await contextAgent.RunAsync(
            appendMessages: chatHistory.TakeLast(10).ToList());

        GoalMessage result = conv.Messages.Last().Content.ParseJson<GoalMessage>();

        GoalHistory.Add(result.Goal);
    }
}

public class AgentContext
{
    public List<Tool> Tools { get; set; } = new List<Tool>();
    public ChatModel Model { get; set; }
    public List<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    public string Instructions { get; set; } = string.Empty;
}


public class ContextService : IContextService
{   
    public TornadoApi Client { get; set; }

    public IInstructionsContextService InstructionsContextService { get; set; }
    public IToolContextService ToolContextService { get; set; }
    public IModelContextService ModelContextService { get; set; }
    public IMessageContextService MessageContextService { get; set; }

    public ContextService(
        TornadoApi client, 
        IInstructionsContextService instructionsContextService, 
        IToolContextService toolContextService, 
        IModelContextService modelContextService, 
        IMessageContextService messageContextService)
    {
        Client = client;
        InstructionsContextService = instructionsContextService;
        ToolContextService = toolContextService;
        ModelContextService = modelContextService;
        MessageContextService = messageContextService;
    }

    public TornadoApi GetClient()
    {
        return Client;
    }

    public async Task<AgentContext> GetAgentContext()
    {
        AgentContext context = new AgentContext();

        context.Model = await ModelContextService.GetModelContext();

        context.Tools = await ToolContextService.GetToolContext();

        context.Instructions = await InstructionsContextService.GetInstructionsContext();

        context.ChatMessages = await MessageContextService.GetChatContext();

        return context;
    }
}
