using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Images;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents.Orchestration
{
    public class HandoffOrchestration : ChatOrchestration
    {
        public HandoffOrchestration(string name, TornadoAgent currentAgent) : base(name, currentAgent)
        {

        }

        internal override async Task<List<ChatMessagePart>?> OnInvokedAsync(string userInput, bool streaming = true, string? base64Image = null)
        {
            List<ChatMessage> messages = new List<ChatMessage>();

            ChatMessage userMessage = new ChatMessage(ChatMessageRoles.User, [new ChatMessagePart(userInput)]);

            string inputMessage = userInput;

            if (base64Image is not null)
            {
                userMessage.Parts?.Add(new ChatMessagePart(base64Image, ImageDetail.Auto));
            }

            if (CurrentResult.Messages.Count > 0)
            {
                messages.AddRange(CurrentResult.Messages);
            }

            messages.Add(userMessage);

            await CheckForHandoff(messages); //Check to just switch agents for now 

            //Returns null for no extra messages
            return await base.OnInvokedAsync(userInput, streaming, base64Image);
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


{string.Join("\n\n", CurrentAgent.HandoffAgents.Select(handoff => $" {{\"NAME\": \"{handoff.Id}\",\"Handoff Reason\":\"{handoff.Description}\"}}"))}

";
            TornadoAgent handoffDecider = new TornadoAgent(CurrentAgent.Client, ChatModel.OpenAi.Gpt41.V41Nano, instructions)
            {
                Options =
            {
                ResponseFormat = AgentHandoff.CreateHandoffResponseFormat(CurrentAgent.HandoffAgents.ToArray()),
                CancellationToken = cts.Token // Set the cancellation token source for the Control Agent
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

            Conversation handoff = await TornadoRunner.RunAsync(handoffDecider, prompt, streaming: false, cancellationToken: cts.Token);

            if (handoff.Messages.Count > 0 && handoff.Messages.Last().Content != null)
            {
                if (handoff.Messages.Last() is { Role: ChatMessageRoles.Assistant })
                {
                    string response = handoff.Messages.Last().Content!;
                    if (response is not null)
                    {
                        List<string> selectedAgents = AgentHandoff.ParseHandoffResponse(response);
                        CurrentAgent = CurrentAgent.HandoffAgents.FirstOrDefault(agent => agent.Id.Equals(selectedAgents[0], StringComparison.OrdinalIgnoreCase)) ?? CurrentAgent;
                    }
                }
            }
        }
    }
}
