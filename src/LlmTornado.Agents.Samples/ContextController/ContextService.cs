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

public class ContextService
{
    public List<Tool> AvailableTools { get; set; } = new List<Tool>();
    public List<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    private Dictionary<string, ChatModel> modelLibrary { get; set; } = new Dictionary<string, ChatModel>();
    /// <summary>
    /// Set the name and the description of the model to be used in the context
    /// </summary>
    private Dictionary<string, string> modelDescriptions { get; set; } = new Dictionary<string, string>();

    private List<ChatMessage> unsavedMessages  = new List<ChatMessage>();
    
    public List<string> GoalHistory { get; set; } = new List<string>();

    public string? Goal => GoalHistory.LastOrDefault();

    public IVectorDatabase ToolStore { get; set; } // Will need this when tools exceed 50

    public IVectorDatabase LongTermMemory { get; set; }

    public CompressedContextStore CompressedContextStore { get; set; } = new CompressedContextStore();

    public TornadoApi Client { get; set; }

    public ContextService(TornadoApi client)
    {
        Client = client;
    }

    public async Task<List<Tool>> GetToolContext()
    {
        throw new NotImplementedException();
    }

    public async Task<List<ChatMessage>> GetChatContext()
    {
        throw new NotImplementedException();
    }

    public async Task<string> GetInstructionsContext()
    {
        throw new NotImplementedException();
    }

    public async Task<ChatModel> GetModelContext()
    {
        throw new NotImplementedException();
    }

    public async Task ReviewToDo(ChatMessage message)
    {
        throw new NotImplementedException();
    }

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

    public async Task<TornadoAgent> GetAgent()
    {
        TornadoAgent agent = new TornadoAgent(Client, await GetModelContext());

        List<Tool> tools = await GetToolContext();
        foreach (var tool in tools)
        {
            if(tool.RemoteTool != null)
                agent.AddMcpTools([tool]);
            else
                agent.AddTornadoTool(tool);
        }

        agent.Instructions = await GetInstructionsContext();

        return agent;
    }

    private struct GoalMessage
    {
        public string Goal { get; set; }
    }

    private async Task UpdateGoal(ChatMessage newMessage)
    {
        string? userPrompt = newMessage.GetMessageContent();
        if (string.IsNullOrEmpty(userPrompt))
            return;

        TornadoAgent contextAgent = new TornadoAgent(Client, ChatModel.OpenAi.Gpt5.V5);
        contextAgent.Instructions = $@"The current goal is: {Goal ?? "N/A"}  The new user prompt is {userPrompt}. Given the Latest Message Stream Determine a new goal message. 
If the message contains any information about the user's intent or desired outcome, incorporate that into the new goal.";
        contextAgent.UpdateOutputSchema(typeof(GoalMessage));

        Conversation conv = await contextAgent.RunAsync(
            appendMessages: ChatMessages.TakeLast(10).ToList());

        GoalMessage result = conv.Messages.Last().Content.ParseJson<GoalMessage>();

        GoalHistory.Add(result.Goal);
    }

    private async Task StoreNewMessages(List<ChatMessage> newMessages)
    {
        unsavedMessages.AddRange(newMessages);
    }

    public async Task<Conversation> RunAgentWithContext(ChatMessage message, CancellationToken cancellationToken)
    {
        //Update Goal
        await UpdateGoal(message);

        //Generate Chat Context
        List<ChatMessage> chatContext = await GetChatContext();

        //Create Agent
        TornadoAgent tornadoAgent = await GetAgent();

        // Run Agent
        Conversation conversation = await tornadoAgent.RunAsync(appendMessages: chatContext, cancellationToken: cancellationToken);

        // Track unsaved messages
        List<ChatMessage> newMessages = conversation.Messages.Skip(chatContext.Count).ToList();
        await StoreNewMessages(newMessages);

        //Return conversation
        return conversation;
    }
}
