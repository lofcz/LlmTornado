using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Code;
using LlmTornado.Embedding.Vendors.Cohere;
using LlmTornado.Embedding.Vendors.Google;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;

namespace LlmTornado.Embedding;

/// <summary>
///     Represents an embedding result returned by the Embedding API.
/// </summary>
public class EmbeddingResult : ApiResultBase
{
	/// <summary>
	///     List of results of the embedding
	/// </summary>
	[JsonProperty("data")]
	public List<EmbeddingEntry> Data { get; set; } = [];

	/// <summary>
	///     Usage statistics of how many tokens have been used for this request
	/// </summary>
	[JsonProperty("usage")]
    public Usage Usage { get; set; }

	internal static EmbeddingResult? Deserialize(LLmProviders provider, string jsonData, string? postData)
	{
		return provider switch
		{
			LLmProviders.OpenAi => JsonConvert.DeserializeObject<EmbeddingResult>(jsonData),
			//LLmProviders.Anthropic => JsonConvert.DeserializeObject<VendorAnthropicChatResult>(jsonData)?.ToChatResult(postData),
			LLmProviders.Cohere => JsonConvert.DeserializeObject<VendorCohereEmbeddingResult>(jsonData)?.ToResult(postData),
			LLmProviders.Google => JsonConvert.DeserializeObject<VendorGoogleEmbeddingResult>(jsonData)?.ToResult(postData),
			_ => JsonConvert.DeserializeObject<EmbeddingResult>(jsonData)
		};
	}
	
	/// <summary>
	///     Allows an EmbeddingResult to be implicitly cast to the array of floats repsresenting the first ebmedding result
	/// </summary>
	/// <param name="embeddingResult">The <see cref="EmbeddingResult" /> to cast to an array of floats.</param>
	public static implicit operator float[](EmbeddingResult embeddingResult)
    {
        return embeddingResult.Data.FirstOrDefault()?.Embedding;
    }
}

/// <summary>
///		Billed units for the embedding request.
/// </summary>
public class EmbeddingUsage : Usage
{
	internal EmbeddingUsage(VendorCohereUsage usage)
	{
		PromptTokens = usage.BilledUnits.InputTokens;
		TotalTokens = PromptTokens;
	}
}

/// <summary>
///     Data returned from the Embedding API.
/// </summary>
public class EmbeddingEntry
{
	/// <summary>
	///     Type of the response.
	/// </summary>
	[JsonProperty("object")]
    public string? Object { get; set; }

	/// <summary>
	///     The input text represented as a vector (list) of floating point numbers.<br/>
	///		Note: If your request asked for embeddings in a different data type, such as <see cref="byte"/> or <see cref="sbyte"/>, float entries can be cast to this type.
	/// </summary>
	[JsonProperty("embedding")]
    public float[] Embedding { get; set; }
	
	/// <summary>
	///     Index.
	/// </summary>
	[JsonProperty("index")]
    public int Index { get; set; }
}