using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LlmTornado.Agents.ChatRuntime.RuntimeConfigurations
{
    public class HandoffAgent : TornadoAgent
    {
        public string Description { get; set; } = "";
        public List<HandoffAgent> HandoffAgents { get; set; } = new List<HandoffAgent>();
        public HandoffAgent(
            TornadoApi client,
            string description,
            ChatModel model,
            string name = "Handoff Agent",
            string instructions = "You are a helpful assistant",
            Type? outputSchema = null,
            List<Delegate>? tools = null,
            List<MCPServer>? mcpServers = null,
            List<HandoffAgent>? handoffs = null) : base(client, model, name, instructions, outputSchema, tools, mcpServers)
        {
            HandoffAgents = handoffs ?? new List<HandoffAgent>();
            Description = description;
        }

        public HandoffAgent(
            TornadoAgent cloneAgent,
            List<HandoffAgent>? handoffs = null) : base(cloneAgent.Client, cloneAgent.Model, cloneAgent.Name, cloneAgent.Instructions, cloneAgent.OutputSchema, cloneAgent.Tools, cloneAgent.McpServers)
        {
            HandoffAgents = handoffs ?? new List<HandoffAgent>();
        }
    }

    internal class HandoffRuntimeConfiguration : IRuntimeConfiguration
    {
        public CancellationTokenSource cts { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<ChatMessage> Conversation { get; set; } = new List<ChatMessage>();
        public HandoffAgent CurrentAgent { get; set; }

        public HandoffRuntimeConfiguration(HandoffAgent initialAgent)
        {
            CurrentAgent = initialAgent;
        }

        public async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            this.Conversation.Add(message);

            ConcurrentBag<ChatMessage> bag = new ConcurrentBag<ChatMessage>(GetMessages());
            List <HandoffAgent> handoffAgents = await SelectCurrentAgent(message);
            List<Task> agentTask = new List<Task>();

            foreach (HandoffAgent agent in handoffAgents)
            {
                agentTask.Add(Task.Run(async () => { 
                    Conversation conv = await agent.RunAsync(appendMessages: [message], cancellationToken: cancellationToken);
                    if(conv.Messages.Count > 0)
                    {
                        bag.Add(conv.Messages.LastOrDefault()!);
                    }     
                }));
            }

            await Task.WhenAll(agentTask);

            ChatMessage resultMessage = new ChatMessage(ChatMessageRoles.Assistant);
            resultMessage.Parts = new List<ChatMessagePart>();
            resultMessage.Parts.AddRange(bag.SelectMany(m => m.Parts ?? []));

            this.Conversation.Add(resultMessage);

            return resultMessage;
        }

        public void ClearMessages()
        {
            Conversation.Clear();
        }

        public List<ChatMessage> GetMessages()
        {
            return Conversation;
        }

        public async Task<List<HandoffAgent>> SelectCurrentAgent(ChatMessage? inputMessage)
        {
            string instructions = @$"
I need you to decide if you need to handoff the conversation to another agent.
If you do, please return the agent you want to handoff to and the reason for the handoff.
If not just return CurrentAgent
Out of the following Agents which agent should we Handoff the conversation too and why? 


{{""NAME"": ""CurrentAgent"",""Instructions"":""{CurrentAgent.Instructions}""}}


{string.Join("\n\n", CurrentAgent.HandoffAgents.Select(handoff => $" {{\"NAME\": \"{handoff.Id}\",\"Handoff Reason\":\"{handoff.Description}\"}}"))}

";
            TornadoAgent handoffDecider = new TornadoAgent(CurrentAgent.Client, ChatModel.OpenAi.Gpt41.V41, instructions)
            {
                Options =
        {
            ResponseFormat = AgentHandoffUtility.CreateHandoffResponseFormat(CurrentAgent.HandoffAgents.ToArray()),
            CancellationToken = cts.Token // Set the cancellation token source for the Control Agent
        }
            };

            string prompt = "Current Conversation:\n";

            if (Conversation != null)
            {
                foreach (ChatMessage message in Conversation)
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

            foreach (ChatMessagePart part in inputMessage.Parts ?? [])
            {
                if (part is not { Text: "" })
                {
                    prompt += $"{inputMessage.Role}: {part.Text}\n";
                }
            }

            Conversation handoff = await TornadoRunner.RunAsync(handoffDecider, prompt, cancellationToken: cts.Token);

            List<HandoffAgent> handoffAgents = new List<HandoffAgent>();
            if (handoff.Messages.Count > 0 && handoff.Messages.Last().Content != null)
            {
                if (handoff.Messages.Last() is { Role: ChatMessageRoles.Assistant })
                {
                    string response = handoff.Messages.Last().Content!;
                    if (response is not null)
                    {
                        List<string> selectedAgents = AgentHandoffUtility.ParseHandoffResponse(response);
                        foreach(string agent in selectedAgents)
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

            CurrentAgent = handoffAgents.FirstOrDefault() ?? CurrentAgent;

            return handoffAgents;
        }
    }
}
