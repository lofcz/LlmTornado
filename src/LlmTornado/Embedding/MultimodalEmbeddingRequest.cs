using System;
using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Embedding.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Embedding;

/// <summary>
/// A request for the multimodal embeddings API.
/// </summary>
public class MultimodalEmbeddingRequest : ISerializableRequest
{
    /// <summary>
    /// Creates a new request for the multimodal embeddings API.
    /// </summary>
    /// <param name="model">The model to use for the embeddings.</param>
    /// <param name="inputs">A list of multimodal inputs to be vectorized.</param>
    public MultimodalEmbeddingRequest(MultimodalEmbeddingModel model, List<MultimodalInput> inputs)
    {
        Model = model;
        Inputs = inputs;
    }

    /// <summary>
    /// The model to use for the embeddings.
    /// </summary>
    [JsonProperty("model")]
    [JsonConverter(typeof(IModelConverter))]
    public MultimodalEmbeddingModel Model { get; set; }

    /// <summary>
    /// A list of multimodal inputs to be vectorized.
    /// </summary>
    [JsonProperty("inputs")]
    public List<MultimodalInput> Inputs { get; set; }

    /// <summary>
    /// Type of the input text.
    /// </summary>
    [JsonProperty("input_type")]
    public MultimodalEmbeddingInputType? InputType { get; set; }

    /// <summary>
    /// Whether to truncate the inputs to fit within the context length.
    /// </summary>
    [JsonProperty("truncation")]
    public bool? Truncation { get; set; }

    /// <summary>
    /// Format in which the embeddings are encoded.
    /// </summary>
    [JsonProperty("output_encoding")]
    public MultimodalEmbeddingEncodingFormat? OutputEncoding { get; set; }
    
    [JsonIgnore]
    internal string? UrlOverride { get; set; }

    /// <summary>
    /// Serializes the request.
    /// </summary>
    public TornadoRequestContent Serialize(IEndpointProvider provider, RequestSerializeOptions options)
    {
        return SerializeInternal(provider, options);
    }
    
    /// <summary>
    /// Serializes the request.
    /// </summary>
    public TornadoRequestContent Serialize(IEndpointProvider provider)
    {
        return SerializeInternal(provider, null);
    }
    
    internal TornadoRequestContent SerializeInternal(IEndpointProvider provider, RequestSerializeOptions? options)
    {
        return new TornadoRequestContent(this.ToJson(options?.Pretty ?? false), Model, UrlOverride ?? EndpointBase.BuildRequestUrl(null, provider, CapabilityEndpoints.MultimodalEmbeddings, Model), provider, CapabilityEndpoints.MultimodalEmbeddings);
    }
}

/// <summary>
/// Represents a single multimodal input.
/// </summary>
public class MultimodalInput
{
    /// <summary>
    /// A sequence of text and images.
    /// </summary>
    [JsonProperty("content")]
    public List<MultimodalContent> Content { get; set; }

    /// <summary>
    /// Creates a new multimodal input.
    /// </summary>
    /// <param name="content">A sequence of text and images.</param>
    public MultimodalInput(List<MultimodalContent> content)
    {
        Content = content;
    }
}

/// <summary>
/// Base class for a piece of multimodal content.
/// </summary>
[JsonConverter(typeof(MultimodalContentConverter))]
public abstract class MultimodalContent
{
    /// <summary>
    /// The type of the content.
    /// </summary>
    [JsonProperty("type")]
    public abstract string Type { get; }
}

/// <summary>
/// Represents a piece of text content.
/// </summary>
public class MultimodalContentText : MultimodalContent
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override string Type => "text";

    /// <summary>
    /// The text content.
    /// </summary>
    [JsonProperty("text")]
    public string Text { get; set; }

    /// <summary>
    /// Creates a new piece of text content.
    /// </summary>
    /// <param name="text">The text content.</param>
    public MultimodalContentText(string text)
    {
        Text = text;
    }
}

/// <summary>
/// Represents a piece of image content from a URL.
/// </summary>
public class MultimodalContentImageUrl : MultimodalContent
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override string Type => "image_url";
    
    /// <summary>
    /// The URL of the image.
    /// </summary>
    [JsonProperty("image_url")]
    public string ImageUrl { get; set; }

    /// <summary>
    /// Creates a new piece of image content from a URL.
    /// </summary>
    /// <param name="imageUrl">The URL of the image.</param>
    public MultimodalContentImageUrl(string imageUrl)
    {
        ImageUrl = imageUrl;
    }
}

/// <summary>
/// Represents a piece of image content from a base64 encoded string.
/// </summary>
public class MultimodalContentImageBase64 : MultimodalContent
{
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    public override string Type => "image_base64";

    /// <summary>
    /// The base64 encoded image.
    /// </summary>
    [JsonProperty("image_base64")]
    public string ImageBase64 { get; set; }

    /// <summary>
    /// Creates a new piece of image content from a base64 encoded string.
    /// </summary>
    /// <param name="imageBase64">The base64 encoded image.</param>
    public MultimodalContentImageBase64(string imageBase64)
    {
        ImageBase64 = imageBase64;
    }
}

internal class MultimodalContentConverter : JsonConverter<MultimodalContent>
{
    public override void WriteJson(JsonWriter writer, MultimodalContent? value, JsonSerializer serializer)
    {
        JObject o = new JObject
        {
            { "type", value?.Type }
        };

        switch (value)
        {
            case MultimodalContentText text:
                o.Add("text", text.Text);
                break;
            case MultimodalContentImageUrl imageUrl:
                o.Add("image_url", imageUrl.ImageUrl);
                break;
            case MultimodalContentImageBase64 imageBase64:
                o.Add("image_base64", imageBase64.ImageBase64);
                break;
        }

        o.WriteTo(writer);
    }

    public override MultimodalContent? ReadJson(JsonReader reader, Type objectType, MultimodalContent? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return existingValue;
    }
}