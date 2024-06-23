using System;
using System.Collections.Generic;
using LlmTornado.Models;
using Newtonsoft.Json;

namespace LlmTornado.Code.Models;

public class ModelVendor<T> where T : ModelBase
{
    public List<T> Models { get; set; }

    public ModelVendor()
    {
        Models = [];
    }

    public ModelVendor(List<T> models)
    {
        Models = models;
    }
}

public interface IModel
{
    public string Name { get; }
    public LLmProviders Provider { get; }
}

/// <summary>
/// Shared base class for vendor model providers.
/// </summary>
public abstract class BaseVendorModelProvider : IVendorModelProvider
{
    /// <summary>
    /// All models owned by the provider.
    /// </summary>
    public abstract List<IModel> AllModels { get; }

    /// <summary>
    /// Checks whether the vendor owns given model.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public bool OwnsModel(IModel model)
    {
        return OwnsModel(model.Name);
    }

    /// <summary>
    /// Checks whether the vendor owns given model.
    /// </summary>
    /// <param name="model"></param>
    /// <returns></returns>
    public abstract bool OwnsModel(string model);
}

interface IVendorModelProvider
{
    public List<IModel> AllModels { get; }
    public bool OwnsModel(IModel model);
    public bool OwnsModel(string model);
}

interface IVendorModelClassProvider
{
    public List<IModel> AllModels { get; }
}

/// <summary>
/// Represents a base shared between all LLMs.
/// </summary>
public abstract class ModelBase : IModel
{
    /// <summary>
    ///     The id/name of the model.
    /// </summary>
    [JsonProperty("id")]
    public string Name { get; set; }

    /// <summary>
    ///     The owner of this model.  Generally "openai" is a generic OpenAI model, or the organization if a custom or
    ///     fine-tuned model.
    /// </summary>
    [JsonProperty("owned_by")]
    public string? OwnedBy { get; set; }

    /// <summary>
    ///     The type of object. Should always be 'model'.
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; }

    /// <summary>
    ///     The time when the model was created.
    /// </summary>
    [JsonIgnore]
    public DateTime? Created => CreatedUnixTime.HasValue ? DateTimeOffset.FromUnixTimeSeconds(CreatedUnixTime.Value).DateTime : null;

    /// <summary>
    ///     The type of object. Should always be 'model'.
    /// </summary>
    [JsonIgnore]
    public LLmProviders Provider { get; set; }
    
    /// <summary>
    ///     The time when the model was created in unix epoch format.
    /// </summary>
    [JsonProperty("created")]
    public long? CreatedUnixTime { get; set; }

    /// <summary>
    ///     Permissions for use of the model.
    /// </summary>
    [JsonProperty("permission")]
    public List<Permissions>? Permission { get; set; }
    
    /// <summary>
    ///     Maximum context length the model supports. For self-hosted models with ROPE support,
    ///     set this to the current ROPE value. This can be used to trim conversations to fit into
    ///     the supported context size.
    /// </summary>
    [JsonIgnore]
    public int? ContextTokens { get; set; }

    /// <summary>
    ///     Allows a model to be implicitly cast to the string of its <see cref="Name" />
    /// </summary>
    /// <param name="model">The <see cref="Model" /> to cast to a string.</param>
    public static implicit operator string(ModelBase model)
    {
        return model.Name;
    }
}

/// <summary>
///     Shared base for embedding models.
/// </summary>
public abstract class ModelEmbeddingBase : ModelBase
{
    /// <summary>
    ///     The length of the output vector.
    /// </summary>
    [JsonIgnore]
    public int? OutputDimensions { get; set; } 
}