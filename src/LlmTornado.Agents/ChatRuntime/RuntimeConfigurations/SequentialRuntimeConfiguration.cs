using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime.RuntimeConfigurations
{
    public class SequentialRuntimeAgent : TornadoAgent
    {
        public string SequentialInstructions = """
            You are part of a sequential chain of agents. You will receive a message, 
            and you must respond to it as best as you can. Once you have responded, 
            the next agent in the chain will receive your response as input. 
            Make sure your response is clear and concise, as it will be used by the next agent.
            """;

        public SequentialRuntimeAgent(
            TornadoApi client, 
            ChatModel model,
            string name = "Assistant",
            string instructions = "You are a helpful assistant",
            string? sequentialInstructions = null,
            Type? outputSchema = null,
            List<Delegate>? tools = null,
            List<MCPServer>? mcpServers = null
            ) : base(client, model, name, instructions, outputSchema, tools, mcpServers)
        {
            SequentialInstructions = sequentialInstructions ?? SequentialInstructions;
        }

        public SequentialRuntimeAgent(
            TornadoAgent cloneAgent,
            string? sequentialInstructions = null) : base(cloneAgent.Client, cloneAgent.Model, cloneAgent.Name, cloneAgent.Instructions, cloneAgent.OutputSchema, cloneAgent.Tools, cloneAgent.McpServers)
        {
            SequentialInstructions = sequentialInstructions ?? SequentialInstructions;
        }
    }

    public class SequentialRuntimeConfiguration : IRuntimeConfiguration
    {
        public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();
        public Conversation? Conversation { get; set; }
        
        public List<SequentialRuntimeAgent> Agents { get; set; } = new List<SequentialRuntimeAgent>();

        public SequentialRuntimeConfiguration(SequentialRuntimeAgent[] agents)
        {
            Agents.AddRange(agents);
        }

        public async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            bool isFirstAgent = true;
            foreach (var agent in Agents)
            {
                if (Conversation == null)
                {
                    Conversation = await agent.RunAsync(appendMessages: [new ChatMessage(Code.ChatMessageRoles.User, agent.SequentialInstructions), message], cancellationToken: cancellationToken);
                    isFirstAgent = false;
                }
                else
                {
                    Conversation.AddUserMessage(agent.SequentialInstructions);

                    if (isFirstAgent)
                    {
                        Conversation.AppendMessage(message);
                        isFirstAgent = false;
                    }

                    Conversation = await agent.RunAsync(appendMessages: Conversation.Messages.ToList(), cancellationToken: cancellationToken);
                }
            }
            
            return Conversation?.Messages.LastOrDefault() ?? new ChatMessage();
        }

        public List<ChatMessage> GetMessages()
        {
            return Conversation?.Messages.ToList() ?? new List<ChatMessage>();
        }

        public void ClearMessages()
        {
            Conversation?.Clear();
        }
    }
}
