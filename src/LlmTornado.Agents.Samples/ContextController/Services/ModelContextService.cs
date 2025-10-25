using LlmTornado.Chat;
using LlmTornado.Chat.Models;

namespace LlmTornado.Agents.Samples.ContextController;

public class ModelContextService : IModelContextService
{
    private TornadoApi _client { get; set; }
    private ContextContainer _contextContainer { get; set; } 
    private Dictionary<string, ChatModel> modelLibrary { get; set; } = new Dictionary<string, ChatModel>();
    /// <summary>
    /// Set the name and the description of the model to be used in the context
    /// </summary>
    private Dictionary<string, string> modelDescriptions { get; set; } = new Dictionary<string, string>();

    public ModelContextService(TornadoApi api, ContextContainer contextContainer)
    {
        _client = api;
        _contextContainer = contextContainer;
    }
    /// <summary>
    /// Adds a model to the internal library for selection
    /// </summary>
    /// <param name="name"> Maybe use a unique identifier for the model so model isnt bias towards its owns API </param>
    /// <param name="model"> Chat model to add</param>
    /// <param name="description">Description on why to select this model</param>
    public void AddModelToLibrary(string name, ChatModel model, string description)
    {
        modelLibrary[name] = model;
        modelDescriptions[name] = description;
    }

    public async Task<ChatModel> GetModelContext()
    {
        string? prompt = _contextContainer.ChatMessages.Last().GetMessageContent();
        if (string.IsNullOrEmpty(prompt))
            return modelLibrary.FirstOrDefault().Value;

        TornadoAgent contextAgent = new TornadoAgent(_client, ChatModel.OpenAi.Gpt5.V5);
        contextAgent.Instructions = $@"The current goal is: {_contextContainer.Goal ?? "N/A"}. The current task is {prompt}. 
Given the goal and the current task please choose the best model from the provided list";

        contextAgent.Options.ResponseFormat = ModelSelectorHelper.CreateResponseFormat(modelDescriptions.Keys.ToArray());

        string contextDescription = "Here is a list of available models and their descriptions:\n";
        foreach (var kvp in modelDescriptions)
        {
            contextDescription += $"- {kvp.Key}: {kvp.Value}\n";
        }

        Conversation conv = await contextAgent.RunAsync(contextDescription);

        string modelSelected = ModelSelectorHelper.ParseModelSelectionResponse(conv.Messages.Last().Content);

        if (modelLibrary.ContainsKey(modelSelected))
        {
            return modelLibrary[modelSelected];
        }

        return modelLibrary.FirstOrDefault().Value;
    }
}
