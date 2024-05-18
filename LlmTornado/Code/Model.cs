using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Code.Models;
using LlmTornado.Code.Vendor;
using Newtonsoft.Json;

namespace LlmTornado.Models;

/// <summary>
///     Represents a language model
/// </summary>
public class Model : ModelBase
{
    public const string OpenAi = "openai";

    /// <summary>
    ///     Represents an Model with the given id/<see cref="Name" />
    /// </summary>
    /// <param name="name">The id/<see cref="Name" /> to use.</param>
    /// <param name="ownedBy">Either</param>
    /// <param name="provider">Either</param>
    public Model(string name, string? ownedBy = OpenAi, LLmProviders provider = LLmProviders.OpenAi)
    {
        Name = name;
        OwnedBy = ownedBy;
        Provider = provider;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="name"></param>
    /// <param name="provider"></param>
    public Model(string name, LLmProviders provider)
    {
        Name = name;
        Provider = provider;
    }

    /// <summary>
    ///     Represents a generic Model/model
    /// </summary>
    public Model()
    {
    }

    /// <summary>
    ///     The default model to use in requests if no other model is specified.
    /// </summary>
    public static Model DefaultModel { get; set; } = DavinciText;

    /// <summary>
    ///     Capable of very simple tasks, usually the fastest model in the GPT-3 series, and lowest cost
    /// </summary>
    public static Model AdaText => new("text-ada-001");

    /// <summary>
    ///     Capable of straightforward tasks, very fast, and lower cost.
    /// </summary>
    public static Model BabbageText => new("text-babbage-001");

    /// <summary>
    ///     Very capable, but faster and lower cost than Davinci.
    /// </summary>
    public static Model CurieText => new("text-curie-001");

    /// <summary>
    ///     Most capable GPT-3 model. Can do any task the other models can do, often with higher quality, longer output and
    ///     better instruction-following. Also supports inserting completions within text.
    /// </summary>
    public static Model DavinciText => new("text-davinci-003");

    /// <summary>
    ///     Similar capabilities to text-davinci-003 but trained with supervised fine-tuning instead of reinforcement learning
    /// </summary>
    public static Model DavinciText002 => new("text-davinci-002");

    /// <summary>
    ///     Almost as capable as Davinci Codex, but slightly faster. This speed advantage may make it preferable for real-time
    ///     applications.
    /// </summary>
    public static Model CushmanCode => new("code-cushman-001");

    /// <summary>
    ///     Most capable Codex model. Particularly good at translating natural language to code. In addition to completing
    ///     code, also supports inserting completions within code.
    /// </summary>
    public static Model DavinciCode => new("code-davinci-002");

    /// <summary>
    ///     Similar capabilities as text-davinci-003 but compatible with legacy Completions endpoint and not Chat Completions.
    /// </summary>
    public static Model GptTurboInstruct => new("gpt-3.5-turbo-instruct");

    /// <summary>
    ///     Snapshot of gpt-3.5-turbo-instruct from September 14th 2023. Unlike gpt-3.5-turbo-instruct, this model will not
    ///     receive updates, and will be deprecated 3 months after a new version is released.
    ///     Similar capabilities as text-davinci-003 but compatible with legacy Completions endpoint and not Chat Completions.
    /// </summary>
    public static Model GptTurboInstruct0914 => new("gpt-3.5-turbo-instruct-0914");

    /// <summary>
    ///     OpenAI offers one second-generation embedding model for use with the embeddings API endpoint.
    ///     Dimensions: 1536
    /// </summary>
    public static Model AdaTextEmbedding => new("text-embedding-ada-002");

    /// <summary>
    ///     Model released in 01/24 update, superseeds <see cref="AdaTextEmbedding" /> at a cheaper price.
    ///     Dimensions: 1536 (end of the sequence numbers can be removed and dimensions reduced up to 256 at a reasonable
    ///     perplexity increase)
    /// </summary>
    public static Model TextEmbedding3Small => new("text-embedding-3-small");

    /// <summary>
    ///     Model released in 01/24 update, superseeds <see cref="AdaTextEmbedding" />, comes at a 33% price increase but is
    ///     even more powerful that <see cref="TextEmbedding3Small" />
    ///     Dimensions: 3072 (end of the sequence numbers can be removed and dimensions reduced up to 256 at a reasonable
    ///     perplexity increase)
    /// </summary>
    public static Model TextEmbedding3Large => new("text-embedding-3-large");

    /// <summary>
    ///     Most capable GPT-3.5 model and optimized for chat at 1/10th the cost of text-davinci-003. Will be updated with the
    ///     latest model iteration. Currently <see cref="GPT35_Turbo_1106" />
    /// </summary>
    public static Model GPT35_Turbo => new("gpt-3.5-turbo");

    /// <summary>
    ///     Snapshot of gpt-3.5-turbo from March 1st 2023. Unlike gpt-3.5-turbo, this model will not receive updates, and will
    ///     only be supported for a three-month period ending on June 1st 2023.
    /// </summary>
    public static Model ChatGPTTurbo0301 => new("gpt-3.5-turbo-0301");

    /// <summary>
    ///     More capable than any GPT-3.5 model, able to do more complex tasks, and optimized for chat. Will be updated with
    ///     the latest model iteration.
    /// </summary>
    public static Model GPT4 => new("gpt-4");

    /// <summary>
    ///     More capable than any GPT-3.5 model, able to do more complex tasks, and optimized for chat. Will be updated with
    ///     the latest model iteration.
    /// </summary>
    public static Model GPT4_VisionPreview => new("gpt-4-vision-preview");

    /// <summary>
    ///     More capable than any GPT-3.5 model, able to do more complex tasks, and optimized for chat.
    /// </summary>
    public static Model GPT4_1106_Preview => new("gpt-4-1106-preview");

    /// <summary>
    ///     More capable than any GPT-3.5 model, able to do more complex tasks, and optimized for chat. Will be updated with
    ///     the latest model iteration. Currently in limited beta so your OpenAI account needs to be whitelisted to use this.
    ///     Supports images.
    /// </summary>
    public static Model GPT4_Vision_Preview => new("gpt-4-vision-preview");

    /// <summary>
    ///     More capable than any GPT-3.5 model, able to do more complex tasks, and optimized for chat.
    ///     This model completes tasks like code generation more thoroughly than the previous preview model and is intended to
    ///     reduce cases of “laziness” where the model doesn’t complete a task.
    /// </summary>
    public static Model GPT4_4_0125_Preview => new("gpt-4-0125-preview");
    
    /// <summary>
    ///     GPT-4 Turbo with Vision model. Vision requests can now use JSON mode and function calling.
    ///     <see cref="GPT4_Turbo"/> currently points to this version.
    /// </summary>
    public static Model GPT4_4_0409_Preview => new("gpt-4-turbo-2024-04-09");

    /// <summary>
    ///     The latest GPT-4 Turbo model with vision capabilities. Vision requests can now use JSON mode and function calling.
    ///     Currently points to <see cref="GPT4_4_0409_Preview"/>.
    /// </summary>
    public static Model GPT4_Turbo => new("gpt-4-turbo");
    
    /// <summary>
    ///     Currently <see cref="GPT4_4_0125_Preview" /> will be auto updated to the latest GPT4 preview.
    /// </summary>
    public static Model GPT4_Turbo_Preview => new("gpt-4-turbo-preview");

    /// <summary>
    ///     Same capabilities as the base gpt-4 mode but with 4x the context length. Will be updated with the latest model
    ///     iteration.  Currently in limited beta so your OpenAI account needs to be whitelisted to use this.
    /// </summary>
    public static Model GPT4_32k_Context => new("gpt-4-32k");

    /// <summary>
    ///     Snapshot of gpt-3.5-turbo from June 13th 2023. This model allows the use of function calling as well as more
    ///     reliable steering via the system message.
    /// </summary>
    [Obsolete("Use Gpt_3_5_Turbo_1106 or Gpt_3_5_Turbo")]
    public static Model ChatGPTTurbo0613 => new("gpt-3.5-turbo-0613");

    /// <summary>
    ///     Snapshot of gpt-3.5-turbo from 11/6/23. This model allows the use of parallel function calling as well as more
    ///     reliable steering via the system message, and returns up to 4096 tokens.
    /// </summary>
    [Obsolete("Use Gpt_3_5_Turbo_1106 or Gpt_3_5_Turbo")]
    public static Model ChatGPTTurbo1106 => new("gpt-3.5-turbo-1106");

    /// <summary>
    ///     Snapshot of gpt-3.5-turbo from 25/01/24. Fixes a bug in <see cref="ChatGPTTurbo1106" /> with function calling.
    /// </summary>
    public static Model GPT35_Turbo_1106 => new("gpt-3.5-turbo-1106");

    /// <summary>
    ///     Snapshot of gpt-4 from June 13th 2023. This model allows the use of function calling as well as more reliable
    ///     steering via the system message.
    /// </summary>
    public static Model GPT4_0613 => new("gpt-4-0613");

    /// <summary>
    ///     Stable text moderation model that may provide lower accuracy compared to TextModerationLatest.
    ///     OpenAI states they will provide advanced notice before updating this model.
    /// </summary>
    public static Model TextModerationStable => new("text-moderation-stable");

    /// <summary>
    ///     The latest text moderation model. This model will be automatically upgraded over time.
    /// </summary>
    public static Model TextModerationLatest => new("text-moderation-latest");

    /// <summary>
    ///     The 01/24 text moderation model.
    /// </summary>
    public static Model TextModeration007 => new("text-moderation-007");

    /// <summary>
    ///     Whisper model. This model generates transcript from audio.
    /// </summary>
    public static Model Whisper_1 => new("whisper-1");

    /// <summary>
    ///     TTS-1 model. This model generates speech from text.
    /// </summary>
    public static Model TTS_1 => new("tts-1");

    /// <summary>
    ///     TTS-1-HD model. This model generates speech from text, higer quality than <see cref="TTS_1" />
    /// </summary>
    public static Model TTS_1_HD => new("tts-1-hd");

    /// <summary>
    ///     Dalle2 model. This model generates images.
    /// </summary>
    public static Model Dalle2 => new("dall-e-2");

    /// <summary>
    ///     Dalle2 model. This model generates images.
    /// </summary>
    public static Model Dalle3 => new("dall-e-3");
    
    /// <summary>
    ///     Dalle2 model. This model generates images.
    /// </summary>
    public static Model Claude3Sonnet => new("claude-3-sonnet-20240229", LLmProviders.Anthropic);
    
    /// <summary>
    ///     Dalle2 model. This model generates images.
    /// </summary>
    public static Model Claude3Opus => new("claude-3-opus-20240229", LLmProviders.Anthropic);
    

    /// <summary>
    ///     A custom model, equivalent of instantiating <see cref="Model" />
    /// </summary>
    /// <param name="name">The name of the model</param>
    /// <returns></returns>
    public static Model Custom(string name, string? ownedBy = null)
    {
        return new Model(name);
    }

    /// <summary>
    ///     Allows an model to be implicitly cast to the string of its <see cref="Name" />
    /// </summary>
    /// <param name="model">The <see cref="Model" /> to cast to a string.</param>
    public static implicit operator string(Model model)
    {
        return model.Name;
    }
    
    /// <summary>
    /// Looks up the model provider. Only works for known models.
    /// </summary>
    /// <param name="modelName"></param>
    /// <returns></returns>
    public static IModel? GetModel(string? modelName)
    {
        // [todo] bake all classes of models into one map statically
        if (modelName is not null && ChatModel.AllModelsMap.TryGetValue(modelName, out IModel? protoModel))
        {
            return protoModel;
        }

        return null;
    }
    
    /// <summary>
    ///     Allows a string to be implicitly cast as an <see cref="Model" /> with that <see cref="name" />
    /// </summary>
    /// <param name="name">The id/<see cref="name" /> to use</param>
    public static implicit operator Model(string? name)
    {
        return new Model(name ?? string.Empty, name is null ? LLmProviders.OpenAi : GetModel(name)?.Provider ?? LLmProviders.OpenAi);
    }

    /// <summary>
    ///     Gets more details about this Model from the API, specifically properties such as <see cref="OwnedBy" /> and
    ///     permissions.
    /// </summary>
    /// <param name="api">An instance of the API with authentication in order to call the endpoint.</param>
    /// <returns>Asynchronously returns a Model with all relevant properties filled in</returns>
    public Task<Model> RetrieveModelDetailsAsync(TornadoApi api)
    {
        return api.Models.RetrieveModelDetailsAsync(Name);
    }
}

/// <summary>
///     Permissions for using the model
/// </summary>
public class Permissions
{
	/// <summary>
	///     Permission Id (not to be confused with ModelId)
	/// </summary>
	[JsonProperty("id")]
    public string Id { get; set; }

	/// <summary>
	///     Object type, should always be 'model_permission'
	/// </summary>
	[JsonProperty("object")]
    public string? Object { get; set; }

    /// The time when the permission was created
    [JsonIgnore]
    public DateTime Created => DateTimeOffset.FromUnixTimeSeconds(CreatedUnixTime).DateTime;

    /// <summary>
    ///     Unix timestamp for creation date/time
    /// </summary>
    [JsonProperty("created")]
    public long CreatedUnixTime { get; set; }

    /// <summary>
    ///     Can the model be created?
    /// </summary>
    [JsonProperty("allow_create_engine")]
    public bool AllowCreateEngine { get; set; }

    /// <summary>
    ///     Does the model support temperature sampling?
    ///     https://beta.openai.com/docs/api-reference/completions/create#completions/create-temperature
    /// </summary>
    [JsonProperty("allow_sampling")]
    public bool AllowSampling { get; set; }

    /// <summary>
    ///     Does the model support logprobs?
    ///     https://beta.openai.com/docs/api-reference/completions/create#completions/create-logprobs
    /// </summary>
    [JsonProperty("allow_logprobs")]
    public bool AllowLogProbs { get; set; }

    /// <summary>
    ///     Does the model support search indices?
    /// </summary>
    [JsonProperty("allow_search_indices")]
    public bool AllowSearchIndices { get; set; }

    [JsonProperty("allow_view")] public bool AllowView { get; set; }

    /// <summary>
    ///     Does the model allow fine tuning?
    ///     https://beta.openai.com/docs/api-reference/fine-tunes
    /// </summary>
    [JsonProperty("allow_fine_tuning")]
    public bool AllowFineTuning { get; set; }

    /// <summary>
    ///     Is the model only allowed for a particular organization? May not be implemented yet.
    /// </summary>
    [JsonProperty("organization")]
    public string Organization { get; set; }

    /// <summary>
    ///     Is the model part of a group? Seems not implemented yet. Always null.
    /// </summary>
    [JsonProperty("group")]
    public string Group { get; set; }

    [JsonProperty("is_blocking")] 
    public bool IsBlocking { get; set; }
}