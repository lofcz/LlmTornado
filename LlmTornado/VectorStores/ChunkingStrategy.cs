using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace LlmTornado.VectorStores;

/// <summary>
/// Base class for chunking strategies
/// </summary>
public abstract class ChunkingStrategy
{
    /// <summary>
    /// The type of chunking strategy
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = null!;
}

/// <summary>
/// Defines the custom converter for polymorphic chunking strategy deserialization
/// </summary>
public class ChunkingStrategyConverter : JsonConverter<ChunkingStrategy>
{
    /// <summary>
    ///     Writes the JSON representation of the object.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="serializer"></param>
    public override void WriteJson(JsonWriter writer, ChunkingStrategy? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    /// <summary>
    ///     Converter reads the JSON representation of the object.
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="objectType"></param>
    /// <param name="existingValue"></param>
    /// <param name="hasExistingValue"></param>
    /// <param name="serializer"></param>
    /// <returns></returns>
    /// <exception cref="JsonSerializationException"></exception>
    public override ChunkingStrategy? ReadJson(JsonReader reader, Type objectType, ChunkingStrategy? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);
        string? strategyType = jsonObject["type"]?.ToString();
        return strategyType switch
        {
            "static" => jsonObject.ToObject<StaticChunkingStrategy>(),
            _ => throw new JsonSerializationException($"Unknown chunking strategy type: {strategyType}")
        };
    }
}