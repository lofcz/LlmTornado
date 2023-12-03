using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using OpenAiNg.ChatFunctions;
using OpenAiNg.Code;
using OpenAiNg.Completions;

namespace OpenAiNg.Chat;

/// <summary>
///     A request to the Chat API. This is similar, but not exactly the same as the
///     <see cref="Completions.CompletionRequest" />
///     Based on the <see href="https://platform.openai.com/docs/api-reference/chat">OpenAI API docs</see>
/// </summary>
public class ChatRequest
{
	/// <summary>
	///     Creates a new, empty <see cref="ChatRequest" />
	/// </summary>
	public ChatRequest()
    {
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

        Model = basedOn.Model;
        Messages = basedOn.Messages;
        Temperature = basedOn.Temperature;
        TopP = basedOn.TopP;
        NumChoicesPerMessage = basedOn.NumChoicesPerMessage;
        MultipleStopSequences = basedOn.MultipleStopSequences;
        MaxTokens = basedOn.MaxTokens;
        FrequencyPenalty = basedOn.FrequencyPenalty;
        PresencePenalty = basedOn.PresencePenalty;
        LogitBias = basedOn.LogitBias;
        Tools = basedOn.Tools;
        ToolChoice = basedOn.ToolChoice;
        OuboundFunctionsContent = basedOn.OuboundFunctionsContent;
        Adapter = basedOn.Adapter;
    }

	/// <summary>
	///     The model to use for this request
	/// </summary>
	[JsonProperty("model")]
    public string? Model { get; set; } = Models.Model.ChatGPTTurbo;

	/// <summary>
	///     The messages to send with this Chat Request
	/// </summary>
	[JsonProperty("messages")]
	[JsonConverter(typeof(ChatMessageRequestMessagesJsonConverter))]
    public IList<ChatMessage>? Messages { get; set; }

	/// <summary>
	///     What sampling temperature to use. Higher values means the model will take more risks. Try 0.9 for more creative
	///     applications, and 0 (argmax sampling) for ones with a well-defined answer. It is generally recommend to use this or
	///     <see cref="TopP" /> but not both.
	/// </summary>
	[JsonProperty("temperature")]
    public double? Temperature { get; set; }

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
	///     The seed to use for for deterministic requests.
	/// </summary>
	[JsonProperty("seed")]
	public int? Seed { get; set; }
	
	/// <summary>
	///     The response format to use.
	/// </summary>
	[JsonProperty("response_format")]
	public ChatRequestResponseFormats? ResponseFormat { get; set; }
	
	/// <summary>
	///     Specifies where the results should stream and be returned at one time.  Do not set this yourself, use the
	///     appropriate methods on <see cref="CompletionEndpoint" /> instead.
	/// </summary>
	[JsonProperty("stream")]
    public bool Stream { get; internal set; }

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
                MultipleStopSequences = new[] { value };
        }
    }

	/// <summary>
	///     How many tokens to complete to. Can return fewer if a stop sequence is hit.  Defaults to 16.
	/// </summary>
	[JsonProperty("max_tokens")]
    public int? MaxTokens { get; set; }

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
    [JsonConverter(typeof(OutboundToolCall.OutboundToolCallConverter))]
    public OutboundToolCall? ToolChoice { get; set; }

	/// <summary>
	///     If set the functions part of the outbound request encoded as JSON are stored here.
	///     This can be used a cheap heuristic for counting tokens used when streaming.
	///     Note that OpenAI silently transforms the provided JSON-schema into TypeScript and hence the real usage will be
	///     somewhat lower.
	/// </summary>
	[JsonIgnore]
    public Ref<string>? OuboundFunctionsContent { get; internal set; }
	
	/// <summary>
	///		This can be any API provider specific data. Currently used in KoboldCpp.
	/// </summary>
	[JsonProperty("adapter")]
	public Dictionary<string, object?>? Adapter { get; set; }
	
	internal class ChatMessageRequestMessagesJsonConverter : JsonConverter<IList<ChatMessage>?>
	{
		public override void WriteJson(JsonWriter writer, IList<ChatMessage>? value, JsonSerializer serializer)
		{
			if (value is null)
			{
				writer.WriteNull();
				return;
			}
			
			writer.WriteStartArray();

			foreach (ChatMessage msg in value)
			{
				writer.WriteStartObject();
				
				writer.WritePropertyName("role");
				writer.WriteValue(msg.rawRole);

				if (msg.Role is not null)
				{
					if (ChatMessageRole.Tool.Equals(msg.Role))
					{
						writer.WritePropertyName("tool_call_id");
						writer.WriteValue(msg.Name);	
					}
					else if (ChatMessageRole.Assistant.Equals(msg.Role))
					{
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
					}
					else if (ChatMessageRole.Tool.Equals(msg.Role))
					{
						writer.WritePropertyName("tool_call_id");
						writer.WriteValue(msg.ToolCallId);
					}
				}
				
				if (!string.IsNullOrWhiteSpace(msg.Name))
				{
					writer.WritePropertyName("name");
					writer.WriteValue(msg.Name);	
				}
				
				writer.WritePropertyName("content");
				
				if (msg.Parts?.Count > 0)
				{
					writer.WriteStartArray();
					
					foreach (ChatMessagePart part in msg.Parts)
					{
						writer.WriteStartObject();
						
						writer.WritePropertyName("type");
						writer.WriteValue(part.Type);

						if (part.Type.Value == ChatMessageTypes.Text.Value)
						{
							writer.WritePropertyName("text");
							writer.WriteValue(part.Text);	
						}
						else if (part.Type.Value == ChatMessageTypes.Image.Value)
						{
							writer.WritePropertyName("image_url");
							writer.WriteStartObject();
							
							writer.WritePropertyName("url");
							writer.WriteValue(part.Image?.Url);	
							
							writer.WriteEndObject();
						}
						
						writer.WriteEndObject();
					}
					
					writer.WriteEndArray();
				}
				else
				{
					writer.WriteValue(msg.Content);
				}
				
				writer.WriteEndObject();
			}
			
			writer.WriteEndArray();
		}

		public override IList<ChatMessage>? ReadJson(JsonReader reader, Type objectType, IList<ChatMessage>? existingValue, bool hasExistingValue,
			JsonSerializer serializer)
		{
			return existingValue;
		}
	}
}