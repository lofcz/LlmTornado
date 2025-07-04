using System;
using System.Collections.Generic;
using LlmTornado.Chat.Models.DeepInfra;
using LlmTornado.Chat.Models.DeepSeek;
using LlmTornado.Chat.Models.Mistral;
using LlmTornado.Chat.Models.OpenRouter;
using LlmTornado.Chat.Models.Perplexity;
using LlmTornado.Chat.Models.XAi;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Models;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Models;

/// <summary>
/// Models supporting text based inference, such as chat or completions
/// </summary>
public class ChatModel : ModelBase
{
    /// <summary>
    /// Models from OpenAI.
    /// </summary>
    public static readonly ChatModelOpenAi OpenAi = new ChatModelOpenAi();

    /// <summary>
    /// Models from Anthropic.
    /// </summary>
    public static readonly ChatModelAnthropic Anthropic = new ChatModelAnthropic();
    
    /// <summary>
    /// Models from Cohere.
    /// </summary>
    public static readonly ChatModelCohere Cohere = new ChatModelCohere();
    
    /// <summary>
    /// Models from Google.
    /// </summary>
    public static readonly ChatModelGoogle Google = new ChatModelGoogle();
    
    /// <summary>
    /// Models provided by Groq.
    /// </summary>
    public static readonly ChatModelGroq Groq = new ChatModelGroq();
    
    /// <summary>
    /// Models provided by DeepSeek.
    /// </summary>
    public static readonly ChatModelDeepSeek DeepSeek = new ChatModelDeepSeek();
    
    /// <summary>
    /// Models provided by Mistral.
    /// </summary>
    public static readonly ChatModelMistral Mistral = new ChatModelMistral();
    
    /// <summary>
    /// Models provided by xAI.
    /// </summary>
    public static readonly ChatModelXAi XAi = new ChatModelXAi();
    
    /// <summary>
    /// Models provided by xAI.
    /// </summary>
    public static readonly ChatModelPerplexity Perplexity = new ChatModelPerplexity();
    
    /// <summary>
    /// Models provided by DeepInfra.
    /// </summary>
    public static readonly ChatModelDeepInfra DeepInfra = new ChatModelDeepInfra();
    
    /// <summary>
    /// Models from Open Router.
    /// </summary>
    public static readonly ChatModelOpenRouter OpenRouter = new ChatModelOpenRouter();
    
    /// <summary>
    /// All known models keyed by name.
    /// </summary>
    public static readonly Dictionary<string, IModel> AllModelsMap = [];

    internal static readonly Dictionary<string, IModel> AllModelsApiMap = [];
    
    /// <summary>
    /// All known chat models.
    /// </summary>
    public static readonly List<IModel> AllModels;
    
    /// <summary>
    /// Minimum reasoning tokens
    /// </summary>
    public int? ReasoningTokensMin { get; set; }
    
    /// <summary>
    /// Maximum reasoning tokens
    /// </summary>
    public int? ReasoningTokensMax { get; set; }
    
    /// <summary>
    /// Special values enabled for reasoning mode
    /// </summary>
    public HashSet<int>? ReasoningTokensSpecialValues { get; set; }
    
    /// <summary>
    /// Endpoints supporting this model.
    /// </summary>
    public HashSet<ChatModelEndpointCapabilities>? EndpointCapabilities { get; set; }

    /// <summary>
    /// Clamps the preferred reasoning tokens so that they are compatible with the model.
    /// </summary>
    internal int? ClampReasoningTokens(int? preferred)
    {
        if (preferred is null)
        {
            // 0 and -1 are common special values for disabled thinking / dynamic thinking
            return (ReasoningTokensSpecialValues?.Contains(0) ?? false) ? 0 : (ReasoningTokensSpecialValues?.Contains(-1) ?? false) ? -1 : ReasoningTokensMin;
        }
        
        if (ReasoningTokensSpecialValues?.Contains(preferred.Value) ?? false)
        {
            return preferred;
        }

        if (ReasoningTokensMin is not null && ReasoningTokensMax is null)
        {
            return Math.Min(ReasoningTokensMin.Value, preferred.Value);
        }
        
        if (ReasoningTokensMax is not null && ReasoningTokensMin is null)
        {
            return Math.Min(ReasoningTokensMax.Value, preferred.Value);
        }
        
        if (ReasoningTokensMin is not null && ReasoningTokensMax is not null)
        {
            return preferred.Value.Clamp(ReasoningTokensMin.Value, ReasoningTokensMax.Value);
        }

        return preferred.Value;
    }
    
    static ChatModel()
    {
        AllModels = [
            ..OpenAi.AllModels,
            ..Anthropic.AllModels,
            ..Cohere.AllModels,
            ..Google.AllModels,
            ..Groq.AllModels,
            ..DeepSeek.AllModels,
            ..Mistral.AllModels,
            ..XAi.AllModels,
            ..Perplexity.AllModels,
            ..DeepInfra.AllModels,
            ..OpenRouter.AllModels
        ];
        
        AllModels.ForEach(x =>
        {
            AllModelsMap.TryAdd(x.Name, x);

            if (!x.ApiName.IsNullOrWhiteSpace())
            {
                AllModelsApiMap.TryAdd(x.ApiName, x);
            }
        });
    }
    
    /// <summary>
    /// Represents a Model with the given name.
    /// </summary>
    /// <param name="name">The id/name of the model.</param>
    /// <param name="ownedBy"></param>
    /// <param name="provider"></param>
    public ChatModel(string name, string? ownedBy = null, LLmProviders? provider = null)
    {
        Name = name;
        OwnedBy = ownedBy ?? "openai";
        Provider = provider ?? GetProvider(name) ?? LLmProviders.OpenAi;
    }

    /// <summary>
    /// Creates a new model identified by name and provider.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="provider"></param>
    public ChatModel(string name, LLmProviders provider)
    {
        Name = name;
        Provider = provider;
    }
    
    /// <summary>
    /// Creates a new model identified by name and provider.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="provider"></param>
    /// <param name="contextTokens"></param>
    public ChatModel(string name, LLmProviders provider, int contextTokens)
    {
        Name = name;
        Provider = provider;
        ContextTokens = contextTokens;
    }
    
    internal ChatModel(string name, string apiName, LLmProviders provider, int contextTokens)
    {
        Name = name;
        ApiName = apiName;
        Provider = provider;
        ContextTokens = contextTokens;
    }
    
    /// <summary>
    /// Creates a new model identified by name and provider.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="provider"></param>
    /// <param name="contextTokens"></param>
    /// <param name="aliases"></param>
    public ChatModel(string name, LLmProviders provider, int contextTokens, List<string> aliases)
    {
        Name = name;
        Provider = provider;
        ContextTokens = contextTokens;
    }
    
    /// <summary>
    /// Creates a new model identified by name. The provider of the model is inferred automatically.
    /// </summary>
    /// <param name="name"></param>
    public ChatModel(string name)
    {
        Name = name;
        Provider = GetProvider(name) ?? LLmProviders.OpenAi;
    }

    /// <summary>
    /// Represents a generic model.
    /// </summary>
    public ChatModel()
    {
    }
    
    /// <summary>
    /// Allows a model to be implicitly cast to the string of its <see cref="ModelBase.Name" />
    /// </summary>
    /// <param name="model">The <see cref="ChatModel" /> to cast to a string.</param>
    public static implicit operator string(ChatModel model)
    {
        return model.Name;
    }

    /// <summary>
    /// Looks up the model provider. Only works for known models.
    /// </summary>
    /// <param name="modelName"></param>
    /// <returns></returns>
    public static LLmProviders? GetProvider(string? modelName)
    {
        if (modelName is not null && AllModelsMap.TryGetValue(modelName, out IModel? protoModel))
        {
            return protoModel.Provider;
        }

        return null;
    }
    
    /// <summary>
    /// Allows a string to be implicitly cast as an <see cref="Model" /> with that <see cref="IModel.Name" />
    /// </summary>
    /// <param name="name">The id/<see cref="IModel.Name" /> to use</param>
    public static implicit operator ChatModel(string? name)
    {
        return new ChatModel(name ?? string.Empty, name is null ? LLmProviders.Unknown : GetProvider(name) ?? LLmProviders.Unknown);
    }
}

internal class ChatModelJsonConverter : JsonConverter<ChatModel>
{
    public override void WriteJson(JsonWriter writer, ChatModel? value, JsonSerializer serializer)
    {
        writer.WriteValue(value?.GetApiName);
    }

    public override ChatModel? ReadJson(JsonReader reader, Type objectType, ChatModel? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return existingValue;
    }
}
