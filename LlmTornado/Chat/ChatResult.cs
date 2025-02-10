using System.Collections.Generic;
using LlmTornado.Code;
using LlmTornado.Vendor.Anthropic;
using LlmTornado;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Vendor.Google;
using Newtonsoft.Json;

namespace LlmTornado.Chat;

/// <summary>
///     Represents a result from calling the Chat API
/// </summary>
public class ChatResult : ApiResultBase
{
	/// <summary>
	///     The identifier of the result, which may be used during troubleshooting
	/// </summary>
	[JsonProperty("id")]
    public string? Id { get; set; }

	/// <summary>
	///     The list of choices that the user was presented with during the chat interaction
	/// </summary>
	[JsonProperty("choices")]
    public List<ChatChoice>? Choices { get; set; }

	/// <summary>
	///     The usage statistics for the chat interaction
	/// </summary>
	[JsonProperty("usage")]
    public ChatUsage? Usage { get; set; }
	
	/// <summary>
	///     Fingerprint of the system used to resolve the request. Currently supported only by OpenAI.
	/// </summary>
	[JsonProperty("system_fingerprint")]
	public string? SystemFingerprint { get; set; }
	
	/// <summary>
	///		Features supported only by a specific/few providers with no shared equivalent.
	/// </summary>
	[JsonIgnore]
	public ChatResponseVendorExtensions? VendorExtensions { get; set; }

	[JsonIgnore]
	internal ChatResultStreamInternalKinds? StreamInternalKind { get; set; }
	
	/// <summary>
	///     A convenience method to return the content of the message in the first choice of this response
	/// </summary>
	/// <returns>The content of the message, not including <see cref="ChatMessageRoles" />.</returns>
	public override string? ToString()
    {
        return Choices is { Count: > 0 } ? Choices[0].ToString() : null;
    }

	internal static ChatResult? Deserialize(LLmProviders provider, string jsonData, string? postData)
	{
		return provider switch
		{
			LLmProviders.OpenAi => JsonConvert.DeserializeObject<ChatResult>(jsonData),
			LLmProviders.Anthropic => JsonConvert.DeserializeObject<VendorAnthropicChatResult>(jsonData)?.ToChatResult(postData),
			LLmProviders.Cohere => JsonConvert.DeserializeObject<VendorCohereChatResult>(jsonData)?.ToChatResult(postData),
			LLmProviders.Google => JsonConvert.DeserializeObject<VendorGoogleChatResult>(jsonData)?.ToChatResult(postData),
			_ => JsonConvert.DeserializeObject<ChatResult>(jsonData)
		};
	}
}

/// <summary>
///     A message received from the API, including the message text, index, and reason why the message finished.
/// </summary>
public class ChatChoice
{
	/// <summary>
	///     The index of the choice in the list of choices
	/// </summary>
	[JsonProperty("index")]
    public int Index { get; set; }

	/// <summary>
	///     The message that was presented to the user as the choice
	/// </summary>
	[JsonProperty("message")]
    public ChatMessage? Message { get; set; }

	/// <summary>
	///     The reason why the chat interaction ended after this choice was presented to the user
	/// </summary>
	[JsonProperty("finish_reason")]
    public string? FinishReason { get; set; }

	/// <summary>
	///     Partial message "delta" from a stream. For example, the result from
	///     <see cref="ChatEndpoint.StreamChatEnumerable">StreamChatEnumerableAsync.</see>
	///     If this result object is not from a stream, this will be null
	/// </summary>
	[JsonProperty("delta")]
    public ChatMessage? Delta { get; set; }

	/// <summary>
	///     A convenience method to return the content of the message in this response
	/// </summary>
	/// <returns>The content of the message in this response, not including <see cref="ChatMessageRoles" />.</returns>
	public override string? ToString()
    {
        return Message?.Content;
    }
}

/// <summary>
///		Prompt token usage of different modalities and functionalities.
/// </summary>
public class ChatPromptTokenDetails
{
	/// <summary>
	///		How many tokens were cached and billed at a discount.
	/// </summary>
	[JsonProperty("cached_tokens")]
	public int? CachedTokens { get; set; }
	
	/// <summary>
	///		How many tokens were in the audio part of the prompt.
	/// </summary>
	[JsonProperty("audio_tokens")]
	public int? AudioTokens { get; set; }
	
	/// <summary>
	///		How many tokens were in the text part of the prompt.
	/// </summary>
	[JsonProperty("text_tokens")]
	public int? TextTokens { get; set; }
	
	/// <summary>
	///		How many tokens were in the image part of the prompt.
	/// </summary>
	[JsonProperty("image_tokens")]
	public int? ImageTokens { get; set; }
}

/// <summary>
///		Token usage for a specialized prompting strategy used.
/// </summary>
public class ChatUsageTokenDetails
{
	/// <summary>
	///		How many tokens were used for COT.
	/// </summary>
	[JsonProperty("reasoning_tokens")]
	public int? ReasoningTokens { get; set; }
	
	/// <summary>
	///		How many tokens were used for synthesizing audio.
	/// </summary>
	[JsonProperty("audio_tokens")]
	public int? AudioTokens { get; set; }
	
	/// <summary>
	///		How many text tokens were used.
	/// </summary>
	[JsonProperty("text_tokens")]
	public int? TextTokens { get; set; }
	
	/// <summary>
	///		How many text tokens were accepted from the predicted output.
	/// </summary>
	[JsonProperty("accepted_prediction_tokens")]
	public int? AcceptedPredictionTokens { get; set; }
	
	/// <summary>
	///		How many text tokens were rejected from the predicted output.
	/// </summary>
	[JsonProperty("rejected_prediction_tokens")]
	public int? RejectedPredictionTokens { get; set; }
}

/// <summary>
///     How many tokens were used in this chat message.
/// </summary>
public class ChatUsage : Usage
{
	/// <summary>
	///     Number of tokens in the generated completion.
	/// </summary>
	[JsonProperty("completion_tokens")]
    public int CompletionTokens { get; set; }

	/// <summary>
	///     Number of tokens in the prompt.
	/// </summary>
	[JsonProperty("prompt_tokens")]
    public int PromptTokens { get; set; }

	/// <summary>
	///     Total number of tokens used in the request (prompt + completion).
	/// </summary>
	[JsonProperty("total_tokens")]
    public int TotalTokens { get; set; }
	
	/// <summary>
	///     Details about prompt tokens calculation. Currently supported only by OpenAI.
	/// </summary>
	[JsonProperty("prompt_tokens_details")]
	public ChatPromptTokenDetails? PromptTokenDetails { get; set; }
	
	/// <summary>
	///     Number of tokens in the generated completion.
	/// </summary>
	[JsonProperty("completion_tokens_details")]
	public ChatUsageTokenDetails? CompletionTokensDetails { get; set; }
	
	/// <summary>
	///		Number of cached tokens.
	/// </summary>
	[JsonIgnore]
	public int? CacheCreationTokens { get; set; }
	
	/// <summary>
	///		Number of tokens read from cache.
	/// </summary>
	[JsonIgnore]
	public int? CacheReadTokens { get; set; }
	
	/// <summary>
	/// Native usage object returned by the vendor. Provided for Anthropic, Cohere, and Google.
	/// Use this to read details about the billed units for vendor-specific features.
	/// </summary>
	[JsonIgnore]
	public IChatUsage? VendorUsageObject { get; set; }
	
	/// <summary>
	/// Which provider returned the usage.
	/// </summary>
	[JsonIgnore]
	public LLmProviders Provider { get; set; }
	
	/// <summary>
	/// Creates a new empty chat usage associated with a provider.
	/// </summary>
	/// <param name="provider"></param>
	public ChatUsage(LLmProviders provider)
	{
		Provider = provider;
	}
	
	internal ChatUsage(VendorAnthropicUsage usage)
	{
		CompletionTokens = usage.OutputTokens;
		PromptTokens = usage.InputTokens;
		TotalTokens = CompletionTokens + PromptTokens;
		VendorUsageObject = usage;
		Provider = LLmProviders.Anthropic;
	}
	
	internal ChatUsage(VendorCohereUsage usage)
	{
		CompletionTokens = usage.BilledUnits.OutputTokens;
		PromptTokens = usage.BilledUnits.OutputTokens;
		TotalTokens = CompletionTokens + PromptTokens;
		VendorUsageObject = usage;
		Provider = LLmProviders.Cohere;
	}

	internal ChatUsage(VendorGoogleUsage usage)
	{
		CompletionTokens = usage.CandidatesTokenCount > 0 ? usage.CandidatesTokenCount : usage.PromptTokenCount - usage.CachedContentTokenCount;
		PromptTokens = usage.PromptTokenCount;
		TotalTokens = usage.TotalTokenCount;
		CacheReadTokens = usage.CachedContentTokenCount;
		VendorUsageObject = usage;
		Provider = LLmProviders.Google;
	}

	/// <summary>
	///	View of the usage. The returned string differs based on the provider.
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		return Provider switch
		{
			LLmProviders.Google => $"Total: {TotalTokens}, Prompt: {PromptTokens}, Completion: {CompletionTokens}, Cache read: {CacheReadTokens}",
			LLmProviders.Cohere => $"Total: {TotalTokens}, Prompt: {PromptTokens}, Completion: {CompletionTokens}",
			_ => $"Total: {TotalTokens}, Prompt: {PromptTokens}, Completion: {CompletionTokens}, Cache created: {CacheCreationTokens}, Cache read: {CacheReadTokens}"
		};
	}
}