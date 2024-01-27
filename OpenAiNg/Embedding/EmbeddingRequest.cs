using System;
using Newtonsoft.Json;
using OpenAiNg.Models;

namespace OpenAiNg.Embedding;

/// <summary>
///     Represents a request to the Completions API. Matches with the docs at
///     <see href="https://platform.openai.com/docs/api-reference/embeddings">the OpenAI docs</see>
/// </summary>
public class EmbeddingRequest
{
	/// <summary>
	///     Cretes a new, empty <see cref="EmbeddingRequest" />
	/// </summary>
	public EmbeddingRequest()
    {
    }

	/// <summary>
	///     Creates a new <see cref="EmbeddingRequest" /> with the specified parameters
	/// </summary>
	/// <param name="model">
	///     The model to use. You can use <see cref="ModelsEndpoint.GetModelsAsync()" /> to see all of your
	///     available models, or use a standard model like <see cref="Model.TextEmbedding3Small" />, <see cref="Model.TextEmbedding3Large"/> or (legacy) <see cref="Model.AdaTextEmbedding" />.
	/// </param>
	/// <param name="input">The prompt to transform</param>
	/// <param name="dimensions">The dimensions length to return. The maximum value is the size supported by model. This is only supported in <see cref="Model.TextEmbedding3Small" /> and <see cref="Model.TextEmbedding3Large"/></param>
	public EmbeddingRequest(Model model, string input, int? dimensions = null)
    {
        Model = model;
        Input = input;

        if (model.ModelID == Models.Model.AdaTextEmbedding.ModelID)
        {
	        dimensions = null;
        }
    }

	/// <summary>
	///     Creates a new <see cref="EmbeddingRequest" /> with the specified input and the
	///     <see cref="Model.AdaTextEmbedding" /> model.
	/// </summary>
	/// <param name="input">The prompt to transform</param>
	public EmbeddingRequest(string input)
    {
        Model = Models.Model.AdaTextEmbedding;
        Input = input;
    }
	
	/// <summary>
	///     Creates a new <see cref="EmbeddingRequest" /> with the specified input and the
	///     <see cref="Model.TextEmbedding3Large" /> model.
	/// </summary>
	/// <param name="input">The prompt to transform</param>
	/// <param name="dimensions">The dimensions length to return. The maximum value is the size supported by model. This is only supported in <see cref="Model.TextEmbedding3Small" /> and <see cref="Model.TextEmbedding3Large"/></param>
	public EmbeddingRequest(string input, int dimensions)
	{
		Model = Models.Model.TextEmbedding3Large;
		Input = input;
		Dimensions = dimensions;
	}

	/// <summary>
	///     ID of the model to use. You can use <see cref="ModelsEndpoint.GetModelsAsync()" /> to see all of your available
	///     models, or use a standard model like <see cref="Model.TextEmbedding3Small" />, <see cref="Model.TextEmbedding3Large"/> or (legacy) <see cref="Model.AdaTextEmbedding" />.
	/// </summary>
	[JsonProperty("model")]
    public string Model { get; set; }

	/// <summary>
	///     Main text to be embedded
	/// </summary>
	[JsonProperty("input")]
    public string Input { get; set; }
	
	/// <summary>
	///     The dimensions length to be returned
	/// </summary>
	[JsonProperty("dimensions")]
	public int? Dimensions { get; set; }
}