using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace LlmTornado.Agents.ChatRuntime.RuntimeConfigurations
{
    public class HandoffAgent : RuntimeAgent
    {
        public string Description { get; set; } = "";
        public List<HandoffAgent> HandoffAgents { get; set; } = new List<HandoffAgent>();

        /// <summary>
        /// Initializes a new instance of the <see cref="HandoffAgent"/> class.
        /// </summary>
        /// <param name="client">TornadoAPI client</param>
        /// <param name="description">Description of the agent</param>
        /// <param name="model">Chat model to use</param>
        /// <param name="name">Name of the agent</param>
        /// <param name="instructions">Instructions for the agent</param>
        /// <param name="outputSchema">Output schema for the agent</param>
        /// <param name="tools">List of tools the agent can use</param>
        /// <param name="mcpServers">List of MCP servers to use</param>
        /// <param name="handoffs">List of handoff agents</param>
        /// <param name="streaming">Whether the agent supports streaming</param>
        public HandoffAgent(
            TornadoApi client,
            string description,
            ChatModel model,
            string name = "Handoff Agent",
            string instructions = "You are a helpful assistant",
            Type? outputSchema = null,
            List<Delegate>? tools = null,
            List<MCPServer>? mcpServers = null,
            List<HandoffAgent>? handoffs = null,
            bool streaming = false) : base(client, model, name, instructions, outputSchema, tools, mcpServers, streaming)
        {
            HandoffAgents = handoffs ?? new List<HandoffAgent>();
            Description = description;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandoffAgent"/> class with the specified parameters.
        /// </summary>
        /// <param name="cloneAgent">The <see cref="TornadoAgent"/> instance to clone, providing the base configuration for the new agent.</param>
        /// <param name="streaming">A value indicating whether the agent operates in streaming mode. Defaults to <see langword="false"/>.</param>
        /// <param name="handoffs">An optional list of <see cref="HandoffAgent"/> instances to associate with this agent. If not provided, an
        /// empty list is used.</param>
        /// <param name="description">An optional description of the agent. Defaults to an empty string.</param>
        public HandoffAgent(
            TornadoAgent cloneAgent,
            bool streaming = false,
            List<HandoffAgent>? handoffs = null,
            string description = "") : base(cloneAgent.Client, cloneAgent.Model, cloneAgent.Name, cloneAgent.Instructions, cloneAgent.OutputSchema, cloneAgent.Tools, cloneAgent.McpServers, streaming)
        {
            HandoffAgents = handoffs ?? new List<HandoffAgent>();
            Description = description;
        }
    }

    /// <summary>
    /// Handoff runtime configuration for managing conversations with multiple agents and handing off between them as needed.
    /// </summary>
    public class HandoffRuntimeConfiguration : IRuntimeConfiguration
    {
        public ChatRuntime Runtime { get; set; }
        public CancellationTokenSource cts { get; set; }
        public Func<ChatRuntimeEvents, ValueTask>? OnRuntimeEvent { get; set; }

        /// <summary>
        /// Current conversation being managed by the runtime configuration.
        /// </summary>
        public Conversation Conversation { get; set; }

        /// <summary>
        /// Current agent handling the conversation.
        /// </summary>
        public HandoffAgent CurrentAgent { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="HandoffRuntimeConfiguration"/> class.
        /// </summary>
        /// <param name="initialAgent">Initial Agent to start the loop with</param>
        public HandoffRuntimeConfiguration(HandoffAgent initialAgent)
        {
            CurrentAgent = initialAgent;
        }

        public async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message,  CancellationToken cancellationToken = default)
        {
            await SelectCurrentAgent(message);

            if(Conversation == null)
            {
                Conversation = await CurrentAgent.RunAsync(
               appendMessages: [message],
               streaming: CurrentAgent.Streaming,
               onAgentRunnerEvent: (sEvent) => { OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, Runtime.Id)); return Threading.ValueTaskCompleted; },
               cancellationToken: cancellationToken);
            }
            else
            {
                Conversation = await CurrentAgent.RunAsync(
               appendMessages: Conversation.Messages.ToList(),
               streaming: CurrentAgent.Streaming,
               onAgentRunnerEvent: (sEvent) => { OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, Runtime.Id)); return Threading.ValueTaskCompleted; },
               cancellationToken: cancellationToken);
            }

            return GetLastMessage();
        }

        public void ClearMessages()
        {
            Conversation.Clear();
        }

        public List<ChatMessage> GetMessages()
        {
            return Conversation.Messages.ToList();
        }

        public ChatMessage GetLastMessage()
        {
            return Conversation.Messages.LastOrDefault() ?? new ChatMessage(ChatMessageRoles.System, "No messages in conversation");
        }

        private string GenerateHandoffInstructions()
        {
            return @$"
I need you to decide if you need to handoff the conversation to another agent.
If you do, please return the agent you want to handoff to and the reason for the handoff.
If not just return CurrentAgent
Out of the following Agents which agent should we Handoff the conversation too and why? 


{{""NAME"": ""CurrentAgent"",""Instructions"":""{CurrentAgent.Instructions}""}}


{string.Join("\n\n", CurrentAgent.HandoffAgents.Select(handoff => $" {{\"NAME\": \"{handoff.Id}\",\"Handoff Reason\":\"{handoff.Description}\"}}"))}

";
        }

        private TornadoAgent CreateHandoffDecider()
        {
            string instructions = GenerateHandoffInstructions();
            TornadoAgent handoffDecider = new TornadoAgent(CurrentAgent.Client, ChatModel.OpenAi.Gpt41.V41, instructions: instructions)
            {
                Options =
            {
            ResponseFormat = AgentHandoffUtility.CreateHandoffResponseFormat(CurrentAgent.HandoffAgents.ToArray()),
            CancellationToken = cts.Token // Set the cancellation token source for the Control Agent
            }
            };
            return handoffDecider;
        }

        private string GenerateHandoffPrompt(ChatMessage? inputMessage)
        {
            string prompt = "Current Conversation:\n";
            if (Conversation != null)
            {
                foreach (ChatMessage message in Conversation.Messages)
                {
                    foreach (ChatMessagePart part in message.Parts ?? [])
                    {
                        if (part is not { Text: "" })
                        {
                            prompt += $"{message.Role}: {part.Text}\n";
                        }
                    }
                }
            }
            prompt += "\nBased on the conversation, decide if you need to handoff to another agent. If so, which one and why?\n";

            if (inputMessage != null)
                prompt += $"{inputMessage.Role}: {inputMessage.Content}\n";

            return prompt;
        }

        private List<HandoffAgent> CheckHandoffDeciderResult(Conversation handoff)
        {
            List<HandoffAgent> handoffAgents = new List<HandoffAgent>();
            if (handoff.Messages.Count > 0 && handoff.Messages.Last().Content != null)
            {
                if (handoff.Messages.Last() is { Role: ChatMessageRoles.Assistant })
                {
                    string response = handoff.Messages.Last().Content!;
                    if (response is not null)
                    {
                        List<string> selectedAgents = AgentHandoffUtility.ParseHandoffResponse(response);
                        foreach (string agent in selectedAgents)
                        {
                            HandoffAgent? handoffAgent = CurrentAgent.HandoffAgents.FirstOrDefault(a => a.Id == agent);
                            if (handoffAgent != null)
                            {
                                handoffAgents.Add(handoffAgent);
                            }
                        }
                    }
                }
            }
            return handoffAgents;
        }

        private async Task SelectCurrentAgent(ChatMessage? inputMessage)
        {
            if(CurrentAgent.HandoffAgents.Count == 0)
            {
                return; // No handoff agents available, skip selection
            }

            TornadoAgent handoffDecider = CreateHandoffDecider();

            string prompt = GenerateHandoffPrompt(inputMessage);

            Conversation handoffResult = await handoffDecider.RunAsync(prompt, cancellationToken: cts.Token);

            List<HandoffAgent> handoffAgents = CheckHandoffDeciderResult(handoffResult);

            HandoffAgent? lastAgent = CurrentAgent;

            CurrentAgent = handoffAgents.FirstOrDefault() ?? lastAgent;

            //If new agent selected, run the handoff selector again
            if (lastAgent.Id != CurrentAgent.Id)
            {
                await SelectCurrentAgent(inputMessage);
            }
        }
    }
}
