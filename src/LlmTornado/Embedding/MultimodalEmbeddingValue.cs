using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Embedding;

/// <summary>
/// Base class for a multimodal embedding value.
/// </summary>
public abstract class MultimodalEmbeddingValue { }

/// <summary>
/// Represents an embedding vector of floats.
/// </summary>
public class MultimodalEmbeddingValueFloat : MultimodalEmbeddingValue
{
    /// <summary>
    /// The embedding values.
    /// </summary>
    public float[] Values { get; }

    internal MultimodalEmbeddingValueFloat(float[] values)
    {
        Values = values;
    }
}

/// <summary>
/// Represents a base64 encoded embedding vector.
/// </summary>
public class MultimodalEmbeddingValueString : MultimodalEmbeddingValue
{
    /// <summary>
    /// The base64 encoded embedding.
    /// </summary>
    public string Base64 { get; }
    
    internal MultimodalEmbeddingValueString(string base64)
    {
        Base64 = base64;
    }
}

internal class MultimodalEmbeddingValueConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(MultimodalEmbeddingValue);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);

        switch (token.Type)
        {
            case JTokenType.String:
                return new MultimodalEmbeddingValueString(token.Value<string>()!);
            case JTokenType.Array:
                return new MultimodalEmbeddingValueFloat(token.ToObject<float[]>(serializer)!);
        }

        throw new JsonSerializationException("Unexpected token type for embedding vector.");
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}