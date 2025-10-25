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

    private List<ChatMessage> unsavedMessages  = new List<ChatMessage>();
    public string Goal { get; set; } = string.Empty;

    public IVectorDatabase ToolStore { get; set; }

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

    public async Task<TornadoAgent> SetupAgent()
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

    private async Task UpdateGoal(string userPrompt)
    {
        if (string.IsNullOrEmpty(userPrompt))
            return;
        TornadoAgent contextAgent = new TornadoAgent(Client, ChatModel.OpenAi.Gpt5.V5);
        contextAgent.Instructions = $@"The current goal is: {Goal}  The new user prompt is {userPrompt}. Given the Latest Message Stream Determine a new goal message. 
If the message contains any information about the user's intent or desired outcome, incorporate that into the new goal.";
        contextAgent.UpdateOutputSchema(typeof(GoalMessage));
        Conversation conv = await contextAgent.RunAsync(
            appendMessages: ChatMessages.TakeLast(10).ToList());
        GoalMessage result = conv.Messages.Last().Content.ParseJson<GoalMessage>();
        Goal = result.Goal;
    }

    public async Task<Conversation> RunAgentWithContext(TornadoAgent agent, ChatMessage message, CancellationToken cancellationToken)
    {
        await UpdateGoal(message.GetMessageContent());
        List<ChatMessage> chatContext = await GetChatContext(); 
        Conversation conversation = await agent.RunAsync(appendMessages: chatContext, cancellationToken: cancellationToken);
        List<ChatMessage> newMessages = conversation.Messages.Skip(chatContext.Count).ToList();
        unsavedMessages.AddRange(newMessages);
        return conversation;
    }
}
