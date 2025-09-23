using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat.Models.DeepInfra;
using LlmTornado.Chat.Models.DeepSeek;
using LlmTornado.Chat.Models.Mistral;
using LlmTornado.Chat.Models.MoonshotAi;
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
    /// Models provided by Moonshot AI.
    /// </summary>
    public static readonly ChatModelMoonshotAi MoonshotAi = new ChatModelMoonshotAi();
    
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
    public static Dictionary<string, IModel> AllModelsMap => LazyAllModelsMap.Value;

    private static readonly Lazy<Dictionary<string, IModel>> LazyAllModelsMap = new Lazy<Dictionary<string, IModel>>(() =>
    {
        Dictionary<string, IModel> map = [];

        AllModels.ForEach(x =>
        {
            map.TryAdd(x.Name, x);

            if (!x.ApiName.IsNullOrWhiteSpace())
            {
                AllModelsApiMap.TryAdd(x.ApiName, x);
            }
        });

        return map;
    });

    internal static Dictionary<string, IModel> AllModelsApiMap => LazyAllModelsApiMap.Value;
    
    private static readonly Lazy<Dictionary<string, IModel>> LazyAllModelsApiMap = new Lazy<Dictionary<string, IModel>>(() =>
    {
        Dictionary<string, IModel> map = [];

        AllModels.ForEach(x =>
        {
            if (!x.ApiName.IsNullOrWhiteSpace())
            {
                map.TryAdd(x.ApiName, x);
            }
        });

        return map;
    });

    /// <summary>
    /// All known chat models.
    /// </summary>
    public static List<IModel> AllModels => LazyAllModels.Value;
    
    private static readonly Lazy<List<IModel>> LazyAllModels = new Lazy<List<IModel>>(() => AllProviders.SelectMany(x => x.AllModels).ToList());
    
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

    /// <summary>
    /// All known chat model providers.
    /// </summary>
    public static List<BaseVendorModelProvider> AllProviders => LazyAllProviders.Value;

    private static readonly Lazy<List<BaseVendorModelProvider>> LazyAllProviders = new Lazy<List<BaseVendorModelProvider>>(() => [
        OpenAi, Anthropic, Cohere, Google, Groq, DeepSeek, Mistral, XAi, Perplexity, MoonshotAi, DeepInfra, OpenRouter
    ]);
    
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
    /// Creates a copy of a model based on another model.
    /// </summary>
    /// <param name="basedOn"></param>
    public ChatModel(ChatModel basedOn)
    {
        Copy(basedOn);
    }
    
    
    /// <summary>
    /// Creates a copy of a model based on another model.
    /// </summary>
    /// <param name="basedOn"></param>
    public ChatModel(IModel basedOn)
    {
        if (basedOn is ChatModel chatModel)
        {
            Copy(chatModel);
            return;
        }

        Copy(basedOn);
    }

    internal void Copy(IModel basedOn)
    {
        Name = basedOn.Name;
        Provider = basedOn.Provider;
        ApiName = basedOn.ApiName;
        Aliases = basedOn.Aliases;
        OptimisticallyResolved = basedOn.OptimisticallyResolved;
    }

    internal void Copy(ChatModel basedOn)
    {
        Name = basedOn.Name;
        Provider = basedOn.Provider;
        ContextTokens = basedOn.ContextTokens;
        ApiName = basedOn.ApiName;
        Aliases = basedOn.Aliases;
        EndpointCapabilities = basedOn.EndpointCapabilities;
        ReasoningTokensMax = basedOn.ReasoningTokensMax;
        ReasoningTokensMin = basedOn.ReasoningTokensMin;
        ReasoningTokensSpecialValues = basedOn.ReasoningTokensSpecialValues;
        CreatedUnixTime = basedOn.CreatedUnixTime;
        Object = basedOn.Object;
        OwnedBy = basedOn.OwnedBy;
        Permission = basedOn.Permission;
        OptimisticallyResolved = basedOn.OptimisticallyResolved;
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

    private static readonly Lazy<FrozenDictionary<string, IModel>> modelsByName = new Lazy<FrozenDictionary<string, IModel>>(() =>
    {
        Dictionary<string, IModel> dict = new Dictionary<string, IModel>(AllModels?.Count ?? 0);

        if (AllModels is not null)
        {
            foreach (IModel model in AllModels)
            {
                dict[model.Name] = model;
            }   
        }

        return dict.ToFrozenDictionary();
    });

    internal static ChatModel? ResolveModel(LLmProviders provider, string modelName)
    {
        if (modelsByProviderName.Value.TryGetValue(provider, out ChatModelVendorMap map))
        {
            if (map.ModelsByName.TryGetValue(modelName, out IModel model))
            {
                return new ChatModel(model)
                {
                    Provider = provider,
                    OptimisticallyResolved = true
                };   
            }
        }

        return null;
    }
    
    private static readonly Lazy<Dictionary<LLmProviders, ChatModelVendorMap>> modelsByProviderName = new Lazy<Dictionary<LLmProviders, ChatModelVendorMap>>(() =>
    {
        Dictionary<LLmProviders, ChatModelVendorMap> map = new Dictionary<LLmProviders, ChatModelVendorMap>(AllProviders.Count);
        
        foreach (BaseVendorModelProvider provider in AllProviders)
        {
            Dictionary<string, IModel> dictByName = new Dictionary<string, IModel>(provider.AllModels.Count);

            foreach (IModel model in provider.AllModels)
            {
                dictByName[model.Name] = model;

                if (model.Aliases is not null)
                {
                    foreach (string alias in model.Aliases)
                    {
                        dictByName[alias] = model;
                    }
                }

                if (model.ApiName is not null)
                {
                    dictByName[model.ApiName] = model;   
                }
            }

            map[provider.Provider] = new ChatModelVendorMap
            {
                ModelsByName = dictByName.ToFrozenDictionary()
            };
        }

        return map;
    });
    
    /// <summary>
    /// Allows a string to be implicitly cast as an <see cref="Model" /> with that <see cref="IModel.Name" />
    /// </summary>
    /// <param name="name">The id/<see cref="IModel.Name" /> to use</param>
    public static implicit operator ChatModel(string? name)
    {
        LLmProviders provider = GetProvider(name) ?? LLmProviders.Unknown;
        
        if (name?.Length > 0)
        {
            ChatModel? resolved = ResolveModel(provider, name);

            if (resolved is not null)
            {
                return resolved;
            }
        }
        
        return new ChatModel(name ?? string.Empty, name is null ? LLmProviders.Unknown : provider)
        {
            OptimisticallyResolved = true
        };
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

internal class ChatModelVendorMap
{
    public FrozenDictionary<string, IModel> ModelsByName { get; set; }
}