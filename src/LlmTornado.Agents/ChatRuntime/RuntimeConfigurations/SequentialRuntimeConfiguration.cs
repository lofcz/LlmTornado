using LlmTornado.Agents.DataModels;
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
        /// <summary>
        /// Instructions to be added before each message for the sequential agent.
        /// </summary>
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
            List<MCPServer>? mcpServers = null,
            bool streaming = false
            ) : base(client, model, name, instructions, outputSchema, tools, mcpServers, streaming)
        {
            SequentialInstructions = sequentialInstructions ?? SequentialInstructions;
        }

        public SequentialRuntimeAgent(
            TornadoAgent cloneAgent,
            string? sequentialInstructions = null,
            bool streaming = false) : base(cloneAgent.Client, cloneAgent.Model, cloneAgent.Name, cloneAgent.Instructions, cloneAgent.OutputSchema, cloneAgent.Tools, cloneAgent.McpServers, streaming)
        {
            SequentialInstructions = sequentialInstructions ?? SequentialInstructions;
            Streaming = streaming;
        }
    }

    /// <summary>
    /// Runtime configuration for managing a sequence of agents that process messages one after the other.
    /// </summary>
    public class SequentialRuntimeConfiguration : IRuntimeConfiguration
    {
        public ChatRuntime Runtime { get; set; }
        public Func<ChatRuntimeEvents, ValueTask>? OnRuntimeEvent { get; set; }

        public CancellationTokenSource cts { get; set; } = new CancellationTokenSource();

        /// <summary>
        /// Current conversation state being managed by the sequential agents.
        /// </summary>
        public Conversation? Conversation { get; set; }

        /// <summary>
        /// List of agents that will process messages sequentially.
        /// </summary>
        public List<SequentialRuntimeAgent> Agents { get; set; } = new List<SequentialRuntimeAgent>();

        /// <summary>
        /// Initializes a new instance of the <see cref="SequentialRuntimeConfiguration"/> class with the specified agents.
        /// </summary>
        /// <param name="agents">Agents to run in order</param>
        public SequentialRuntimeConfiguration(SequentialRuntimeAgent[] agents)
        {
            Agents = agents.ToList();
        }

        public void OnRuntimeInitialized()
        {

        }

        public void CancelRuntime()
        {
            cts.Cancel();
        }
        public async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            bool isFirstAgent = true;
            foreach (var agent in Agents)
            {
                if (Conversation == null)
                {
                    Conversation = await agent.RunAsync(
                        appendMessages: [new ChatMessage(Code.ChatMessageRoles.User, agent.SequentialInstructions), message], 
                        streaming:agent.Streaming, 
                        onAgentRunnerEvent:(sEvent) =>
                        {
                            OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, Runtime.Id));
                            return Threading.ValueTaskCompleted;
                        }, 
                        cancellationToken: cancellationToken
                        );
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

                    Conversation = await agent.RunAsync(
                        appendMessages: Conversation.Messages.ToList(), 
                        streaming: agent.Streaming,
                        onAgentRunnerEvent: (sEvent) =>
                        {
                            OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, Runtime.Id));
                            return Threading.ValueTaskCompleted;
                        }, 
                        cancellationToken: cancellationToken
                        );
                }
            }
            
            return Conversation?.Messages.LastOrDefault() ?? new ChatMessage();
        }

        public ChatMessage GetLastMessage()
        {
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
