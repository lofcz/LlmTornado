using System;
using System.Collections.Generic;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Code;
using LlmTornado.Embedding.Models;
using LlmTornado.Embedding.Vendors.OpenAi;
using LlmTornado.Models;
using Newtonsoft.Json;

namespace LlmTornado.Embedding;

/// <summary>
///     Represents a request to the Embeddings API.
/// </summary>
public class EmbeddingRequest
{
	/// <summary>
	///     Creates a new, empty <see cref="EmbeddingRequest" />.
	/// </summary>
	public EmbeddingRequest()
    {
    }

	/// <summary>
	///     Creates a new <see cref="EmbeddingRequest" /> with the specified parameters
	/// </summary>
	/// <param name="model">
	///     The model to use. You can use <see cref="ModelsEndpoint.GetModelsAsync()" /> to see all of your
	///     available models, or use a standard model like <see cref="Model.TextEmbedding3Small" />,
	///     <see cref="Model.TextEmbedding3Large" /> or (legacy) <see cref="Model.AdaTextEmbedding" />.
	/// </param>
	/// <param name="input">The prompt to transform</param>
	/// <param name="dimensions">
	///     The dimensions length to return. The maximum value is the size supported by model. This is
	///     only supported in newer embedding models.
	/// </param>
	public EmbeddingRequest(EmbeddingModel model, string input, int dimensions)
    {
        Model = model;
        InputScalar = input;
        Dimensions = dimensions;
    }

	/// <summary>
	///     Creates a new <see cref="EmbeddingRequest" /> with the specified input and the
	///     <see cref="Model.AdaTextEmbedding" /> model.
	/// </summary>
	/// <param name="model">The model to use</param>
	/// <param name="input">The prompt to transform</param>
	public EmbeddingRequest(EmbeddingModel model, string input)
    {
        Model = model;
        InputScalar = input;
    }
	
	/// <summary>
	///     Creates a new <see cref="EmbeddingRequest" /> with the specified input and the
	///     <see cref="Model.AdaTextEmbedding" /> model.
	/// </summary>
	/// <param name="model">The model to use</param>
	/// <param name="input">The prompt to transform</param>
	public EmbeddingRequest(EmbeddingModel model, IEnumerable<string> input)
	{
		Model = model;
		InputVector = input;
	}
	
	/// <summary>
	///     Creates a new <see cref="EmbeddingRequest" /> with the specified input and the
	///     <see cref="Model.AdaTextEmbedding" /> model.
	/// </summary>
	/// <param name="model">The model to use</param>
	/// <param name="input">The prompt to transform</param>
	/// <param name="dimensions">Output dimensions</param>
	public EmbeddingRequest(EmbeddingModel model, IEnumerable<string> input, int dimensions)
	{
		Model = model;
		InputVector = input;
		Dimensions = dimensions;
	}

	/// <summary>
	///     Model to use.
	/// </summary>
	[JsonProperty("model")]
	[JsonConverter(typeof(ModelJsonConverter))]
    public EmbeddingModel Model { get; set; }

	/// <summary>
	///     Main text to be embedded
	/// </summary>
	[JsonIgnore]
    public string? InputScalar { get; set; }
	
	/// <summary>
	///     Main text to be embedded
	/// </summary>
	[JsonIgnore]
	public IEnumerable<string>? InputVector { get; set; }

	[JsonProperty("input")]
	internal object InputSerialized { get; set; }

	/// <summary>
	///     The dimensions length to be returned. Only supported by newer models.
	/// </summary>
	[JsonProperty("dimensions")]
    public int? Dimensions { get; set; }
	
	[JsonIgnore]
	internal string? UrlOverride { get; set; }
	
	/// <summary>
	///		Serializes the embedding request into the request body, based on the conventions used by the LLM provider.
	/// </summary>
	/// <param name="provider"></param>
	/// <returns></returns>
	public TornadoRequestContent Serialize(IEndpointProvider provider)
	{
		string content = provider.Provider switch
		{
			LLmProviders.OpenAi => JsonConvert.SerializeObject(new VendorOpenAiEmbeddingRequest(this, provider), EndpointBase.NullSettings),
			//LLmProviders.Anthropic => JsonConvert.SerializeObject(new VendorAnthropicEmbeddingRequest(this, provider), EndpointBase.NullSettings),
			LLmProviders.Cohere => JsonConvert.SerializeObject(new VendorCohereEmbeddingRequest(this, provider), EndpointBase.NullSettings),
			//LLmProviders.Google => JsonConvert.SerializeObject(new VendorGoogleEmbeddingRequest(this, provider), EndpointBase.NullSettings),
			_ => string.Empty
		};
		
		return new TornadoRequestContent(content, UrlOverride);
	}
	
	internal class ModelJsonConverter : JsonConverter<EmbeddingModel>
	{
		public override void WriteJson(JsonWriter writer, EmbeddingModel? value, JsonSerializer serializer)
		{
			writer.WriteValue(value?.Name);
		}

		public override EmbeddingModel? ReadJson(JsonReader reader, Type objectType, EmbeddingModel? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return existingValue;
		}
	}
}