    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using LlmTornado.Vendor.Anthropic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;

    namespace LlmTornado.Chat.Vendors.Cohere;

        /// <summary>
        /// Base class for Cohere chat stream events.
        /// </summary>
        [JsonConverter(typeof(CohereChatStreamEventConverter))]
        internal abstract class CohereChatStreamEvent
        {
            /// <summary>
            /// The type of the event.
            /// </summary>
            [JsonProperty("type")]
            public CohereChatStreamEventType Type { get; set; }
        }

        /// <summary>
        /// The type of a Cohere chat stream event.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        internal enum CohereChatStreamEventType
        {
            /// <summary>
            /// A stream has started.
            /// </summary>
            [EnumMember(Value = "message-start")]
            MessageStart,
            /// <summary>
            /// A new content block has started.
            /// </summary>
            [EnumMember(Value = "content-start")]
            ContentStart,
            /// <summary>
            /// A delta of chat text content.
            /// </summary>
            [EnumMember(Value = "content-delta")]
            ContentDelta,
            /// <summary>
            /// The content block has ended.
            /// </summary>
            [EnumMember(Value = "content-end")]
            ContentEnd,
            /// <summary>
            /// A delta of tool plan text.
            /// </summary>
            [EnumMember(Value = "tool-plan-delta")]
            ToolPlanDelta,
            /// <summary>
            /// A tool call has started streaming.
            /// </summary>
            [EnumMember(Value = "tool-call-start")]
            ToolCallStart,
            /// <summary>
            /// A delta in tool call arguments.
            /// </summary>
            [EnumMember(Value = "tool-call-delta")]
            ToolCallDelta,
            /// <summary>
            /// A tool call has finished streaming.
            /// </summary>
            [EnumMember(Value = "tool-call-end")]
            ToolCallEnd,
            /// <summary>
            /// A citation has been created.
            /// </summary>
            [EnumMember(Value = "citation-start")]
            CitationStart,
            /// <summary>
            /// A citation has finished streaming.
            /// </summary>
            [EnumMember(Value = "citation-end")]
            CitationEnd,
            /// <summary>
            /// The chat message has ended.
            /// </summary>
            [EnumMember(Value = "message-end")]
            MessageEnd,
            /// <summary>
            /// A debug event.
            /// </summary>
            [EnumMember(Value = "debug")]
            Debug
        }

        /// <summary>
        /// A streamed event which signifies that a stream has started.
        /// </summary>
        internal class MessageStartEvent : CohereChatStreamEvent
        {
            /// <summary>
            /// The delta for the message start event.
            /// </summary>
            [JsonProperty("delta")]
            public MessageStartDelta? Delta { get; set; }
            
            /// <summary>
            /// Unique identifier for the generated reply.
            /// </summary>
            [JsonProperty("id")]
            public string? Id { get; set; }
        }
        
        /// <summary>
        /// The delta for a message start event.
        /// </summary>
        internal class MessageStartDelta
        {
            /// <summary>
            /// The message that is starting.
            /// </summary>
            [JsonProperty("message")]
            public MessageStartMessage? Message { get; set; }
        }
        
        /// <summary>
        /// The message that is starting.
        /// </summary>
        internal class MessageStartMessage
        {
            /// <summary>
            /// The role of the message.
            /// </summary>
            [JsonProperty("role")]
            public string? Role { get; set; }
        }

        /// <summary>
        /// A streamed delta event which signifies that a new content block has started.
        /// </summary>
        internal class ContentStartEvent : CohereChatStreamEvent
        {
            /// <summary>
            /// The delta for the content start event.
            /// </summary>
            [JsonProperty("delta")]
            public ContentStartDelta? Delta { get; set; }
            
            /// <summary>
            /// The index of the content block.
            /// </summary>
            [JsonProperty("index")]
            public int? Index { get; set; }
        }
        
        /// <summary>
        /// The delta for a content start event.
        /// </summary>
        internal class ContentStartDelta
        {
            /// <summary>
            /// The message content that is starting.
            /// </summary>
            [JsonProperty("message")]
            public ContentStartMessage? Message { get; set; }
        }
        
        /// <summary>
        /// The message content that is starting.
        /// </summary>
        internal class ContentStartMessage
        {
            /// <summary>
            /// The content block.
            /// </summary>
            [JsonProperty("content")]
            public ContentStartMessageContent? Content { get; set; }
        }
        
        /// <summary>
        /// The content of a message.
        /// </summary>
        internal class ContentStartMessageContent
        {
            /// <summary>
            /// Thinking process of the model.
            /// </summary>
            [JsonProperty("thinking")]
            public string? Thinking { get; set; }
            
            /// <summary>
            /// The text content.
            /// </summary>
            [JsonProperty("text")]
            public string? Text { get; set; }
            
            /// <summary>
            /// The type of content.
            /// </summary>
            [JsonProperty("type")]
            public string? Type { get; set; }
        }

        /// <summary>
        /// A streamed delta event which contains a delta of chat text content.
        /// </summary>
        internal class ContentDeltaEvent : CohereChatStreamEvent
        {
            /// <summary>
            /// The delta for the content delta event.
            /// </summary>
            [JsonProperty("delta")]
            public ContentDelta? Delta { get; set; }
            
            /// <summary>
            /// The index of the content block.
            /// </summary>
            [JsonProperty("index")]
            public int? Index { get; set; }
            
            /// <summary>
            /// Log probabilities for the tokens.
            /// </summary>
            [JsonProperty("logprobs")]
            public VendorCohereLogProbs? Logprobs { get; set; }
        }
        
        /// <summary>
        /// The delta for a content delta event.
        /// </summary>
        internal class ContentDelta
        {
            /// <summary>
            /// The message content delta.
            /// </summary>
            [JsonProperty("message")]
            public ContentDeltaMessage? Message { get; set; }
        }
        
        /// <summary>
        /// The message content delta.
        /// </summary>
        internal class ContentDeltaMessage
        {
            /// <summary>
            /// The content delta.
            /// </summary>
            [JsonProperty("content")]
            public ContentDeltaMessageContent? Content { get; set; }
        }
        
        /// <summary>
        /// The content of a message delta.
        /// </summary>
        internal class ContentDeltaMessageContent
        {
            /// <summary>
            /// Thinking process of the model.
            /// </summary>
            [JsonProperty("thinking")]
            public string? Thinking { get; set; }
            
            /// <summary>
            /// The text content delta.
            /// </summary>
            [JsonProperty("text")]
            public string? Text { get; set; }
        }

        /// <summary>
        /// A streamed delta event which signifies that the content block has ended.
        /// </summary>
        internal class ContentEndEvent : CohereChatStreamEvent
        {
            /// <summary>
            /// The index of the content block.
            /// </summary>
            [JsonProperty("index")]
            public int? Index { get; set; }
        }

        /// <summary>
        /// A streamed event which contains a delta of tool plan text.
        /// </summary>
        internal class ToolPlanDeltaEvent : CohereChatStreamEvent
        {
            /// <summary>
            /// The delta for the tool plan delta event.
            /// </summary>
            [JsonProperty("delta")]
            public ToolPlanDelta? Delta { get; set; }
        }
        
        /// <summary>
        /// The delta for a tool plan delta event.
        /// </summary>
        internal class ToolPlanDelta
        {
            /// <summary>
            /// The message with the tool plan delta.
            /// </summary>
            [JsonProperty("message")]
            public ToolPlanDeltaMessage? Message { get; set; }
        }
        
        /// <summary>
        /// The message with the tool plan delta.
        /// </summary>
        internal class ToolPlanDeltaMessage
        {
            /// <summary>
            /// The tool plan delta.
            /// </summary>
            [JsonProperty("tool_plan")]
            public string? ToolPlan { get; set; }
        }

        /// <summary>
        /// A streamed event delta which signifies a tool call has started streaming.
        /// </summary>
        internal class ToolCallStartEvent : CohereChatStreamEvent
        {
            /// <summary>
            /// The delta for the tool call start event.
            /// </summary>
            [JsonProperty("delta")]
            public ToolCallStartDelta? Delta { get; set; }
            
            /// <summary>
            /// The index of the tool call.
            /// </summary>
            [JsonProperty("index")]
            public int? Index { get; set; }
        }
        
        /// <summary>
        /// The delta for a tool call start event.
        /// </summary>
        internal class ToolCallStartDelta
        {
            /// <summary>
            /// The message with the tool call.
            /// </summary>
            [JsonProperty("message")]
            public ToolCallStartMessage? Message { get; set; }
        }
        
        /// <summary>
        /// The message with the tool call.
        /// </summary>
        internal class ToolCallStartMessage
        {
            /// <summary>
            /// The tool call.
            /// </summary>
            [JsonProperty("tool_calls")]
            public VendorCohereToolCall? ToolCalls { get; set; }
        }

        /// <summary>
        /// A streamed event delta which signifies a delta in tool call arguments.
        /// </summary>
        internal class ToolCallDeltaEvent : CohereChatStreamEvent
        {
            /// <summary>
            /// The delta for the tool call delta event.
            /// </summary>
            [JsonProperty("delta")]
            public ToolCallDelta? Delta { get; set; }
            
            /// <summary>
            /// The index of the tool call.
            /// </summary>
            [JsonProperty("index")]
            public int? Index { get; set; }
        }
        
        /// <summary>
        /// The delta for a tool call delta event.
        /// </summary>
        internal class ToolCallDelta
        {
            /// <summary>
            /// The message with the tool call delta.
            /// </summary>
            [JsonProperty("message")]
            public ToolCallDeltaMessage? Message { get; set; }
        }
        
        /// <summary>
        /// The message with the tool call delta.
        /// </summary>
        internal class ToolCallDeltaMessage
        {
            /// <summary>
            /// The tool call delta.
            /// </summary>
            [JsonProperty("tool_calls")]
            public ToolCallDeltaMessageToolCalls? ToolCalls { get; set; }
        }
        
        /// <summary>
        /// The tool call delta.
        /// </summary>
        internal class ToolCallDeltaMessageToolCalls
        {
            /// <summary>
            /// The function call delta.
            /// </summary>
            [JsonProperty("function")]
            public ToolCallDeltaFunction? Function { get; set; }
        }
        
        /// <summary>
        /// The function call delta.
        /// </summary>
        internal class ToolCallDeltaFunction
        {
            /// <summary>
            /// The arguments delta.
            /// </summary>
            [JsonProperty("arguments")]
            public string? Arguments { get; set; }
        }

        /// <summary>
        /// A streamed event delta which signifies a tool call has finished streaming.
        /// </summary>
        internal class ToolCallEndEvent : CohereChatStreamEvent
        {
            /// <summary>
            /// The index of the tool call.
            /// </summary>
            [JsonProperty("index")]
            public int? Index { get; set; }
        }

        /// <summary>
        /// A streamed event which signifies a citation has been created.
        /// </summary>
        internal class CitationStartEvent : CohereChatStreamEvent
        {
            /// <summary>
            /// The delta for the citation start event.
            /// </summary>
            [JsonProperty("delta")]
            public CitationStartDelta? Delta { get; set; }
            
            /// <summary>
            /// The index of the citation.
            /// </summary>
            [JsonProperty("index")]
            public int? Index { get; set; }
        }
        
        /// <summary>
        /// The delta for a citation start event.
        /// </summary>
        internal class CitationStartDelta
        {
            /// <summary>
            /// The message with the citation.
            /// </summary>
            [JsonProperty("message")]
            public CitationStartMessage? Message { get; set; }
        }
        
        /// <summary>
        /// The message with the citation.
        /// </summary>
        internal class CitationStartMessage
        {
            /// <summary>
            /// The citation.
            /// </summary>
            [JsonProperty("citations")]
            public VendorCohereChatCitation? Citations { get; set; }
        }

        /// <summary>
        /// A source for a citation.
        /// </summary>
        [JsonConverter(typeof(CohereCitationSourceConverter))]
        public abstract class CohereCitationSource
        {
            /// <summary>
            /// The type of the source.
            /// </summary>
            [JsonProperty("type")]
            public string? Type { get; set; }
        }

        /// <summary>
        /// A tool source for a citation.
        /// </summary>
        public class ToolSource : CohereCitationSource
        {
            /// <summary>
            /// The unique identifier of the document.
            /// </summary>
            [JsonProperty("id")]
            public string? Id { get; set; }
            
            /// <summary>
            /// The tool output.
            /// </summary>
            [JsonProperty("tool_output")]
            public Dictionary<string, object>? ToolOutput { get; set; }
        }

        /// <summary>
        /// A document source for a citation.
        /// </summary>
        public class DocumentSource : CohereCitationSource
        {
            /// <summary>
            /// The document.
            /// </summary>
            [JsonProperty("document")]
            public Dictionary<string, object>? Document { get; set; }
            
            /// <summary>
            /// The unique identifier of the document.
            /// </summary>
            [JsonProperty("id")]
            public string? Id { get; set; }
        }

        /// <summary>
        /// A streamed event which signifies a citation has finished streaming.
        /// </summary>
        internal class CitationEndEvent : CohereChatStreamEvent
        {
            /// <summary>
            /// The index of the citation.
            /// </summary>
            [JsonProperty("index")]
            public int? Index { get; set; }
        }

        /// <summary>
        /// A streamed event which signifies that the chat message has ended.
        /// </summary>
        internal class MessageEndEvent : CohereChatStreamEvent
        {
            /// <summary>
            /// The delta for the message end event.
            /// </summary>
            [JsonProperty("delta")]
            public MessageEndDelta? Delta { get; set; }
            
            /// <summary>
            /// The unique identifier for the message.
            /// </summary>
            [JsonProperty("id")]
            public string? Id { get; set; }
        }
        
        /// <summary>
        /// The delta for a message end event.
        /// </summary>
        internal class MessageEndDelta
        {
            /// <summary>
            /// An error message if an error occurred during the generation.
            /// </summary>
            [JsonProperty("error")]
            public string? Error { get; set; }
            
            /// <summary>
            /// The reason a chat request has finished.
            /// </summary>
            [JsonProperty("finish_reason")]
            public VendorCohereChatFinishReason? FinishReason { get; set; }
            
            /// <summary>
            /// The usage information for the request.
            /// </summary>
            [JsonProperty("usage")]
            public VendorCohereUsage? Usage { get; set; }
        }

        /// <summary>
        /// A debug event.
        /// </summary>
        internal class DebugEvent : CohereChatStreamEvent
        {
            /// <summary>
            /// The type of the debug event.
            /// </summary>
            [JsonProperty("event_type")]
            public string? EventType { get; set; }
            
            /// <summary>
            /// The prompt.
            /// </summary>
            [JsonProperty("prompt")]
            public string? Prompt { get; set; }
        }

        // The following classes are based on those in LlmTornado.Chat.Vendors.Cohere.VendorCohereChatResult
        // and are reproduced here to be self-contained.
        
        /// <summary>
        /// Log probabilities for tokens.
        /// </summary>
        internal class VendorCohereLogProbs
        {
            /// <summary>
            /// The token ids of each token used to construct the text chunk.
            /// </summary>
            [JsonProperty("token_ids")]
            public List<int>? TokenIds { get; set; }
            
            /// <summary>
            /// The text chunk for which the log probabilities was calculated.
            /// </summary>
            [JsonProperty("text")]
            public string? Text { get; set; }
            
            /// <summary>
            /// The log probability of each token used to construct the text chunk.
            /// </summary>
            [JsonProperty("logprobs")]
            public List<double>? Logprobs { get; set; }
        }

        /// <summary>
        /// A citation from the model.
        /// </summary>
        public class VendorCohereChatCitation
        {
            /// <summary>
            /// Start index of the cited snippet in the original source text.
            /// </summary>
            [JsonProperty("start")]
            public int? Start { get; set; }
            
            /// <summary>
            /// End index of the cited snippet in the original source text.
            /// </summary>
            [JsonProperty("end")]
            public int? End { get; set; }
            
            /// <summary>
            /// Text snippet that is being cited.
            /// </summary>
            [JsonProperty("text")]
            public string? Text { get; set; }
            
            /// <summary>
            /// The sources for the citation.
            /// </summary>
            [JsonProperty("sources")]
            public List<CohereCitationSource>? Sources { get; set; }
            
            /// <summary>
            /// Index of the content block in which this citation appears.
            /// </summary>
            [JsonProperty("content_index")]
            public int? ContentIndex { get; set; }
            
            /// <summary>
            /// The type of citation which indicates what part of the response the citation is for.
            /// </summary>
            [JsonProperty("type")]
            public VendorCohereCitationType? Type { get; set; }
        }
        
        /// <summary>
        /// The type of citation.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public enum VendorCohereCitationType
        {
            /// <summary>
            /// Citation for text content.
            /// </summary>
            [EnumMember(Value = "TEXT_CONTENT")]
            TextContent,
            /// <summary>
            /// Citation for thinking content.
            /// </summary>
            [EnumMember(Value = "THINKING_CONTENT")]
            ThinkingContent,
            /// <summary>
            /// Citation for a plan.
            /// </summary>
            [EnumMember(Value = "PLAN")]
            Plan
        }
        
        /// <summary>
        /// The reason a chat request has finished.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        internal enum VendorCohereChatFinishReason
        {
            /// <summary>
            /// The model finished sending a complete message.
            /// </summary>
            [EnumMember(Value = "COMPLETE")]
            Complete,
            /// <summary>
            /// One of the provided stop_sequence entries was reached in the model’s generation.
            /// </summary>
            [EnumMember(Value = "STOP_SEQUENCE")]
            StopSequence,
            /// <summary>
            /// The number of generated tokens exceeded the model’s context length or the value specified via the max_tokens parameter.
            /// </summary>
            [EnumMember(Value = "MAX_TOKENS")]
            MaxTokens,
            /// <summary>
            /// The model generated a Tool Call and is expecting a Tool Message in return.
            /// </summary>
            [EnumMember(Value = "TOOL_CALL")]
            ToolCall,
            /// <summary>
            /// The generation failed due to an internal error.
            /// </summary>
            [EnumMember(Value = "ERROR")]
            Error
        }

        /// <summary>
        /// Billed units for a request.
        /// </summary>
        internal class VendorCohereBilledUnits
        {
            /// <summary>
            /// The number of billed input tokens.
            /// </summary>
            [JsonProperty("input_tokens")]
            public double? InputTokens { get; set; }
            
            /// <summary>
            /// The number of billed output tokens.
            /// </summary>
            [JsonProperty("output_tokens")]
            public double? OutputTokens { get; set; }
            
            /// <summary>
            /// The number of billed search units.
            /// </summary>
            [JsonProperty("search_units")]
            public double? SearchUnits { get; set; }
            
            /// <summary>
            /// The number of billed classifications units.
            /// </summary>
            [JsonProperty("classifications")]
            public double? Classifications { get; set; }
        }

        /// <summary>
        /// Token counts for a request.
        /// </summary>
        internal class VendorCohereTokens
        {
            /// <summary>
            /// The number of tokens used as input to the model.
            /// </summary>
            [JsonProperty("input_tokens")]
            public double? InputTokens { get; set; }
            
            /// <summary>
            /// The number of tokens produced by the model.
            /// </summary>
            [JsonProperty("output_tokens")]
            public double? OutputTokens { get; set; }
        }

        internal class CohereChatStreamEventConverter : JsonConverter<CohereChatStreamEvent>
        {
            public override CohereChatStreamEvent? ReadJson(JsonReader reader, Type objectType, CohereChatStreamEvent? existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }
                
                JObject item = JObject.Load(reader);
                string? type = item["type"]?.Value<string>();

                CohereChatStreamEvent? target = type switch
                {
                    "message-start" => new MessageStartEvent(),
                    "content-start" => new ContentStartEvent(),
                    "content-delta" => new ContentDeltaEvent(),
                    "content-end" => new ContentEndEvent(),
                    "tool-plan-delta" => new ToolPlanDeltaEvent(),
                    "tool-call-start" => new ToolCallStartEvent(),
                    "tool-call-delta" => new ToolCallDeltaEvent(),
                    "tool-call-end" => new ToolCallEndEvent(),
                    "citation-start" => new CitationStartEvent(),
                    "citation-end" => new CitationEndEvent(),
                    "message-end" => new MessageEndEvent(),
                    "debug" => new DebugEvent(),
                    _ => null
                };

                if (target is null)
                {
                    return null;
                }

                using (JsonReader jsonReader = item.CreateReader())
                {
                    serializer.Populate(jsonReader, target);
                }

                return target;
            }

            public override void WriteJson(JsonWriter writer, CohereChatStreamEvent? value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }
        
        internal class CohereCitationSourceConverter : JsonConverter<CohereCitationSource>
        {
            public override CohereCitationSource? ReadJson(JsonReader reader, Type objectType, CohereCitationSource? existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }
                
                JObject item = JObject.Load(reader);
                string? type = item["type"]?.Value<string>();

                CohereCitationSource? target = type switch
                {
                    "tool" => new ToolSource(),
                    "document" => new DocumentSource(),
                    _ => null
                };
                
                if (target is null)
                {
                    return null;
                }

                using (JsonReader jsonReader = item.CreateReader())
                {
                    serializer.Populate(jsonReader, target);
                }

                return target;
            }

            public override void WriteJson(JsonWriter writer, CohereCitationSource? value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }
        }