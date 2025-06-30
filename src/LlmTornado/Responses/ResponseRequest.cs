using System.Collections.Generic;
using System.Threading;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.Responses;

/// <summary>
/// Request for responses API.
/// </summary>
public class ResponseRequest
{
    /// <summary>
    ///		Cancellation token to use with the request.
    /// </summary>
    [JsonIgnore]
    public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
    
    /// <summary>
    /// Whether to run the model response in the background.
    /// </summary>
    [JsonProperty("background")]
    public bool? Background { get; set; }

    /// <summary>
    /// Specify additional output data to include in the model response. 
    /// </summary>
    [JsonProperty("include")]
    public List<string>? Include { get; set; }

    /// <summary>
    /// Text input.
    /// </summary>
    [JsonIgnore]
    public string? InputString { get; set; }

    /// <summary>
    /// Text, image, or file inputs to the model, used to generate a response.
    /// </summary>
    [JsonIgnore]
    public List<object>? InputParts { get; set; }

    /// <summary>
    /// Text, image, or file inputs to the model, used to generate a response.
    /// </summary>
    [JsonProperty("input")]
    internal object? Input => InputString;

    /// <summary>
    /// A system (or developer) message inserted into the model's context. When using along with previous_response_id, the instructions from a previous response will not be carried over to the next response. This makes it simple to swap out system (or developer) messages in new responses.
    /// </summary>
    [JsonProperty("instructions")]
    public string? Instructions { get; set; }
    
    /// <summary>
    /// An upper bound for the number of tokens that can be generated for a response, including visible output tokens and reasoning tokens.
    /// </summary>
    [JsonProperty("max_output_tokens")]
    public int? MaxOutputTokens { get; set; }
    
    /// <summary>
    /// The maximum number of total calls to built-in tools that can be processed in a response. This maximum number applies across all built-in tool calls, not per individual tool. Any further attempts to call a tool by the model will be ignored.
    /// </summary>
    [JsonProperty("max_tool_calls")]
    public int? MaxToolCalls { get; set; }
    
    /// <summary>
    /// Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format, and querying for objects via API or the dashboard.<br/>
    /// Keys are strings with a maximum length of 64 characters. Values are strings with a maximum length of 512 characters.
    /// </summary>
    [JsonProperty("metadata")]
    public Dictionary<string, string>? Metadata { get; set; }
    
    /// <summary>
    /// Model ID used to generate the response, like gpt-4o or o3. OpenAI offers a wide range of models with different capabilities, performance characteristics, and price points. Refer to the model guide to browse and compare available models.
    /// </summary>
    [JsonProperty("model")]
    [JsonConverter(typeof(ChatModelJsonConverter))]
    public ChatModel? Model { get; set; }
    
    /// <summary>
    /// Whether to allow the model to run tool calls in parallel. Defaults to true if null.
    /// </summary>
    [JsonProperty("parallel_tool_calls")]
    public bool? ParallelToolCalls { get; set; }
    
    /// <summary>
    /// The unique ID of the previous response to the model. Use this to create multi-turn conversations.
    /// </summary>
    [JsonProperty("previous_response_id")]
    public string? PreviousResponseId { get; set; }
    
    /// <summary>
    /// Reference to a prompt template and its variables.
    /// </summary>
    [JsonProperty("prompt")]
    public object? Prompt { get; set; }
    
    /// <summary>
    /// Configuration options for reasoning models.
    /// </summary>
    [JsonProperty("reasoning")]
    public object? Reasoning { get; set; }
    
    /// <summary>
    /// Specifies the processing type used for serving the request.
    /// </summary>
    [JsonProperty("service_tier")]
    public string? ServiceTier { get; set; }
    
    /// <summary>
    /// Whether to store the generated model response for later retrieval via API. Defaults to true if null.
    /// </summary>
    [JsonProperty("store")]
    public bool? Store { get; set; }
    
    /// <summary>
    /// If set to true, the model response data will be streamed to the client as it is generated using server-sent events. See the Streaming section below for more information.
    /// </summary>
    [JsonProperty("stream")]
    public bool? Stream { get; set; }
    
    /// <summary>
    /// What sampling temperature to use, between 0 and 2. Higher values like 0.8 will make the output more random, while lower values like 0.2 will make it more focused and deterministic. We generally recommend altering this or top_p but not both.
    /// </summary>
    [JsonProperty("temperature")]
    public double? Temperature { get; set; }
    
    /// <summary>
    /// Configuration options for a text response from the model. Can be plain text or structured JSON data.
    /// </summary>
    [JsonProperty("text")]
    public object? Text { get; set; }
    
    /// <summary>
    ///     Represents an optional field when sending tools calling prompt.
    ///     This field determines which function to call.
    /// </summary>
    /// <remarks>
    ///     If this field is not specified, the default behavior ("auto") allows the model to automatically decide whether to
    ///     call tools or not.
    ///     Specify the name of the function to call in the "Name" attribute of the FunctionCall object.
    ///     If you do not want the model to call any function, pass "None" for the "Name" attribute.
    /// </remarks>
    [JsonProperty("tool_choice")]
    [JsonConverter(typeof(OutboundToolChoice.OutboundToolChoiceConverter))]
    public OutboundToolChoice? ToolChoice { get; set; }
    
    /// <summary>
    /// An array of tools the model may call while generating a response. You can specify which tool to use by setting the tool_choice parameter.
    /// </summary>
    [JsonProperty("tools")]
    public List<Tool>? Tools { get; set; }
    
    /// <summary>
    /// An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability.
    /// </summary>
    [JsonProperty("top_logprobs")]
    public int? TopLogprobs { get; set; }
    
    /// <summary>
    /// An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are considered. We generally recommend altering this or temperature but not both.
    /// </summary>
    [JsonProperty("top_p")]
    public double? TopP { get; set; }
    
    /// <summary>
    /// The truncation strategy to use for the model response.
    /// </summary>
    [JsonProperty("truncation")]
    public string? Truncation { get; set; }
    
    /// <summary>
    /// A stable identifier for your end-users. Used to boost cache hit rates by better bucketing similar requests and to help OpenAI detect and prevent abuse.
    /// </summary>
    [JsonProperty("user")]
    public string? User { get; set; }

    /// <summary>
    ///	Serializes the chat request into the request body, based on the conventions used by the LLM provider.
    /// </summary>
    public TornadoRequestContent Serialize(IEndpointProvider provider)
    {
        return Serialize(provider, false);
    }
    
    private TornadoRequestContent Serialize(IEndpointProvider provider, bool pretty)
    {
        string body = this.ToJson();
        return new TornadoRequestContent(body, Model, null, provider, CapabilityEndpoints.Responses);
    }
}