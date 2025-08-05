using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Orchestration
{
    public class HandoffOrchestration : ChatOrchestration
    {
        public TornadoAgent CurrentAgent { get; set; }
        public CancellationToken CancellationTokenSource { get; set; } = CancellationToken.None;
        public Action<string>? VerboseCallback { get; set; }

        public HandoffOrchestration(string name, TornadoAgent currentAgent) : base(name, currentAgent)
        {
            CurrentAgent = currentAgent ?? throw new ArgumentNullException(nameof(currentAgent), "Current Agent cannot be null");
        }

        public async Task CheckForHandoff(List<ChatMessage> messages)
        {
            if (CurrentAgent.HandoffAgents.Count == 0)
            {
                return; // No handoff agents to process
            }

            string instructions = @$"
I need you to decide if you need to handoff the conversation to another agent.
If you do, please return the agent you want to handoff to and the reason for the handoff.
If not just return CurrentAgent
Out of the following Agents which agent should we Handoff the conversation too and why? 


{{""NAME"": ""CurrentAgent"",""Instructions"":""{CurrentAgent.Instructions}""}}


{string.Join("\n\n", CurrentAgent.HandoffAgents.Select(agent => $" {{\"NAME\": \"{agent.Name}\",\"Handoff Reason\":\"{agent.HandoffReason}\"}}"))}

";
            TornadoAgent handoffDecider = new TornadoAgent(CurrentAgent.Client, ChatModel.OpenAi.Gpt41.V41Nano, instructions)
            {
                Options =
            {
                ResponseFormat = AgentHandoff.CreateHandoffResponseFormat(CurrentAgent.HandoffAgents.ToArray()),
                CancellationToken = CancellationTokenSource // Set the cancellation token source for the Control Agent
            }
            };

            string prompt = "Current Conversation:\n";

            foreach (ChatMessage message in messages)
            {
                foreach (ChatMessagePart part in message.Parts ?? [])
                {
                    if (part is not { Text: "" })
                    {
                        prompt += $"{message.Role}: {part.Text}\n";
                    }
                }
            }

            Conversation handoff = await TornadoRunner.RunAsync(handoffDecider, prompt, streaming: false, cancellationToken: CancellationTokenSource);

            if (handoff.Messages.Count > 0 && handoff.Messages.Last().Content != null)
            {
                if (handoff.Messages.Last() is { Role: ChatMessageRoles.Assistant })
                {
                    string response = handoff.Messages.Last().Content!;
                    if (response is not null)
                    {
                        List<string> selectedAgents = AgentHandoff.ParseHandoffResponse(response);
                        CurrentAgent = CurrentAgent.HandoffAgents.FirstOrDefault(agent => agent.Name.Equals(selectedAgents[0], StringComparison.OrdinalIgnoreCase))?.Agent ?? CurrentAgent;
                    }
                }
            }
        }
    }
}
