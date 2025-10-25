using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using LlmTornado.VectorDatabases;

namespace LlmTornado.Agents.Samples.ContextController;

public class ToolContextService : IToolContextService
{
    private TornadoApi _client { get; set; }
    private ContextContainer _contextContainer { get; set; }
    public IVectorDatabase ToolStore { get; set; } // Will need this when tools exceed 50
    private Dictionary<string, Tool> toolLibrary { get; set; } = new Dictionary<string, Tool>();
    private Dictionary<string, string> toolDescriptions { get; set; } = new Dictionary<string, string>();
    public ToolContextService(TornadoApi api, ContextContainer contextContainer)
    {
        _client = api;
        _contextContainer = contextContainer;
    }
    public async Task<List<Tool>>? GetToolContext()
    {
        List<Tool> selectedTools = new List<Tool>();
        string? prompt = _contextContainer.ChatMessages.Last().GetMessageContent();
        if (string.IsNullOrEmpty(prompt))
            return selectedTools;

        TornadoAgent contextAgent = new TornadoAgent(_client, ChatModel.OpenAi.Gpt5.V5Mini);

        contextAgent.Instructions = $@"The current goal is: {_contextContainer.Goal ?? "N/A"}. The current task is {prompt}. 
Given the goal and the current task please choose the tools required from the provided list";

        contextAgent.Options.ResponseFormat = GetToolResponseFormat();

        string contextDescription = "Here is a list of available tools and their descriptions:\n";
        contextDescription += GetToolDescriptions();

        Conversation conv = await contextAgent.RunAsync(contextDescription);

        List<string> toolSelected = ToolSelectorHelper.ParseResponse(conv.Messages.Last().Content);

        foreach(string tool in toolSelected)
        {
            if (toolLibrary.ContainsKey(tool))
            {
                selectedTools.Add(toolLibrary[tool]);
            }
        }
       
        return selectedTools;
    }

    public void AddToolToLibrary(string toolKey, Tool tool, string description)
    {
        toolLibrary[toolKey] = tool;
        toolDescriptions[toolKey] = description;
    }

    private string GetToolDescriptions()
    {
        return string.Join("\n", toolDescriptions.Select(kv => $"{kv.Key}: {kv.Value}"));
    }

    private ChatRequestResponseFormats GetToolResponseFormat()
    {
        return ToolSelectorHelper.CreateResponseFormat(toolLibrary.Keys.ToArray());
    }
}
