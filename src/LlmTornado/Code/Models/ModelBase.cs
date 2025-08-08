using System;
using System.Collections.Generic;
using System.Linq;
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

/// <summary>
/// LLM model.
/// </summary>
public interface IModel : IEquatable<IModel>
{
    /// <summary>
    /// Name of the model. This must be globally unique.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// In case a model is hosted by multiple vendor, this is the vendor-specific name.
    /// </summary>
    public string? ApiName { get; }
    
    /// <summary>
    /// Gets the vendor specific name.
    /// </summary>
    public string GetApiName { get; }
    
    /// <summary>
    /// Provider hosting the model.
    /// </summary>
    public LLmProviders Provider { get; }
    
    /// <summary>
    /// Aliases of the model.
    /// </summary>
    public List<string>? Aliases { get; }
    
    /// <summary>
    /// Tracks whether the model was resolved by name and needs to be resolved at the time <see cref="TornadoApi"/> becomes available.
    /// </summary>
    public bool OptimisticallyResolved { get; set; }
}

/// <summary>
/// Shared base class for vendor model providers.
/// </summary>
public abstract class BaseVendorModelProvider : IVendorModelProvider
{
    /// <summary>
    /// Provider.
    /// </summary>
    public abstract LLmProviders Provider { get; }
    
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
    [JsonIgnore]
    public string Name { get; 
        #if MODERN
        internal set; 
        #else
        set;
        #endif
    }
    
    /// <summary>
    /// Gets the vendor specific name.
    /// </summary>
    [JsonProperty("id")]
    public string GetApiName => ApiName ?? Name;
    
    /// <summary>
    ///     In case a model is hosted by multiple vendor, this is the vendor-specific name.
    /// </summary>
    [JsonIgnore]
    public string? ApiName { get; 
#if MODERN
        internal set; 
#else
        set;
#endif
    }

    /// <summary>
    ///     Aliases of the model.
    /// </summary>
    [JsonIgnore]
    public List<string>? Aliases { get; 
#if MODERN
        internal set; 
#else
        set;
#endif
    }
    
    /// <inheritdoc cref="IModel.OptimisticallyResolved" />
    [JsonIgnore]
    public bool OptimisticallyResolved { get; set; }

    /// <summary>
    ///     The owner of this model.  Generally "openai" is a generic OpenAI model, or the organization if a custom or
    ///     fine-tuned model.
    /// </summary>
    [JsonProperty("owned_by")]
    public string? OwnedBy { get; 
#if MODERN
        internal set; 
#else
        set;
#endif
    }

    /// <summary>
    ///     The type of object. Should always be 'model'.
    /// </summary>
    [JsonProperty("object")]
    public string Object { get;
#if MODERN
        internal set; 
#else
        set;
#endif
    }

    /// <summary>
    ///     The time when the model was created.
    /// </summary>
    [JsonIgnore]
    public DateTime? Created => CreatedUnixTime.HasValue ? DateTimeOffset.FromUnixTimeSeconds(CreatedUnixTime.Value).DateTime : null;

    /// <summary>
    ///     The type of object. Should always be 'model'.
    /// </summary>
    [JsonIgnore]
    public LLmProviders Provider { get; internal set; }
    
    /// <summary>
    ///     The time when the model was created in unix epoch format.
    /// </summary>
    [JsonProperty("created")]
    public long? CreatedUnixTime { get; 
#if MODERN
        internal set; 
#else
        set;
#endif
    }

    /// <summary>
    ///     Permissions for use of the model.
    /// </summary>
    [JsonProperty("permission")]
    public List<Permissions>? Permission { get;
#if MODERN
        internal set; 
#else
        set;
#endif
    }
    
    /// <summary>
    ///     Maximum context length the model supports. For self-hosted models with ROPE support,
    ///     set this to the current ROPE value. This can be used to trim conversations to fit into
    ///     the supported context size.
    /// </summary>
    [JsonIgnore]
    public int? ContextTokens { get;
#if MODERN
        internal set; 
#else
        set;
#endif
    }

    /// <summary>
    ///     Allows a model to be implicitly cast to the string of its <see cref="Name" />
    /// </summary>
    /// <param name="model">The <see cref="Model" /> to cast to a string.</param>
    public static implicit operator string(ModelBase model)
    {
        return model.Name;
    }

    /// <summary>
    /// Whether two base models are equal.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    protected bool Equals(ModelBase other)
    {
        return other.Name == Name && other.ApiName == ApiName;
    }

    /// <summary>
    /// Whether two IModels are equal
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(IModel? other)
    {
        if (other is null)
        {
            return false;
        }

        return (other.Name == Name && other.ApiName == ApiName) || ((other.Aliases is not null && other.Aliases.Contains(Name)) || (other.Aliases is not null && Aliases is not null && other.Aliases.Any(x => Aliases.Contains(x))));
    }

    /// <summary>
    /// Whether model and another object are equal.
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
        if (obj is null)
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }
        
        return Equals((ModelBase)obj);
    }

    /// <summary>
    /// Hash code for models.
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return HashCode.Combine(Name, ApiName);
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
    
    /// <summary>
    ///     The matryoshka lengths of the output vector.
    /// </summary>
    [JsonIgnore]
    public List<int>? MatryoshkaDimensions { get; set; } 
}