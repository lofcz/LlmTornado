using LlmTornado.Agents.Orchestration.Core;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;


namespace LlmTornado.Agents.Orchestration;

public class HandoffOrchestration : AgentOrchestration
{
    public HandoffOrchestration(TornadoAgent agent)
    {
        OrchestrationRunnableBase handoffRunnable = new HandoffRunnable(agent);
        Runnables.Add("HandOffAgentState", handoffRunnable);
        SetEntryRunnable(handoffRunnable);
        SetRunnableWithResult(handoffRunnable);
    }
}

public class HandoffRunnable : RunnableAgent
{
    public TornadoAgent CurrentAgent { get; set; }

    public Conversation Conversation { get; set; }

    public HandoffRunnable(TornadoAgent agent) : base(agent)
    {
        CurrentAgent = agent;
        IsDeadEnd = true;
    }

    public override async ValueTask<ChatMessage> Invoke(ChatMessage input)
    {
        if(Conversation != null)
        {
            Conversation.AppendMessage(input);
            Conversation = await CurrentAgent.RunAsync(conversation: Conversation, streaming: IsStreaming, streamingCallback: (sEvent) => { OnStreamingEvent?.Invoke(sEvent); return Threading.ValueTaskCompleted; });
        }
        else
        {
            Conversation = await CurrentAgent.RunAsync(messages: [input]);
        }

        Orchestrator.HasCompletedSuccessfully();

        return Conversation.Messages.Last();
    }

    public override async ValueTask InitializeRunnable(ChatMessage? inputMessage)
    {
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

        if(Conversation != null)
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

        foreach (ChatMessagePart part in inputMessage.Parts ?? [])
        {
            if (part is not { Text: "" })
            {
                prompt += $"{inputMessage.Role}: {part.Text}\n";
            }
        }

        Conversation handoff = await TornadoRunner.RunAsync(handoffDecider, prompt, cancellationToken: cts.Token);

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



