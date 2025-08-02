using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Embedding;

/// <summary>
/// Represents a response from the contextual embeddings API.
/// </summary>
public class ContextualEmbeddingResult
{
    /// <summary>
    /// The object type, which is always "list".
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; }

    /// <summary>
    /// An array of contextualized embeddings.
    /// </summary>
    [JsonProperty("data")]
    public List<ContextualEmbeddingData> Data { get; set; }

    /// <summary>
    /// Name of the model.
    /// </summary>
    [JsonProperty("model")]
    public string Model { get; set; }

    /// <summary>
    /// The total number of tokens used for computing the embeddings.
    /// </summary>
    [JsonProperty("usage")]
    public Usage Usage { get; set; }
}

/// <summary>
/// Represents a single contextualized embedding result.
/// </summary>
public class ContextualEmbeddingData
{
    /// <summary>
    /// The object type, which is always "list".
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; }

    /// <summary>
    /// An array of embedding objects.
    /// </summary>
    [JsonProperty("data")]
    public List<ContextualEmbedding> Data { get; set; }

    /// <summary>
    /// An integer representing the index of the query or document within the list of queries or documents, respectively.
    /// </summary>
    [JsonProperty("index")]
    public int Index { get; set; }
}

/// <summary>
/// Represents a single embedding vector.
/// </summary>
public class ContextualEmbedding
{
    /// <summary>
    /// The object type, which is always "embedding".
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; }

    /// <summary>
    /// The embedding vector as an array of floats. This is populated when the response is not encoded in base64 and the data type is float.
    /// </summary>
    [JsonProperty("embedding")]
    [JsonConverter(typeof(EmbeddingVectorConverter))]
    public ContextualEmbeddingValue ContextualEmbeddingVector { get; set; }

    /// <summary>
    /// An integer representing the index of the query or the contextualized chunk embedding within the list of embeddings from the same document.
    /// </summary>
    [JsonProperty("index")]
    public int Index { get; set; }
}


internal class EmbeddingVectorConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(ContextualEmbeddingValue);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        JToken token = JToken.Load(reader);

        switch (token.Type)
        {
            case JTokenType.String:
                return new ContextualEmbeddingValueString(token.Value<string>()!);
            case JTokenType.Array:
            {
                JArray array = (JArray)token;

                if (array.Count == 0)
                {
                    return new ContextualEmbeddingValueFloat([]);
                }

                switch (array.First)
                {
                    case { Type: JTokenType.Float }:
                        return new ContextualEmbeddingValueFloat(token.ToObject<float[]>(serializer)!);
                    case { Type: JTokenType.Integer }:
                        return new ContextualEmbeddingValueInt(token.ToObject<int[]>(serializer)!);
                }

                break;
            }
        }

        throw new JsonSerializationException("Unexpected token type for embedding vector.");
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        return;
    }
}