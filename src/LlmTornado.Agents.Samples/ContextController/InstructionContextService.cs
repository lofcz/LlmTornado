using LlmTornado.Chat;
using LlmTornado.Chat.Models;

namespace LlmTornado.Agents.Samples.ContextController;
public struct GoalMessage
{
    public string Goal { get; set; }
}

public class InstructionContextService : IInstructionsContextService
{
    public TornadoApi Client { get; set; }
    public List<string> GoalHistory { get; set; } = new List<string>();
    public string? Goal => GoalHistory.LastOrDefault();
   
    public InstructionContextService(TornadoApi api)
    {
        Client = api;
    }

    public async Task<string> GetInstructionsContext()
    {
        throw new NotImplementedException();
    }

    private async Task UpdateGoal(ChatMessage newMessage, List<ChatMessage> chatHistory)
    {
        string? userPrompt = newMessage.GetMessageContent();
        if (string.IsNullOrEmpty(userPrompt))
            return;

        TornadoAgent contextAgent = new TornadoAgent(Client, ChatModel.OpenAi.Gpt5.V5);
        contextAgent.Instructions = $@"The current goal is: {Goal ?? "N/A"}  The new user prompt is {userPrompt}. Given the Latest Message Stream Determine a new goal message. 
If the message contains any information about the user's intent or desired outcome, incorporate that into the new goal.";
        contextAgent.UpdateOutputSchema(typeof(GoalMessage));

        Conversation conv = await contextAgent.RunAsync(
            appendMessages: chatHistory.TakeLast(10).ToList());

        GoalMessage result = conv.Messages.Last().Content.ParseJson<GoalMessage>();

        GoalHistory.Add(result.Goal);
    }
}
