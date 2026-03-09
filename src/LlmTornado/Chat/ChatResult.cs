using System.Collections.Generic;
using System.Linq;
using System.Text;
using LlmTornado.Code;
using LlmTornado.Vendor.Anthropic;
using LlmTornado;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Chat.Vendors.Perplexity;
using LlmTornado.Responses;
using LlmTornado.Vendor.Google;
using Newtonsoft.Json;

namespace LlmTornado.Chat;

/// <summary>
///     Represents a result from calling the Chat API
/// </summary>
public class ChatResult : ApiResultBase
{
	internal ChatResult(ChatResult basedOn)
	{
		Id = basedOn.Id;
		Choices = basedOn.Choices;
		Usage = basedOn.Usage;
		ServiceTier = basedOn.ServiceTier;
		Speed = basedOn.Speed;
		SystemFingerprint = basedOn.SystemFingerprint;
		RawResponse = basedOn.RawResponse;
		VendorExtensions = basedOn.VendorExtensions;
		StreamInternalKind = basedOn.StreamInternalKind;
		InvocationResult = basedOn.InvocationResult;
	}

	public ChatResult()
	{
		
	}
	
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
	///     The usage statistics for the chat interaction.
	/// </summary>
	[JsonProperty("usage")]
    public ChatUsage? Usage { get; set; }

	/// <summary>
	///		The latency tier used to process this request.
	/// </summary>
	[JsonProperty("service_tier")]
	public ChatRequestServiceTiers? ServiceTier { get; set; }
	
	/// <summary>
	///     The speed tier used for the request. Returned by Anthropic when fast mode is used.
	/// </summary>
	[JsonIgnore]
	public ChatRequestSpeeds? Speed { get; set; }
	
	/// <summary>
	///     Fingerprint of the system used to resolve the request. Currently supported only by OpenAI.
	/// </summary>
	[JsonProperty("system_fingerprint")]
	public string? SystemFingerprint { get; set; }

	/// <summary>
	/// Raw response from the API.
	/// </summary>
	[JsonIgnore]
	public string? RawResponse { get; set; }
	
	/// <summary>
	/// A feedback on the prompt, if any. Currently supported only by Google.
	/// </summary>
	[JsonIgnore]
	public ChatResultPromptFeedback? PromptFeedback { get; set; }
	
	/// <summary>
	///		Features supported only by a specific/few providers with no shared equivalent.
	/// </summary>
	[JsonIgnore]
	public ChatResponseVendorExtensions? VendorExtensions { get; set; }

	[JsonIgnore]
	internal ChatResultStreamInternalKinds? StreamInternalKind { get; set; }
	
	[JsonIgnore]
	internal object? InvocationResult { get; set; }
	
	/// <summary>
	///     A convenience method to return the content of the message in the first choice of this response
	/// </summary>
	/// <returns>The content of the message, not including <see cref="ChatMessageRoles" />.</returns>
	public override string? ToString()
    {
        return Choices is { Count: > 0 } ? Choices[0].ToString() : null;
    }

	internal static ChatResult? Deserialize(LLmProviders provider, string jsonData, string? postData, object? requestObj)
	{
		return provider switch
		{
			LLmProviders.OpenAi => JsonConvert.DeserializeObject<ChatResult>(jsonData),
			LLmProviders.Anthropic => JsonConvert.DeserializeObject<VendorAnthropicChatResult>(jsonData)?.ToChatResult(postData, requestObj),
			LLmProviders.Cohere => JsonConvert.DeserializeObject<VendorCohereChatResult>(jsonData)?.ToChatResult(postData, requestObj),
			LLmProviders.Google => JsonConvert.DeserializeObject<VendorGoogleChatResult>(jsonData)?.ToChatResult(postData, requestObj),
			_ => JsonConvert.DeserializeObject<ChatResult>(jsonData)
		};
	}
}

/// <summary>
/// A feedback on the input prompt, if any.
/// </summary>
public class ChatResultPromptFeedback
{
	/// <summary>
	/// A reason why the prompt was blocked, if any.
	/// </summary>
	public ChatResultPromptFeedbackBlockReasons? BlockReason { get; set; }
	
	/// <summary>
	/// A list of safety ratings.
	/// </summary>
	public List<ChatResultPromptFeedbackSafetyRating>? SafetyRatings { get; set; }
	
	/// <summary>
	/// Native details.
	/// </summary>
	public object? NativeObject { get; set; }
}

/// <summary>
/// A safety rating in a category.
/// </summary>
public class ChatResultPromptFeedbackSafetyRating
{
	/// <summary>
	/// Whether the content was blocked in this category.
	/// </summary>
	public bool Blocked { get; set; }

	/// <summary>
	/// Probability of harm.
	/// </summary>
	public ChatResultPromptFeedbackSafetyRatingHarmProbability Probability { get; set; } = ChatResultPromptFeedbackSafetyRatingHarmProbability.Unknown;

	/// <summary>
	/// Harm category.
	/// </summary>
	public ChatResultPromptFeedbackSafetyRatingHarmCategory Category { get; set; } = ChatResultPromptFeedbackSafetyRatingHarmCategory.Unknown;
}

/// <summary>
/// Harm categories.
/// </summary>
public enum ChatResultPromptFeedbackSafetyRatingHarmCategory
{
	/// <summary>
	/// Category was not set.
	/// </summary>
	Unknown,
	
	/// <summary>
	/// Harassment content.
	/// </summary>
	Harassment,
	
	/// <summary>
	/// Hate speech and content.
	/// </summary>
	HateSpeech,
	
	/// <summary>
	/// Sexually explicit content.
	/// </summary>
	SexuallyExplicit,
	
	/// <summary>
	/// Dangerous content.
	/// </summary>
	Dangerous
}

/// <summary>
/// Probability of harm.
/// </summary>
public enum ChatResultPromptFeedbackSafetyRatingHarmProbability
{
	/// <summary>
	/// Probability was not set.
	/// </summary>
	Unknown,
	
	/// <summary>
	/// Content has a negligible chance of being unsafe.
	/// </summary>
	Negligible,
	
	/// <summary>
	/// Content has a low chance of being unsafe.
	/// </summary>
	Low,
	
	/// <summary>
	/// Content has a medium chance of being unsafe.
	/// </summary>
	Medium,
	
	/// <summary>
	/// Content has a high chance of being unsafe.
	/// </summary>
	High
}

/// <summary>
/// Reasons why prompt was blocked.
/// </summary>
public enum ChatResultPromptFeedbackBlockReasons
{
	/// <summary>
	/// Prompt was blocked due to safety reasons.
	/// </summary>
	Safety,
	
	/// <summary>
	/// rompt was blocked due to the terms which are included from the terminology blocklist.
	/// </summary>
	Blocklist,
	
	/// <summary>
	/// Prompt was blocked due to prohibited content.
	/// </summary>
	ProhibitedContent,
	
	/// <summary>
	/// Prompt was blocked due to unknown reasons.
	/// </summary>
	Other
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
	///     The reason why the chat interaction ended after this choice was presented to the user, RAW version.
	/// </summary>
	[JsonConverter(typeof(ChatMessageFinishReasonsConverter))]
	[JsonProperty("finish_reason")]
    public ChatMessageFinishReasons? FinishReason { get; set; }
	
	/// <summary>
	///     The stop string or token ID that caused the completion to stop, if applicable.
	///     This is different from <see cref="FinishReason"/> - it contains the actual stop sequence
	///     that triggered the stop, not the category of why it stopped.
	///     Returned by some providers like vLLM and Anthropic.
	/// </summary>
	[JsonProperty("stop_reason")]
    public string? StopReason { get; set; }
	
	/// <summary>
	///     Partial message "delta" from a stream. If this result object is not from a stream, this will be null.
	/// </summary>
	[JsonProperty("delta")]
    public ChatMessage? Delta { get; set; }
	
	/// <summary>
	///     A convenience method to return the content of the message in this response
	/// </summary>
	/// <returns>The content of the message in this response, not including <see cref="ChatMessageRoles" />.</returns>
	public override string? ToString()
	{
		return Message?.Parts?.Count > 0 ? string.Join("\n", Message.Parts.Select(x => x.ToString())) : Message?.Content;
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
	
	/// <summary>
	///		Number of tokens present in tool-use prompt(s). Reported only by Google.
	/// </summary>
	[JsonIgnore]
	public int? ToolsUseTokens { get; set; }
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
	/// Native usage object returned by the vendor. Provided for Anthropic, Cohere, Google, and Perplexity.
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
	
	internal ChatUsage(VendorPerplexityUsage usage)
	{
		CompletionTokens = usage.CompletionTokens;
		PromptTokens = usage.PromptTokens;
		TotalTokens = usage.TotalTokens;
		VendorUsageObject = usage;
		Provider = LLmProviders.Perplexity;
	}
	
	internal ChatUsage(VendorCohereUsage usage)
	{
		CompletionTokens = usage.BilledUnits?.OutputTokens ?? 0;
		PromptTokens = usage.BilledUnits?.OutputTokens ?? 0;
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
		CompletionTokensDetails = new ChatUsageTokenDetails
		{
			ReasoningTokens = usage.ThoughtsTokenCount,
			ToolsUseTokens = usage.ToolUsePromptTokenCount,
			AudioTokens = usage.CandidatesTokensDetails?.Where(x => x.Modality is "AUDIO").Sum(x => x.TokenCount)
		};
		Provider = LLmProviders.Google;
	}

	internal ChatUsage(ResponseUsage responseUsage)
	{
		CompletionTokens = responseUsage.OutputTokens;
		PromptTokens = responseUsage.InputTokens;
		TotalTokens = responseUsage.TotalTokens;
		CacheReadTokens = responseUsage.InputTokenDetails?.CachedTokens;
		CacheCreationTokens = 0;
		CacheReadTokens = 0;
		
		CompletionTokensDetails = new ChatUsageTokenDetails
		{
			ReasoningTokens = responseUsage.OutputTokenDetails?.ReasoningTokens ?? 0
		};
		
		PromptTokenDetails = new ChatPromptTokenDetails
		{
			TextTokens = responseUsage.InputTokenDetails?.TextTokens,
			CachedTokens = responseUsage.InputTokenDetails?.CachedTokens,
			AudioTokens = responseUsage.InputTokenDetails?.AudioTokens,
		};

		VendorUsageObject = responseUsage;
		Provider = LLmProviders.OpenAi;
	}

	/// <summary>
	///	View of the usage. The returned string differs based on the provider.
	/// </summary>
	/// <returns></returns>
	public override string ToString()
	{
		StringBuilder sb = new StringBuilder();
		sb.Append($"Total: {TotalTokens}, Prompt: {PromptTokens}, Completion: {CompletionTokens}");

		if (CacheCreationTokens > 0)
		{
			sb.Append($", Cache created: {CacheCreationTokens}");
		}

		if (CacheReadTokens > 0)
		{
			sb.Append($", Cache read: {CacheReadTokens}");
		}

		if (CompletionTokensDetails is not null)
		{
			if (CompletionTokensDetails.TextTokens > 0)
			{
				sb.Append($", Text tokens: {CompletionTokensDetails.TextTokens}");
			}
			
			if (CompletionTokensDetails.ReasoningTokens > 0)
			{
				sb.Append($", Reasoning tokens: {CompletionTokensDetails.ReasoningTokens}");
			}
			
			if (CompletionTokensDetails.AudioTokens > 0)
			{
				sb.Append($", Audio tokens: {CompletionTokensDetails.AudioTokens}");
			}
			
			if (CompletionTokensDetails.ToolsUseTokens > 0)
			{
				sb.Append($", Tools use tokens: {CompletionTokensDetails.ToolsUseTokens}");
			}
			
			if (CompletionTokensDetails.AcceptedPredictionTokens > 0)
			{
				sb.Append($", Accepted prediction tokens: {CompletionTokensDetails.AcceptedPredictionTokens}");
			}
			
			if (CompletionTokensDetails.RejectedPredictionTokens > 0)
			{
				sb.Append($", Rejected prediction tokens: {CompletionTokensDetails.RejectedPredictionTokens}");
			}
		}

		return sb.ToString();
	}
}