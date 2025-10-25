using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using System.ComponentModel;

namespace LlmTornado.Agents.Samples.ContextController;
public struct InstructionsMessage
{
    [Description("A new set of instructions for the agent to follow.")]
    public string Instructions { get; set; }
}


public class InstructionContextService : IInstructionsContextService
{
    private TornadoApi _client { get; set; }
    private ContextContainer _contextContainer { get; set; }

    public InstructionContextService(TornadoApi api, ContextContainer contextContainer)
    {
        _client = api;
        _contextContainer = contextContainer;
    }

    public async Task<string> GetInstructionsContext()
    {
        TornadoAgent contextAgent = new TornadoAgent(_client, ChatModel.OpenAi.Gpt5.V5, outputSchema: typeof(InstructionsMessage));

        if(string.IsNullOrEmpty(_contextContainer.Goal))
        {
            throw new InvalidOperationException("No Goal Defined");
        }

        contextAgent.Instructions = $@"Given the information in the following prompt generate the System message for the next agent to complete its task.";

        string prompt = $@"
The current goal is: {_contextContainer.Goal ?? "N/A"}  
The current Task is {_contextContainer.CurrentTask ?? "n/a"}.";

        Conversation conv = await contextAgent.RunAsync(prompt);

        InstructionsMessage result = conv.Messages.Last().Content.ParseJson<InstructionsMessage>();

        return result.Instructions;
    }
}
