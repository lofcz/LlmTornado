using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using LlmTornado.VectorDatabases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Samples.ContextController;

public class ContextService
{
    public List<Tool> AvailableTools { get; set; } = new List<Tool>();
    public List<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    private List<ChatMessage> unsavedMessages  = new List<ChatMessage>();
    public string Goal{ get; set; } = string.Empty;

    public IVectorDatabase ToolStore { get; set; }
    public IVectorDatabase LongTermMemory { get; set; }

    public CompressedContextStore CompressedContextStore { get; set; } = new CompressedContextStore();

    public TornadoApi Client { get; set; }

    public TornadoAgent ContextAgent { get; set; }

    public ContextService(TornadoApi client)
    {
        Client = client;
        ContextAgent = new TornadoAgent(Client, ChatModel.OpenAi.Gpt5.V5);
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

    public async Task<Conversation> RunAgentWithContext(TornadoAgent agent, CancellationToken cancellationToken)
    {
        List<ChatMessage> chatContext = await GetChatContext(); 
        Conversation conversation = await agent.RunAsync(appendMessages: chatContext, cancellationToken: cancellationToken);
        List<ChatMessage> newMessages = conversation.Messages.Skip(chatContext.Count).ToList();
        ChatMessages.AddRange(newMessages);
        return conversation;
    }
}
