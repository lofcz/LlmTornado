using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Code;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.ChatRuntime.RuntimeConfigurations
{
    public class ConcurrentRuntimeConfiguration : IRuntimeConfiguration
    {
        public CancellationTokenSource cts { get; set; }
        public List<ChatMessage> Conversation { get; set; } = new List<ChatMessage>();
        public List<TornadoAgent> Agents { get; set; } = new List<TornadoAgent>();
        public bool Streaming { get; set; } = false;

        public Func<ChatRuntimeEvents, ValueTask>? OnRuntimeEvent { get; set; }
        public string ResultProcessingInstructions { get; set; }

        public ConcurrentRuntimeConfiguration(TornadoAgent[] agents, string resultProcessingInstructions)
        {
            Agents.AddRange(agents);
            ResultProcessingInstructions = resultProcessingInstructions;
        }

        public async ValueTask<ChatMessage> AddToChatAsync(ChatMessage message, CancellationToken cancellationToken = default)
        {
            this.Conversation.Add(message);

            ConcurrentBag<ChatMessage> bag = new ConcurrentBag<ChatMessage>(GetMessages());
            List<Task> agentTask = new List<Task>();

            foreach (TornadoAgent agent in Agents)
            {
                agentTask.Add(Task.Run(async () => {
                    Conversation conv = await agent.RunAsync(appendMessages: Conversation, cancellationToken: cancellationToken);
                    if (conv.Messages.Count > 0)
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

            TornadoAgent finalAgent = new TornadoAgent(
                client: Agents.First().Client,
                name: "Result Synthesizer",
                model: Agents.First().Model,
                instructions: ResultProcessingInstructions
            );

            this.Conversation.Add(new ChatMessage(ChatMessageRoles.User, ResultProcessingInstructions));

            Conversation synthesizedResult =  await finalAgent.RunAsync(
                appendMessages: this.Conversation, 
                streaming:Streaming, 
                runnerCallback: (sEvent) => { OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent)); return Threading.ValueTaskCompleted; }, 
                cancellationToken: cancellationToken);

            return synthesizedResult.Messages.Last();
        }

        public void ClearMessages()
        {
            Conversation.Clear();
        }

        public List<ChatMessage> GetMessages()
        {
            return Conversation;
        }

        public ChatMessage GetLastMessage()
        {
            return Conversation.LastOrDefault() ?? new ChatMessage(ChatMessageRoles.System, "No messages yet.");
        }
    }
}
