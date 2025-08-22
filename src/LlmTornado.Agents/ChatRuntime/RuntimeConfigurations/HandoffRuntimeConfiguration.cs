using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace LlmTornado.Agents.ChatRuntime.RuntimeConfigurations
{
    internal class HandoffRuntimeConfiguration : IRuntimeConfiguration
    {
        public CancellationTokenSource cts { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public List<ChatMessage> Conversation { get; set; } = new List<ChatMessage>();
        public TornadoAgent CurrentAgent { get; set; }

        public HandoffRuntimeConfiguration(TornadoAgent initialAgent)
        {
            CurrentAgent = initialAgent;
        }

        public async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            this.Conversation.Add(message);

            ConcurrentBag<ChatMessage> bag = new ConcurrentBag<ChatMessage>(GetMessages());
            List < TornadoAgent > handoffAgents = await SelectCurrentAgent(message);
            List<Task> agentTask = new List<Task>();

            foreach (TornadoAgent agent in handoffAgents)
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

        public async Task<List<TornadoAgent>> SelectCurrentAgent(ChatMessage? inputMessage)
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

            List<TornadoAgent> handoffAgents = new List<TornadoAgent>();
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
                            TornadoAgent? handoffAgent = CurrentAgent.HandoffAgents.FirstOrDefault(a => a.Id == agent);
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
