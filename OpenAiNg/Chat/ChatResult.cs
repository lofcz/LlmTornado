using System.Collections.Generic;
using Newtonsoft.Json;
using OpenAiNg.Code;
using OpenAiNg.Vendor.Anthropic;

namespace OpenAiNg.Chat;

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
	///     A convenience method to return the content of the message in the first choice of this response
	/// </summary>
	/// <returns>The content of the message, not including <see cref="ChatMessageRole" />.</returns>
	public override string? ToString()
    {
        return Choices is { Count: > 0 } ? Choices[0].ToString() : null;
    }

	internal static ChatResult? Deserialize(LLmProviders provider, string jsonData)
	{
		return provider switch
		{
			LLmProviders.OpenAi => JsonConvert.DeserializeObject<ChatResult>(jsonData),
			LLmProviders.Anthropic => JsonConvert.DeserializeObject<VendorAnthropicChatResult>(jsonData)?.ToChatResult(),
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
	///     <see cref="ChatEndpoint.StreamChatEnumerableAsync(ChatRequest)">StreamChatEnumerableAsync.</see>
	///     If this result object is not from a stream, this will be null
	/// </summary>
	[JsonProperty("delta")]
    public ChatMessage? Delta { get; set; }

	/// <summary>
	///     A convenience method to return the content of the message in this response
	/// </summary>
	/// <returns>The content of the message in this response, not including <see cref="ChatMessageRole" />.</returns>
	public override string? ToString()
    {
        return Message?.Content;
    }
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

	public ChatUsage()
	{
		
	}
	
	internal ChatUsage(VendorAnthropicUsage usage)
	{
		CompletionTokens = usage.OutputTokens;
		PromptTokens = usage.InputTokens;
		TotalTokens = CompletionTokens + PromptTokens;
	}
}