using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Chat.Vendors.Mistral;
using LlmTornado.Chat.Vendors.Perplexity;
using LlmTornado.Chat.Vendors.XAi;
using LlmTornado.Code.Models;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using LlmTornado.Chat.Vendors.Google;
using LlmTornado.Responses;

namespace LlmTornado.Chat;

/// <summary>
///     A request to the Chat API. This is similar, but not exactly the same as the
///     <see cref="Completions.CompletionRequest" />
///     Based on the <see href="https://platform.openai.com/docs/api-reference/chat">OpenAI API docs</see>
/// </summary>
public class ChatRequest : IModelRequest
{
	/// <summary>
	///     Creates a new, empty <see cref="ChatRequest" />
	/// </summary>
	public ChatRequest()
    {
	   
    }

	/// <summary>
	///     Create a new chat request enlisted in a conversation, using the data from the input chat request.
	/// </summary>
	/// <param name="conversation"></param>
	/// <param name="basedOn"></param>
	internal ChatRequest(Conversation conversation, ChatRequest? basedOn)
	{
		OwnerConversation = conversation;

		if (basedOn is not null)
		{
			CopyData(basedOn);
		}
	}

	/// <summary>
	///     Create a new chat request using the data from the input chat request.
	/// </summary>
	/// <param name="basedOn"></param>
	public ChatRequest(ChatRequest? basedOn)
    {
	    if (basedOn is null)
	    {
		    return;
	    }

	    CopyData(basedOn);
    }

	private void CopyData(ChatRequest basedOn)
	{
		Model = basedOn.Model;
		Messages = basedOn.Messages;
		Temperature = basedOn.Temperature;
		TopP = basedOn.TopP;
		NumChoicesPerMessage = basedOn.NumChoicesPerMessage;
		StopSequence = basedOn.StopSequence;
		MultipleStopSequences = basedOn.MultipleStopSequences;
		MaxTokens = basedOn.MaxTokens;
		FrequencyPenalty = basedOn.FrequencyPenalty;
		PresencePenalty = basedOn.PresencePenalty;
		LogitBias = basedOn.LogitBias;
		Tools = basedOn.Tools;
		ToolChoice = basedOn.ToolChoice;
		OutboundFunctionsContent = basedOn.OutboundFunctionsContent;
		Adapter = basedOn.Adapter;
		VendorExtensions = basedOn.VendorExtensions;
		StreamOptions = basedOn.StreamOptions;
		TrimResponseStart = basedOn.TrimResponseStart;
		ParallelToolCalls = basedOn.ParallelToolCalls;
		Seed = basedOn.Seed;
		User = basedOn.User;
		ResponseFormat = basedOn.ResponseFormat;
		Audio = basedOn.Audio;
		Modalities = basedOn.Modalities;
		Metadata = basedOn.Metadata;
		Store = basedOn.Store;
		ReasoningEffort = basedOn.ReasoningEffort;
		Prediction = basedOn.Prediction;
		ServiceTier = basedOn.ServiceTier;
		Stream = basedOn.Stream;
		ReasoningBudget = basedOn.ReasoningBudget;
		Logprobs = basedOn.Logprobs;
		TopLogprobs = basedOn.TopLogprobs;
		WebSearchOptions = basedOn.WebSearchOptions;
		OnSerialize = basedOn.OnSerialize;
		ResponseRequestParameters = basedOn.ResponseRequestParameters;
		UseResponseEndpoint = basedOn.UseResponseEndpoint;
		ReasoningFormat = basedOn.ReasoningFormat;
	}

	/// <summary>
	///     The model to use for this request
	/// </summary>
	[JsonProperty("model")]
	[JsonConverter(typeof(ChatModelJsonConverter))]
	public ChatModel? Model { get; set; } = ChatModel.OpenAi.Gpt35.Turbo;

	[JsonIgnore]
	IModel? IModelRequest.RequestModel => Model;

	/// <summary>
	///		Modalities of the model. Can be omitted for text only conversations.
	///		For audio, OpenAI requires both: <see cref="ChatModelModalities.Text"/> and <see cref="ChatModelModalities.Audio"/>, using only <see cref="ChatModelModalities.Audio"/> is invalid.
	/// </summary>
	[JsonProperty("modalities")]
	[JsonConverter(typeof(ModalitiesJsonConverter))]
	public List<ChatModelModalities>? Modalities { get; set; }
	
	/// <summary>
	///		Parameters for audio output. Required when audio output is requested with <see cref="Modalities"/>: ["<see cref="ChatModelModalities.Audio"/>"].
	///		Currently only works with OpenAI models.
	/// </summary>
	[JsonProperty("audio")]
	public ChatRequestAudio? Audio { get; set; }
	
	/// <summary>
	/// Set of 16 key-value pairs that can be attached to an object. This can be useful for storing additional information about the object in a structured format, and querying for objects via API or the dashboard.<br/>
	/// Keys are strings with a maximum length of 64 characters. Values are strings with a maximum length of 512 characters.
	/// </summary>
	[JsonProperty("metadata")]
	public Dictionary<string, string>? Metadata { get; set; }
	
	/// <summary>
	///		Whether to store the output of this chat completion request for use in our model distillation or evals products.
	///		Currently only works with OpenAI models.
	/// </summary>
	[JsonProperty("store")]
	public bool? Store { get; set; }

	/// <summary>
	///     The messages to send with this Chat Request
	/// </summary>
	[JsonProperty("messages")]
    [JsonConverter(typeof(ChatMessageRequestMessagesJsonConverter))]
    public List<ChatMessage>? Messages { get; set; }

	/// <summary>
	///     What sampling temperature to use. Higher values means the model will take more risks. Try 0.9 for more creative
	///     applications, and 0 (argmax sampling) for ones with a well-defined answer. It is generally recommend to use this or
	///     <see cref="TopP" /> but not both.
	/// </summary>
	[JsonProperty("temperature")]
    public double? Temperature { get; set; }

	/// <summary>
	/// Allows transforming the request on JSON level, before it is serialized into a string.
	/// </summary>
	[JsonIgnore]
	public Action<JObject, ChatRequest>? OnSerialize { get; set; }
	
	/// <summary>
	/// If set, the request may be promoted from <see cref="CapabilityEndpoints.Chat"/> to <see cref="CapabilityEndpoints.Responses"/>, this is currently supported only by OpenAI.<br/>
	/// </summary>
	[JsonIgnore]
	public ResponseRequest? ResponseRequestParameters { get; set; }
	
	/// <summary>
	///     An alternative to sampling with temperature, called nucleus sampling, where the model considers the results of the
	///     tokens with top_p probability mass. So 0.1 means only the tokens comprising the top 10% probability mass are
	///     considered. It is generally recommend to use this or <see cref="Temperature" /> but not both.
	/// </summary>
	[JsonProperty("top_p")]
    public double? TopP { get; set; }

	/// <summary>
	///     How many different choices to request for each message. Defaults to 1.
	/// </summary>
	[JsonProperty("n")]
    public int? NumChoicesPerMessage { get; set; }
	
	/// <summary>
	///     Balance option between response time and cost/latency. Currently supported only by O1, O1 Mini, Grok 3 series, Sonar Deep Research, and Qwen.
	/// </summary>
	[JsonProperty("reasoning_effort")]
	public ChatReasoningEfforts? ReasoningEffort { get; set; }
	
	/// <summary>
	///     Format of the reasoning. Currently supported only by Grok/Qwen.
	/// </summary>
	[JsonProperty("reasoning_format")]
	public ChatReasoningFormats? ReasoningFormat { get; set; }
	
	/// <summary>
	///		Sets a token limit on reasoning. 0 disables reasoning. Currently supported by Google (natively "thinkingBudget") and Anthropic (natively "budget_tokens").<br/>
	///		Note: Some providers (Google) don't guarantee this limit is honored without under/over-flowing.<br/>
	///		Google: 2.5 pro: 128-32768; 2.5 flash: 0-24576; 2.5 flash lite: 512-24576; dynamic thinking for any model: -1;<br/>
	///		Anthropic: 0,1024+
	/// </summary>
	[JsonIgnore]
	public int? ReasoningBudget { get; set; }
	
	/// <summary>
	/// Configuration for a Predicted Output, which can greatly improve response times when large parts of the model response are known ahead of time. This is most common when you are regenerating a file with only minor changes to most of the content.
	/// </summary>
	[JsonProperty("prediction")]
	public ChatRequestPrediction? Prediction { get; set; }
	
	/// <summary>
	///     The seed to use for deterministic requests.
	/// </summary>
	[JsonProperty("seed")]
    public int? Seed { get; set; }
	
	/// <summary>
	///     Specifies the latency tier to use for processing the request. This parameter is relevant for customers subscribed to the OpenAI scale tier service.
	/// </summary>
	[JsonProperty("service_tier")]
	public ChatRequestServiceTiers? ServiceTier { get; set; }

	/// <summary>
	///     The response format to use. If <see cref="ChatRequestResponseFormats.Json" />, either system or user message in the
	///     conversation must contain "JSON".
	/// </summary>
	[JsonProperty("response_format")]
    public ChatRequestResponseFormats? ResponseFormat { get; set; }

	/// <summary>
	///     Specifies the response should be streamed. When using abstractions, such as <see cref="Conversation"/>, this is set automatically by the library.
	/// </summary>
	[JsonProperty("stream")]
    public bool? Stream { get; set; }

	[JsonIgnore]
	internal bool StreamResolved => Stream ?? false;
	
	/// <summary>
	/// Whether to return log probabilities of the output tokens or not. If true, returns the log probabilities of each output token returned in the content of message.
	/// </summary>
	[JsonProperty("logprobs")]
	public bool? Logprobs { get; set; }
	
	/// <summary>
	/// An integer between 0 and 20 specifying the number of most likely tokens to return at each token position, each with an associated log probability. logprobs must be set to true if this parameter is used.
	/// </summary>
	[JsonProperty("top_logprobs")]
	public int? TopLogprobs { get; set; }
	
	/// <summary>
	/// This tool searches the web for relevant results to use in a response. Learn more about the web search tool.
	/// </summary>
	[JsonProperty("web_search_options")]
	public ChatRequestWebSearchOptions? WebSearchOptions { get; set; }
	
	/// <summary>
	///     The stream configuration.<br/>
	///		Note: by default Tornado includes usage for all providers.
	/// </summary>
	[JsonIgnore]
	public ChatStreamOptions? StreamOptions
	{
		get => StreamOptionsInternal;
		set
		{
			StreamOptionsInternal = value;
			StreamOptionsInternalSerialized = StreamOptionsInternal;
		}
	}

	[JsonIgnore]
	internal ChatStreamOptions? StreamOptionsInternal { get; set; }
	
	[JsonProperty("stream_options")]
	internal object? StreamOptionsInternalSerialized { get; set; }
	
	/// <summary>
	///     This is only used for serializing the request into JSON, do not use it directly.
	/// </summary>
	[JsonProperty("stop")]
    internal object? CompiledStop
    {
        get
        {
            return MultipleStopSequences?.Length switch
            {
                1 => StopSequence,
                > 0 => MultipleStopSequences,
                _ => null
            };
        }
    }
	
	/// <summary>
	///     One or more sequences where the API will stop generating further tokens. The returned text will not contain the
	///     stop sequence.
	/// </summary>
	[JsonIgnore]
    public string[]? MultipleStopSequences { get; set; }

	/// <summary>
	///     The stop sequence where the API will stop generating further tokens. The returned text will not contain the stop
	///     sequence.  For convenience, if you are only requesting a single stop sequence, set it here
	/// </summary>
	[JsonIgnore]
    public string? StopSequence
    {
        get => MultipleStopSequences?.FirstOrDefault() ?? null;
        set
        {
            if (value != null)
                MultipleStopSequences = [value];
        }
    }

	/// <summary>
	///     How many tokens to complete to. Can return fewer if a stop sequence is hit.
	///		Note Anthropic: Streaming is required when max_tokens is greater than 21,333 for Claude 3.7+ models.
	/// </summary>
	[JsonProperty("max_tokens")]
    public int? MaxTokens { get; set; }

	/// <summary>
	///		Strategy for serializing <see cref="MaxTokens"/>.
	/// </summary>
	[JsonIgnore] 
	public ChatRequestMaxTokensSerializers MaxTokensSerializer { get; set; } = ChatRequestMaxTokensSerializers.Auto;

	/// <summary>
	///     The scale of the penalty for how often a token is used.  Should generally be between 0 and 1, although negative
	///     numbers are allowed to encourage token reuse.  Defaults to 0.
	/// </summary>
	[JsonProperty("frequency_penalty")]
    public double? FrequencyPenalty { get; set; }

	/// <summary>
	///     The scale of the penalty applied if a token is already present at all.  Should generally be between 0 and 1,
	///     although negative numbers are allowed to encourage token reuse.  Defaults to 0.
	/// </summary>
	[JsonProperty("presence_penalty")]
    public double? PresencePenalty { get; set; }

	/// <summary>
	///     Modify the likelihood of specified tokens appearing in the completion.
	///     Accepts a json object that maps tokens(specified by their token ID in the tokenizer) to an associated bias value
	///     from -100 to 100.
	///     Mathematically, the bias is added to the logits generated by the model prior to sampling.
	///     The exact effect will vary per model, but values between -1 and 1 should decrease or increase likelihood of
	///     selection; values like -100 or 100 should result in a ban or exclusive selection of the relevant token.
	/// </summary>
	[JsonProperty("logit_bias")]
    public IReadOnlyDictionary<string, float>? LogitBias { get; set; }

	/// <summary>
	///     A unique identifier representing your end-user, which can help OpenAI to monitor and detect abuse.
	/// </summary>
	[JsonProperty("user")]
    public string? User { get; set; }

	/// <summary>
	///     A list of tools the model may generate JSON inputs for.
	/// </summary>
	[JsonProperty("tools")]
    public List<Tool>? Tools { get; set; }

	/// <summary>
	///     Parallel function calling can be disabled / enabled for vendors supporting the feature.
	///		As of 6/24, the only vendor supporting the feature is OpenAI.
	/// </summary>
	[JsonProperty("parallel_tool_calls")]
	public bool? ParallelToolCalls { get; set; }

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
	///     If set the functions part of the outbound request encoded as JSON are stored here.
	///     This can be used a cheap heuristic for counting tokens used when streaming.
	///     Note that OpenAI silently transforms the provided JSON-schema into TypeScript and hence the real usage will be
	///     somewhat lower.
	/// </summary>
	[JsonIgnore]
    public Ref<string>? OutboundFunctionsContent { get; internal set; }

	/// <summary>
	///     This can be any API provider specific data. Currently used in KoboldCpp.
	/// </summary>
	[JsonProperty("adapter")]
    public Dictionary<string, object?>? Adapter { get; set; }
	
	/// <summary>
	///		Features supported only by a single/few providers with no shared equivalent.
	/// </summary>
	[JsonIgnore]
	public ChatRequestVendorExtensions? VendorExtensions { get; set; }

	/// <summary>
	///		Trims the leading whitespace and newline characters in the response. Unless you need to work with responses
	///		with leading whitespace it is recommended to keep this switch on. When streaming, some providers/models incorrectly
	///		produce leading whitespace if the text part of the streamed response is preceded with tool blocks.
	/// </summary>
	[JsonIgnore]
	public bool TrimResponseStart { get; set; } = true;

	/// <summary>
	///		Cancellation token to use with the request.
	/// </summary>
	[JsonIgnore]
	public CancellationToken CancellationToken { get; set; } = CancellationToken.None;
	
	[JsonIgnore]
	internal string? UrlOverride { get; set; }
	
	[JsonIgnore]
	internal Conversation? OwnerConversation { get; set; }
	
	/// <summary>
	/// If enabled the requested will be routed via <see cref="ResponsesEndpoint"/>. If null, the request will be routed this way, if the given model supports only the responses endpoint, or a compatible model is used and <see cref="ResponseRequestParameters"/> is not null.
	/// </summary>
	[JsonIgnore]
	public bool? UseResponseEndpoint { get; set; }
	
	internal void OverrideUrl(string url)
	{
		UrlOverride = url;
	}
	
	private static readonly PropertyRenameAndIgnoreSerializerContractResolver MaxTokensRenamer = new PropertyRenameAndIgnoreSerializerContractResolver();
	private static readonly JsonSerializerSettings MaxTokensRenamerSettings = new JsonSerializerSettings
	{
		ContractResolver = MaxTokensRenamer,
		NullValueHandling = NullValueHandling.Ignore
	};
	
	static ChatRequest()
	{
		MaxTokensRenamer.RenameProperty(typeof(ChatRequest), "max_tokens", "max_completion_tokens");	
	}

	private static JsonSerializerSettings GetSerializer(JsonSerializerSettings def, JsonSerializerSettings? input)
	{
		if (input is null)
		{
			return def;
		}

		JsonSerializerSettings newSettings = def.DeepCopy();
		newSettings.Formatting = input.Formatting;
		return newSettings;
	}

	private static string PreparePayload(object sourceObject, ChatRequest context, IEndpointProvider provider, CapabilityEndpoints endpoint, JsonSerializerSettings? settings)
	{
		JsonSerializer serializer = JsonSerializer.CreateDefault(settings);
		JObject jsonPayload = JObject.FromObject(sourceObject, serializer);
		context.OnSerialize?.Invoke(jsonPayload, context);
		provider.RequestSerializer?.Invoke(jsonPayload, new RequestSerializerContext(sourceObject, provider, RequestActionTypes.ChatCompletionCreate));
		return jsonPayload.ToString(settings?.Formatting ?? Formatting.None);
	}
	
	private static string PreparePayload(JObject sourceObject, ChatRequest context, IEndpointProvider provider, CapabilityEndpoints endpoint, JsonSerializerSettings? settings)
	{
		context.OnSerialize?.Invoke(sourceObject, context);
		provider.RequestSerializer?.Invoke(sourceObject, new RequestSerializerContext(sourceObject, provider, RequestActionTypes.ChatCompletionCreate));
		return sourceObject.ToString(settings?.Formatting ?? Formatting.None);
	}

	private static readonly Dictionary<LLmProviders, Func<ChatRequest, IEndpointProvider, CapabilityEndpoints, JsonSerializerSettings?, string>> SerializeMap = new Dictionary<LLmProviders, Func<ChatRequest, IEndpointProvider, CapabilityEndpoints, JsonSerializerSettings?, string>>((int)LLmProviders.Length)
	{
		{
			LLmProviders.OpenAi, (x, y, z, a) =>
			{
				if (x.Model is not null)
				{
					if (ChatModelOpenAi.TempIncompatibleModels.Contains(x.Model))
					{
						x.Temperature = null;
					}
				}

				JsonSerializerSettings settings = (x.MaxTokensSerializer, x.Model) switch
				{
					(ChatRequestMaxTokensSerializers.Auto, not null) when ChatModelOpenAi.ReasoningModelsAll.Contains(x.Model) => GetSerializer(MaxTokensRenamerSettings, a),
					(ChatRequestMaxTokensSerializers.MaxCompletionTokens, _) => GetSerializer(MaxTokensRenamerSettings, a),
					_ => GetSerializer(EndpointBase.NullSettings, a)
				};

				object obj = z is CapabilityEndpoints.Chat ? x : ResponseHelpers.ToResponseRequest(y, x.ResponseRequestParameters, x);
				return PreparePayload(obj, x, y, z, settings);
			}
		},
		{ LLmProviders.DeepSeek, (x, y, z, a) => PreparePayload(x, x, y, z, GetSerializer(EndpointBase.NullSettings, a)) },
		{ LLmProviders.Anthropic, (x, y, z, a) => PreparePayload(new VendorAnthropicChatRequest(x, y), x, y, z, GetSerializer(EndpointBase.NullSettings, a)) },
		{ LLmProviders.Cohere, (x, y, z, a) => PreparePayload(new VendorCohereChatRequest(x, y), x, y, z, GetSerializer(EndpointBase.NullSettings, a)) },
		{ LLmProviders.Google, (x, y, z, a) => PreparePayload(new VendorGoogleChatRequest(x, y), x, y, z, GetSerializer(EndpointBase.NullSettings, a)) },
		{
			LLmProviders.Mistral, (x, y, z, a) =>
			{
				VendorMistralChatRequest request = new VendorMistralChatRequest(x, y);
				JsonSerializerSettings serializer = GetSerializer(EndpointBase.NullSettings, a);
				return PreparePayload(request.Serialize(serializer), x, y, z, serializer);
			}
		},
		{
			LLmProviders.Groq, (x, y, z, a) =>
			{
				// fields unsupported by groq
				x.LogitBias = null;
				return PreparePayload(x, x, y, z, GetSerializer(EndpointBase.NullSettings, a));
			}
		},
		{
			LLmProviders.XAi, (x, y, z, a) =>
			{
				VendorXAiChatRequest request = new VendorXAiChatRequest(x, y);
				JsonSerializerSettings serializer = GetSerializer(EndpointBase.NullSettings, a);
				return PreparePayload(request.Serialize(serializer), x, y, z, serializer);
			}
		},
		{
			LLmProviders.Perplexity, (x, y, z, a) =>
			{
				VendorPerplexityChatRequest request = new VendorPerplexityChatRequest(x, y);
				JsonSerializerSettings serializer = GetSerializer(EndpointBase.NullSettings, a);
				return PreparePayload(request.Serialize(serializer), x, y, z, serializer);
			}
		},
		{
			LLmProviders.DeepInfra, (x, y, z, a) =>
			{
				return PreparePayload(x, x, y, z, GetSerializer(EndpointBase.NullSettings, a));
			}
		},
		{
			LLmProviders.OpenRouter, (x, y, z, a) =>
			{
				return PreparePayload(x, x, y, z, GetSerializer(EndpointBase.NullSettings, a));
			}
		}
	};

	internal CapabilityEndpoints GetCapabilityEndpoint()
	{
		return GetCapabilityEndpoint(this);
	}
	
	internal static CapabilityEndpoints GetCapabilityEndpoint(ChatRequest req)
	{
		// if we are explicitly told to use responses, honor the setting
		if (req.UseResponseEndpoint is true)
		{
			return CapabilityEndpoints.Responses;
		}
		
		// if we are explicitly told to not use responses, or the model is from an unsupported provider
		if (req.UseResponseEndpoint is false || req.Model?.Provider is not LLmProviders.OpenAi)
		{
			return CapabilityEndpoints.Chat;
		}
		
		// if we are missing metadata, use responses if we have parameters for it
		if (req.Model.EndpointCapabilities is null || req.Model.EndpointCapabilities.Count is 0)
		{
			return req.ResponseRequestParameters is not null ? CapabilityEndpoints.Responses : CapabilityEndpoints.Chat;
		}

		// automatically upcast in the case of /chat endpoint not being supported by the model
		if (req.Model.EndpointCapabilities.Contains(ChatModelEndpointCapabilities.Responses) && !req.Model.EndpointCapabilities.Contains(ChatModelEndpointCapabilities.Chat))
		{
			return CapabilityEndpoints.Responses;
		}

		return req.ResponseRequestParameters is not null ? CapabilityEndpoints.Responses : CapabilityEndpoints.Chat;
	}

	/// <summary>
	/// Serializes the request with debugging options
	/// </summary>
	/// <param name="provider"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	public TornadoRequestContent Serialize(IEndpointProvider provider, ChatRequestSerializeOptions? options)
	{
		CapabilityEndpoints capabilityEndpoint = GetCapabilityEndpoint(this);
		TornadoRequestContent serialized = Serialize(provider, capabilityEndpoint, options?.Pretty ?? false);
		
		string finalUrl = EndpointBase.BuildRequestUrl(serialized.Url, provider, capabilityEndpoint, Model);
		serialized.Url = finalUrl;

		if (options?.IncludeHeaders ?? false)
		{
			using HttpRequestMessage msg = provider.OutboundMessage(finalUrl, HttpMethod.Post, serialized.Body, options.Stream);
			serialized.Headers = msg.Headers.ConvertHeaders();
		}

		return serialized;
	}

	internal void Preserialize(IEndpointProvider provider)
	{
		if (OwnerConversation is not null)
		{
			Messages = OwnerConversation.Messages.ToList();
		}
		
		if (Messages is not null)
		{
			foreach (ChatMessage msg in Messages)
			{
				msg.Request = this;
			}	
		}
		
		if (Tools is not null)
		{
			foreach (Tool tool in Tools)
			{
				tool.Serialize(provider);
			}	
		}

		if (ResponseFormat is { Type: ChatRequestResponseFormatTypes.StructuredJson, Schema.Delegate: not null })
		{
			ResponseFormat.Serialize(provider);
		}
	}

	private TornadoRequestContent Serialize(IEndpointProvider provider, CapabilityEndpoints capabilityEndpoint, bool pretty)
	{
		Preserialize(provider);

		ChatRequest outboundCopy = new ChatRequest(this);

		switch (StreamResolved)
		{
			case false when StreamOptions is not null:
			{
				outboundCopy.StreamOptions = null;
				break;
			}
			case true when StreamOptions is null:
			{
				outboundCopy.StreamOptions = ChatStreamOptions.KnownOptionsIncludeUsage;
				break;
			}
		}

		if (provider.Provider is not LLmProviders.Groq)
		{
			outboundCopy.ReasoningFormat = null;
		}
		
		TornadoRequestContent serialized = SerializeMap.TryGetValue(provider.Provider, out Func<ChatRequest, IEndpointProvider, CapabilityEndpoints, JsonSerializerSettings?, string>? serializerFn) ? new TornadoRequestContent(serializerFn.Invoke(outboundCopy, provider, capabilityEndpoint, pretty ? new JsonSerializerSettings
		{
			Formatting = Formatting.Indented
		} : null), Model, UrlOverride, provider, capabilityEndpoint) : new TornadoRequestContent(string.Empty, Model, UrlOverride, provider, CapabilityEndpoints.Chat);
		
		return serialized;
	}

	///  <summary>
	/// 		Serializes the chat request into the request body, based on the conventions used by the LLM provider.
	///  </summary>
	///  <param name="provider"></param>
	///  <param name="capabilityEndpoint"></param>
	///  <returns></returns>
	public TornadoRequestContent Serialize(IEndpointProvider provider)
	{
		return Serialize(provider, GetCapabilityEndpoint(this), false);
	}
	
	internal class ModalitiesJsonConverter : JsonConverter<List<ChatModelModalities>>
	{
		public override void WriteJson(JsonWriter writer, List<ChatModelModalities>? value, JsonSerializer serializer)
		{
			if (value is null)
			{
				return;
			}

			if (value.Count is 0)
			{
				return;
			}
			
			writer.WriteStartArray();

			foreach (ChatModelModalities x in value)
			{
				switch (x)
				{
					case ChatModelModalities.Audio:
						writer.WriteValue("audio");
						break;
					case ChatModelModalities.Text:
						writer.WriteValue("text");
						break;
				}
			}

			writer.WriteEndArray();
		}

		public override List<ChatModelModalities>? ReadJson(JsonReader reader, Type objectType, List<ChatModelModalities>? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return existingValue;
		}
	}
    
    internal class ChatMessageRequestMessagesJsonConverter : JsonConverter<IList<ChatMessage>?>
    {
        public override void WriteJson(JsonWriter writer, IList<ChatMessage>? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
                return;
            }

            ChatRequest? request = null;
            bool isExpired = false;

            if (value.Count > 0)
            {
	            request = value[0].Request;
            }

            writer.WriteStartArray();

            foreach (ChatMessage msg in value)
            {
                writer.WriteStartObject();
                
                if (msg.Role is not null)
                {
	                writer.WritePropertyName("role");
	                
	                if (msg.Role is ChatMessageRoles.System)
	                {
		                if (request?.Model is not null && ChatModelOpenAiGpt4.ReasoningModels.Contains(request.Model))
		                {
			                writer.WriteValue("developer");
		                }
		                else
		                {
			                writer.WriteValue("system");
		                }
	                }
	                else
	                {
		                writer.WriteValue(ChatMessageRolesCls.MemberRolesDictInverse[msg.Role.Value]);   
	                }   
                }

                if (msg.Role is not null)
                {
	                switch (msg.Role)
	                {
		                case ChatMessageRoles.Tool:
		                {
			                writer.WritePropertyName("tool_call_id");
			                writer.WriteValue(msg.ToolCallId);
			                break;
		                }
		                case ChatMessageRoles.Assistant:
		                {
			                if (msg.Prefix is not null)
			                {
				                writer.WritePropertyName("prefix");
				                writer.WriteValue(msg.Prefix.Value);
			                }
			                
			                if (msg.ToolCalls is not null)
			                {
				                writer.WritePropertyName("tool_calls");

				                writer.WriteStartArray();

				                foreach (ToolCall call in msg.ToolCalls)
				                {
					                writer.WriteStartObject();

					                writer.WritePropertyName("id");
					                writer.WriteValue(call.Id);

					                writer.WritePropertyName("type");
					                writer.WriteValue(call.Type);

					                writer.WritePropertyName("function");
					                writer.WriteStartObject();

					                writer.WritePropertyName("name");
					                writer.WriteValue(call.FunctionCall.Name);

					                writer.WritePropertyName("arguments");
					                writer.WriteValue(call.FunctionCall.Arguments);

					                writer.WriteEndObject();

					                writer.WriteEndObject();
				                }

				                writer.WriteEndArray();
			                }

			                if (msg.Audio is not null)
			                {
				                if (request?.Audio?.CompressionStrategy is ChatAudioCompressionStrategies.Native or ChatAudioCompressionStrategies.PreferNative)
				                {
					                if (request.Audio.CompressionStrategy is ChatAudioCompressionStrategies.PreferNative)
					                {
						                isExpired = DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= msg.Audio.ExpiresAt;
					                }

					                if (!isExpired)
					                {
						                writer.WritePropertyName("audio");
						                writer.WriteStartObject();
				                
						                writer.WritePropertyName("id");
						                writer.WriteValue(msg.Audio.Id);
								
						                writer.WriteEndObject();    
					                }
				                }
			                }

			                break;
		                }
	                }
                }

                if (!string.IsNullOrWhiteSpace(msg.Name))
                {
                    writer.WritePropertyName("name");
                    writer.WriteValue(msg.Name);
                }

                if (msg is { Role: ChatMessageRoles.Tool, Content: null })
                {
	                goto closeMsgObj;
                }

                if (msg is { Role: ChatMessageRoles.Assistant, Content: null, ToolCalls: not null })
                {
	                goto closeMsgObj;
                }
                
                writer.WritePropertyName("content");

                if (msg.Parts?.Count > 0)
                {
                    writer.WriteStartArray();

                    foreach (ChatMessagePart part in msg.Parts)
                    {
                        writer.WriteStartObject();

                        writer.WritePropertyName("type");

                        string type = part.Type switch
                        {
	                        ChatMessageTypes.Text => "text",
	                        ChatMessageTypes.Image => "image_url",
	                        ChatMessageTypes.Audio => part.Audio?.Url is not null ? "audio_url" : "input_audio",
                            ChatMessageTypes.Video => "video_url",
                            _ => "text"
                        };
                        
	                    writer.WriteValue(type);   
	                    
                        switch (part.Type)
                        {
	                        case ChatMessageTypes.Text:
	                        {
		                        writer.WritePropertyName("text");
		                        writer.WriteValue(part.Text);
		                        break;
	                        }
	                        case ChatMessageTypes.Image:
	                        {
		                        writer.WritePropertyName("image_url");
		                        writer.WriteStartObject();

		                        writer.WritePropertyName("url");
		                        writer.WriteValue(part.Image?.Url);

		                        writer.WriteEndObject();
		                        break;
	                        }
	                        case ChatMessageTypes.Audio:
	                        {
								if (part.Audio?.Url is not null)
								{
                                    writer.WritePropertyName("audio_url");
                                    writer.WriteStartObject();

                                    writer.WritePropertyName("url");
                                    writer.WriteValue(part.Audio.Url);

                                    writer.WriteEndObject();
                                    break;
                                }

								writer.WritePropertyName("input_audio");
								writer.WriteStartObject();

								writer.WritePropertyName("data");
								writer.WriteValue(part.Audio?.Data);

								writer.WritePropertyName("format");

								if (part.Audio is not null)
								{
									switch (part.Audio.Format)
									{
										case ChatAudioFormats.Wav:
										{
											writer.WriteValue("wav");
											break;
										}
										case ChatAudioFormats.Mp3:
										{
											writer.WriteValue("mp3");
											break;
										}
									}
								}
								else
								{
									writer.WriteValue(string.Empty);
								}

								writer.WriteEndObject();
								break;
	                        }
                            case ChatMessageTypes.Video:
                            {
                                writer.WritePropertyName("video_url");
                                writer.WriteStartObject();

                                writer.WritePropertyName("url");
                                writer.WriteValue(part.Video?.Url);

                                writer.WriteEndObject();
                                break;
                            }
                        }

                        writer.WriteEndObject();
                    }

                    writer.WriteEndArray();
                }
                else
                {
	                if (msg.Audio is not null)
	                {
		                if (request?.Audio?.CompressionStrategy is ChatAudioCompressionStrategies.OutputAsText or ChatAudioCompressionStrategies.PreferNative)
		                {
			                // only write transcription if the audio cache expired
			                if (request.Audio.CompressionStrategy is ChatAudioCompressionStrategies.PreferNative && !isExpired)
			                {
				                msg.Content = msg.Audio.Id;
				                goto writeNativeContent;
			                }
			                
			                writer.WriteValue(msg.Audio.Transcript);
			                goto closeMsgObj;
		                }
	                }
	                
	                writeNativeContent:
	                writer.WriteValue(msg.Content);
                }

                closeMsgObj:
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        public override IList<ChatMessage>? ReadJson(JsonReader reader, Type objectType, IList<ChatMessage>? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return existingValue;
        }
    }
}