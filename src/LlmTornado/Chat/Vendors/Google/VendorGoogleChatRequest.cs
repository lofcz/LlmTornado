using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Chat.Vendors.Google;

/// <summary>
/// Generation config for Google.
/// </summary>
internal class VendorGoogleChatRequestGenerationConfig
{
    /// <summary>
    /// Optional. The set of character sequences (up to 5) that will stop output generation. If specified, the API will stop at the first appearance of a stop_sequence. The stop sequence will not be included as part of the response.
    /// </summary>
    [JsonProperty("stopSequences")]
    public List<string>? StopSequences { get; set; }
    
    /// <summary>
    /// Output response mimetype of the generated candidate text. Supported mimetype: text/plain: (default) Text output. application/json: JSON response in the candidates.
    /// </summary>
    [JsonProperty("responseMimeType")]
    public string? ResponseMimeType { get; set; }
    
    /// <summary>
    /// Output response schema of the generated candidate text when response mime type can have schema. Schema can be objects, primitives or arrays and is a subset of OpenAPI schema.
    /// <see cref="ResponseMimeType"/> has to be "application/json".
    /// </summary>
    [JsonProperty("responseSchema")]
    public object? ResponseSchema { get; set; }
    
    /// <summary>
    /// The requested modalities of the response. Represents the set of modalities that the model can return, and should be expected in the response. This is an exact match to the modalities of the response.
    /// A model may have multiple combinations of supported modalities. If the requested modalities do not match any of the supported combinations, an error will be returned.
    /// An empty list is equivalent to requesting only text.
    /// <br/>
    /// MODALITY_UNSPECIFIED<br/>
    /// TEXT<br/>
    /// IMAGE<br/>
    /// AUDIO
    /// </summary>
    [JsonProperty("responseModalities")]
    public List<string>? ResponseModalities { get; set; }
    
    /// <summary>
    /// Optional. Number of generated responses to return. Currently, this value can only be set to 1. If unset, this will default to 1.
    /// </summary>
    [JsonProperty("candidateCount")]
    public int? CandidateCount { get; set; }
    
    /// <summary>
    /// Note: The default value varies by model, see the Model.output_token_limit attribute of the Model returned from the getModel function.
    /// </summary>
    [JsonProperty("maxOutputTokens")]
    public int? MaxOutputTokens { get; set; }
    
    /// <summary>
    /// Values can range from [0.0, 2.0].
    /// </summary>
    [JsonProperty("temperature")]
    public double? Temperature { get; set; }
    
    /// <summary>
    /// Optional. The maximum cumulative probability of tokens to consider when sampling. The model uses combined Top-k and Top-p (nucleus) sampling.
    /// </summary>
    [JsonProperty("topP")]
    public double? TopP { get; set; }
    
    /// <summary>
    /// Optional. The maximum number of tokens to consider when sampling.
    /// </summary>
    [JsonProperty("topK")]
    public int? TopK { get; set; }
    
    /// <summary>
    /// Optional. Presence penalty applied to the next token's logprobs if the token has already been seen in the response. This penalty is binary on/off and not dependant on the number of times the token is used (after the first). Use frequencyPenalty for a penalty that increases with each use. A positive penalty will discourage the use of tokens that have already been used in the response, increasing the vocabulary.
    /// </summary>
    [JsonProperty("presencePenalty")]
    public double? PresencePenalty { get; set; }
    
    /// <summary>
    /// Optional. Frequency penalty applied to the next token's logprobs, multiplied by the number of times each token has been seen in the respponse so far. A positive penalty will discourage the use of tokens that have already been used, proportional to the number of times the token has been used: The more a token is used, the more dificult it is for the model to use that token again increasing the vocabulary of responses. Caution: A negative penalty will encourage the model to reuse tokens proportional to the number of times the token has been used. Small negative values will reduce the vocabulary of a response. Larger negative values will cause the model to start repeating a common token until it hits the maxOutputTokens limit: "...the the the the the...".
    /// </summary>
    [JsonProperty("frequencyPenalty")]
    public double? FrequencyPenalty { get; set; }
    
    /// <summary>
    /// Optional. If true, export the logprobs results in response.
    /// </summary>
    [JsonProperty("responseLogprobs")]
    public bool? ResponseLogprobs { get; set; }
    
    /// <summary>
    /// Optional. Only valid if responseLogprobs=True. This sets the number of top logprobs to return at each decoding step in the Candidate.logprobs_result.
    /// </summary>
    [JsonProperty("logprobs")]
    public int? Logprobs { get; set; }
    
    /// <summary>
    /// Optional. Enables enhanced civic answers. It may not be available for all models.
    /// </summary>
    [JsonProperty("enableEnhancedCivicAnswers")]
    public bool? EnableEnhancedCivicAnswers { get; set; }
    
    /// <summary>
    /// Optional. The speech generation config.
    /// </summary>
    /// <returns></returns>
    [JsonProperty("speechConfig")]
    public VendorGoogleChatRequestSpeechConfig? SpeechConfig { get; set; }
    
    /// <summary>
    /// Optional. Config for thinking features.
    /// </summary>
    [JsonProperty("thinkingConfig")]
    public VendorGoogleChatRequestThinkingConfig? ThinkingConfig { get; set; }
    
    /// <summary>
    /// MEDIA_RESOLUTION_UNSPECIFIED = Media resolution has not been set.<br/>
    /// MEDIA_RESOLUTION_LOW = Media resolution set to low (64 tokens).<br/>
    /// MEDIA_RESOLUTION_MEDIUM	= Media resolution set to medium (256 tokens).<br/>
    /// MEDIA_RESOLUTION_HIGH = Media resolution set to high (zoomed reframing with 256 tokens).<br/>
    /// </summary>
    [JsonProperty("mediaResolution")]
    public string? MediaResolution { get; set; }
}

internal class VendorGoogleChatRequestVoiceConfig
{
    /// <summary>
    /// The configuration for the prebuilt speaker to use.
    /// </summary>
    [JsonIgnore]
    public VendorGoogleChatRequestPrebuiltVoiceConfig? PrebuiltVoiceConfig { get; set; }

    /// <summary>
    /// The configuration for the speaker to use.
    /// </summary>
    [JsonProperty("voice_config")]
    public object? VoiceConfig => PrebuiltVoiceConfig;
}

internal class VendorGoogleChatRequestMultiSpeakerVoiceConfig
{
    /// <summary>
    /// todo: documentation, once released
    /// </summary>
    [JsonProperty("speaker_voice_configs")]
    public List<VendorGoogleChatRequestMultiSpeakerVoiceConfigSpeaker>? SpeakerVoiceConfigs { get; set; }
}

internal class VendorGoogleChatRequestMultiSpeakerVoiceConfigSpeaker
{
    [JsonProperty("speaker")]
    public string Speaker { get; set; }
    
    [JsonProperty("voice_config")]
    public VendorGoogleChatRequestPrebuiltVoiceConfigWrapper? PrebuiltVoiceConfig { get; set; }
}

internal class VendorGoogleChatRequestPrebuiltVoiceConfigWrapper
{
    [JsonProperty("prebuilt_voice_config")]
    public VendorGoogleChatRequestPrebuiltVoiceConfig? VoiceConfig { get; set; }
}

internal class VendorGoogleChatRequestPrebuiltVoiceConfig
{
    /// <summary>
    /// The name of the preset voice to use.
    /// </summary>
    [JsonProperty("voiceName")]
    public string? VoiceName { get; set; }
}

internal class VendorGoogleChatRequestSpeechConfig
{
    /// <summary>
    /// The configuration in case of single-voice output.
    /// </summary>
    [JsonProperty("voiceConfig")]
    public VendorGoogleChatRequestVoiceConfig? VoiceConfig { get; set; }
    
    /// <summary>
    /// The configuration in case of multi-speaker output.
    /// </summary>
    [JsonProperty("multi_speaker_voice_config")]
    public VendorGoogleChatRequestMultiSpeakerVoiceConfig? MultiSpeakerVoiceConfig { get; set; }
    
    /// <summary>
    /// Optional. Language code (in BCP 47 format, e.g. "en-US") for speech synthesis.
    /// Valid values are: de-DE, en-AU, en-GB, en-IN, es-US, fr-FR, hi-IN, pt-BR, ar-XA, es-ES, fr-CA, id-ID, it-IT, ja-JP, tr-TR, vi-VN, bn-IN, gu-IN, kn-IN, ml-IN, mr-IN, ta-IN, te-IN, nl-NL, ko-KR, cmn-CN, pl-PL, ru-RU, and th-TH.
    /// </summary>
    [JsonProperty("languageCode")]
    public string? LanguageCode { get; set; }
}

internal class VendorGoogleChatRequestThinkingConfig
{
    /// <summary>
    /// Indicates whether to include thoughts in the response. If true, thoughts are returned only when available.
    /// </summary>
    [JsonProperty("includeThoughts")]
    public bool? IncludeThoughts { get; set; }
    
    /// <summary>
    /// The thinkingBudget parameter gives the model guidance on the number of thinking tokens it can use when generating a response. A greater number of tokens is typically associated with more detailed thinking, which is needed for solving more complex tasks. thinkingBudget must be an integer in the range 0 to 24576. Setting the thinking budget to 0 disables thinking. Budgets from 1 to 1024 tokens will be set to 1024.
    /// Depending on the prompt, the model might overflow or underflow the token budget.
    /// </summary>
    [JsonProperty("thinkingBudget")]
    public int? ThinkingBudget { get; set; }
}

internal class VendorGoogleChatRequestMessagePart
{
    /// <summary>
    /// Indicates if the part is thought from the model.
    /// </summary>
    [JsonProperty("thought")]
    public bool? Thought { get; set; }
    
    /// <summary>
    /// An opaque signature for the thought so it can be reused in subsequent requests. A base64-encoded string.
    /// </summary>
    [JsonProperty("thoughtSignature")]
    public string? ThoughtSignature { get; set; }

    /// <summary>
    /// Inline text.
    /// </summary>
    [JsonProperty("text")]
    public string? Text { get; set; }
    
    [JsonProperty("inlineData")]
    public VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartInlineData? InlineData { get; set; }
    
    [JsonProperty("functionCall")]
    public VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartFunctionCall? FunctionCall { get; set; }
    
    [JsonProperty("functionResponse")]
    public VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartFunctionResponse? FunctionResponse { get; set; }

    [JsonProperty("fileData")]
    public VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartFileData? FileData { get; set; }
    
    [JsonProperty("executableCode")]
    public VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartExecutableCode? ExecutableCode { get; set; }
    
    [JsonProperty("codeExecutionResult")]
    public VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartCodeExecutionResult? CodeExecutionResult { get; set; }
    
    [JsonProperty("videoMetadata")]
    public VendorGoogleChatRequest.VendorGoogleChatRequestMetadataVideo? VideoMetadata { get; set; }
    
    public VendorGoogleChatRequestMessagePart()
    {
        
    }

    public VendorGoogleChatRequestMessagePart(ChatMessagePart part)
    {
        switch (part.Type)
        {
            case ChatMessageTypes.Text:
            {
                Text = part.Text;
                break;
            }
            case ChatMessageTypes.Image:
            {
                if (part.Image is not null)
                {
                    if (part.Image.MimeType is null)
                    {
                        throw new Exception("Google requires MIME type of all images to be set, supported values are: image/png, image/jpeg");
                    }
                    
                    InlineData = new VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartInlineData
                    {
                        MimeType = part.Image.MimeType,
                        Data = part.Image.Url
                    };
                }
                
                break;
            }
            case ChatMessageTypes.Audio:
            {
                if (part.Audio is not null)
                {
                    if (part.Audio.MimeType is null)
                    {
                        throw new Exception("Google requires MIME type of all audio to be set, supported values are: audio/wav, audio/mp3, audio/aiff, audio/aac, audio/ogg, audio/flac");
                    }

                    InlineData = new VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartInlineData
                    {
                        MimeType = part.Audio.MimeType,
                        Data = part.Audio.Data
                    };
                }

                break;
            }
            case ChatMessageTypes.FileLink:
            {
                if (part.FileLinkData is not null)
                {
                    FileData = new VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartFileData
                    {
                        FileUri = part.FileLinkData.FileUri,
                        MimeType = part.FileLinkData.MimeType
                    };
                }
                
                break;
            }
            case ChatMessageTypes.Reasoning:
            {
                if (part.Reasoning is not null)
                {
                    Text = part.Reasoning.Content;
                    Thought = true;
                    ThoughtSignature = part.Reasoning.Signature;
                }
                
                break;
            }
            case ChatMessageTypes.ExecutableCode:
            {
                if (part.ExecutableCode is not null)
                {
                    ExecutableCode = new VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartExecutableCode
                    {
                        Code = part.ExecutableCode.Code,
                        Language = part.ExecutableCode.Language switch
                        {
                            ChatMessagePartExecutableCodeLanguage.Unknown => VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartExecutableCodeLanguage.Unspecified,
                            ChatMessagePartExecutableCodeLanguage.Python => VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartExecutableCodeLanguage.Python,
                            _ => VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartExecutableCodeLanguage.Unspecified
                        }
                    };
                }
                
                break;
            }
            case ChatMessageTypes.CodeExecutionResult:
            {
                if (part.CodeExecutionResult is not null)
                {
                    CodeExecutionResult = new VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartCodeExecutionResult
                    {
                        Output = part.CodeExecutionResult.Output,
                        Outcome = part.CodeExecutionResult.Outcome switch
                        {
                            ChatMessagePartCodeExecutionResultOutcomes.Unknown => VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartCodeExecutionResultOutcome.Unspecified,
                            ChatMessagePartCodeExecutionResultOutcomes.Timeout => VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartCodeExecutionResultOutcome.DeadlineExceeded,
                            ChatMessagePartCodeExecutionResultOutcomes.Failed => VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartCodeExecutionResultOutcome.Failed,
                            ChatMessagePartCodeExecutionResultOutcomes.Ok => VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartCodeExecutionResultOutcome.Ok,
                            _ => VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartCodeExecutionResultOutcome.Unspecified
                        }
                    };
                }
                
                break;
            }
        }
    }

    public ChatMessagePart ToMessagePart(StringBuilder sb)
    {
        ChatMessagePart part = new ChatMessagePart();

        if (ExecutableCode is not null)
        {
            part.Type = ChatMessageTypes.ExecutableCode;
            part.ExecutableCode = new ChatMessagePartExecutableCode
            {
                Code = ExecutableCode.Code,
                Language = ExecutableCode.Language switch
                {
                    VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartExecutableCodeLanguage.Unspecified => ChatMessagePartExecutableCodeLanguage.Unknown,
                    VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartExecutableCodeLanguage.Python => ChatMessagePartExecutableCodeLanguage.Python,
                    _ => ChatMessagePartExecutableCodeLanguage.Unknown
                },
                NativeObject = ExecutableCode
            };
        }
        else if (CodeExecutionResult is not null)
        {
            part.Type = ChatMessageTypes.CodeExecutionResult;
            part.CodeExecutionResult = new ChatMessagePartCodeExecutionResult
            {
                Outcome = CodeExecutionResult.Outcome switch
                {
                    VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartCodeExecutionResultOutcome.Unspecified => ChatMessagePartCodeExecutionResultOutcomes.Unknown,
                    VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartCodeExecutionResultOutcome.Ok => ChatMessagePartCodeExecutionResultOutcomes.Ok,
                    VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartCodeExecutionResultOutcome.Failed => ChatMessagePartCodeExecutionResultOutcomes.Failed,
                    VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartCodeExecutionResultOutcome.DeadlineExceeded => ChatMessagePartCodeExecutionResultOutcomes.Timeout,
                    _ => ChatMessagePartCodeExecutionResultOutcomes.Unknown
                },
                Output = CodeExecutionResult.Output,
                NativeObject = CodeExecutionResult
            };
        }
        else if (Text is not null)
        {
            part.Type = ChatMessageTypes.Text;
            part.Text = Text;
            sb.Append(Text);
        }
        else if (InlineData is not null)
        {
            if (InlineData.MimeType.StartsWith("audio"))
            {
                part.Type = ChatMessageTypes.Audio;
                part.Audio = new ChatAudio(InlineData.Data, ChatAudioFormats.L16, InlineData.MimeType);
            }
            else
            {
                part.Type = ChatMessageTypes.Image;
                part.Image = new ChatImage(InlineData.Data)
                {
                    MimeType = InlineData.MimeType
                };   
            }
        }

        if (Thought ?? false)
        {
            part.Type = ChatMessageTypes.Reasoning;
            part.Reasoning = new ChatMessageReasoningData
            {
                Provider = LLmProviders.Google,
                Content = Text,
                Signature = ThoughtSignature
            };
        }

        return part;
    }

    public ToolCall ToToolCall()
    {
        if (FunctionCall is null)
        {
            return new ToolCall();
        }

        FunctionCall fc = new FunctionCall
        {
            Name = FunctionCall.Name,
            Arguments = FunctionCall.Args.ToJson() // todo: this is a bit slow as we encode already decoded value just to decode it once more once args are first accessed
        };

        ToolCall tc = new ToolCall
        {
            Id = FunctionCall.Name,
            FunctionCall = fc
        };

        return tc;
    }
}

internal class VendorGoogleChatRequest
{
    internal class VendorGoogleChatRequestMessagePartInlineData
    {
        /// <summary>
        /// image/png - image/jpeg... https://ai.google.dev/gemini-api/docs/prompting_with_media#supported_file_formats
        /// audio/L16;codec=pcm;rate=24000
        /// </summary>
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }
        
        /// <summary>
        /// A base64-encoded string.
        /// </summary>
        [JsonProperty("data")]
        public string Data { get; set; }
    }

    internal class VendorGoogleChatRequestMessagePartFunctionCall
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("args")]
        public Dictionary<string, object?> Args { get; set; } = [];
    }

    internal class VendorGoogleChatRequestMessagePartFunctionResponse
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("response")]
        public object Response { get; set; }
    }

    /// <summary>
    /// Video metadata. The metadata should only be specified while the video data is presented in inlineData or fileData.
    /// </summary>
    internal class VendorGoogleChatRequestMetadataVideo
    {
        /// <summary>
        /// Optional. The start offset of the video.
        /// A duration in seconds with up to nine fractional digits, ending with 's'. Example: "3.5s".
        /// </summary>
        [JsonProperty("startOffset")]
        [JsonConverter(typeof(VendorGoogleChatRequestDurationConverter))]
        public TimeSpan? StartOffset { get; set; }

        /// <summary>
        /// Optional. The end offset of the video.
        /// A duration in seconds with up to nine fractional digits, ending with 's'. Example: "3.5s".
        /// </summary>
        [JsonProperty("endOffset")]
        [JsonConverter(typeof(VendorGoogleChatRequestDurationConverter))]
        public TimeSpan? EndOffset { get; set; }

        /// <summary>
        /// Optional. The frame rate of the video sent to the model. If not specified, the default value will be 1.0. The fps range is (0.0, 24.0].
        /// </summary>
        [JsonProperty("fps")]
        public double? Fps { get; set; }
    }

    internal class VendorGoogleChatRequestDurationConverter : JsonConverter<TimeSpan?>
    {
        public override void WriteJson(JsonWriter writer, TimeSpan? value, JsonSerializer serializer)
        {
            if (value.HasValue)
            {
                writer.WriteValue($"{value.Value.TotalSeconds}s");
            }
            else
            {
                writer.WriteNull();
            }
        }

        public override TimeSpan? ReadJson(JsonReader reader, Type objectType, TimeSpan? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String)
            {
                string? s = (string?)reader.Value;
                
                if (s is not null && s.EndsWith('s'))
                {
                    if (double.TryParse(s[..^1], out double seconds))
                    {
                        return TimeSpan.FromSeconds(seconds);
                    }
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Result of executing the ExecutableCode.
    /// </summary>
    internal class VendorGoogleChatRequestMessagePartCodeExecutionResult
    {
        /// <summary>
        /// Required. Outcome of the code execution.
        /// </summary>
        [JsonProperty("outcome")]
        public VendorGoogleChatRequestMessagePartCodeExecutionResultOutcome Outcome { get; set; }

        /// <summary>
        /// Optional. Contains stdout when code execution is successful, stderr or other description otherwise.
        /// </summary>
        [JsonProperty("output")]
        public string? Output { get; set; }
    }

    /// <summary>
    /// Enumeration of possible outcomes of the code execution.
    /// </summary>
    internal enum VendorGoogleChatRequestMessagePartCodeExecutionResultOutcome
    {
        /// <summary>
        /// Unspecified status. This value should not be used.
        /// </summary>
        [EnumMember(Value = "OUTCOME_UNSPECIFIED")]
        Unspecified,
        
        /// <summary>
        /// Code execution completed successfully.
        /// </summary>
        [EnumMember(Value = "OUTCOME_OK")]
        Ok,
        
        /// <summary>
        /// Code execution finished but with a failure. stderr should contain the reason.
        /// </summary>
        [EnumMember(Value = "OUTCOME_FAILED")]
        Failed,
        
        /// <summary>
        /// Code execution ran for too long, and was cancelled. There may or may not be a partial output present.
        /// </summary>
        [EnumMember(Value = "OUTCOME_DEADLINE_EXCEEDED")]
        DeadlineExceeded
    }

    /// <summary>
    /// Code generated by the model that is meant to be executed.
    /// </summary>
    internal class VendorGoogleChatRequestMessagePartExecutableCode
    {
        /// <summary>
        /// Required. Programming language of the code.
        /// </summary>
        [JsonProperty("language")]
        public VendorGoogleChatRequestMessagePartExecutableCodeLanguage Language { get; set; }

        /// <summary>
        /// Required. The code to be executed.
        /// </summary>
        [JsonProperty("code")]
        public string Code { get; set; }
    }

    /// <summary>
    /// Supported programming languages for the generated code.
    /// </summary>
    internal enum VendorGoogleChatRequestMessagePartExecutableCodeLanguage
    {
        /// <summary>
        /// Unspecified language. This value should not be used.
        /// </summary>
        [EnumMember(Value = "LANGUAGE_UNSPECIFIED")]
        Unspecified,
        
        /// <summary>
        /// Python >= 3.10, with numpy and simpy available.
        /// </summary>
        [EnumMember(Value = "PYTHON")]
        Python
    }

    internal class VendorGoogleChatRequestMessagePartFileData
    {
        [JsonProperty("mimeType")]
        public string? MimeType { get; set; }
        
        [JsonProperty("fileUri")]
        public string FileUri { get; set; }
    }
    
    internal class VendorGoogleChatRequestMessage
    {
        [JsonProperty("parts")]
        public List<VendorGoogleChatRequestMessagePart>? Parts { get; set; } = [];
        
        /// <summary>
        /// The producer of the content. Must be either 'user' or 'model'.
        /// </summary>
        [JsonProperty("role")]
        public string? Role { get; set; }

        public VendorGoogleChatRequestMessage()
        {
            
        }

        public VendorGoogleChatRequestMessage(ChatMessage msg)
        {
            if (msg is { Role: ChatMessageRoles.Assistant, ToolCalls.Count: > 0 })
            {
                foreach (ToolCall call in msg.ToolCalls)
                {
                    Parts.Add(new VendorGoogleChatRequestMessagePart
                    {
                        FunctionCall = new VendorGoogleChatRequestMessagePartFunctionCall
                        {
                            Name = call.FunctionCall?.Name ?? call.Id ?? string.Empty,
                            Args = call.FunctionCall?.GetArguments() ?? []
                        }
                    });
                }

                Role = "model";
            }
            else if (msg.Role is ChatMessageRoles.Tool)
            {
                Parts.Add(new VendorGoogleChatRequestMessagePart
                {
                    FunctionResponse = new VendorGoogleChatRequestMessagePartFunctionResponse
                    {
                        Name = msg.ToolCallId ?? string.Empty,
                        Response = new // has to be a JSON schema compliant object, so we just wrap the result in one 
                        {
                            name = msg.ToolCallId ?? string.Empty,
                            content = msg.Content?.JsonDecode<object>() ?? new { }
                        }
                    }
                });

                Role = "user";
            }
            else
            {
                if (msg.Parts?.Count > 0)
                {
                    foreach (ChatMessagePart x in msg.Parts)
                    {
                        Parts.Add(new VendorGoogleChatRequestMessagePart(x));
                    }
                }
                else if (msg.Content is not null)
                {
                    Parts.Add(new VendorGoogleChatRequestMessagePart
                    {
                        Text = msg.Content
                    });
                }

                Role = msg.Role is ChatMessageRoles.User ? "user" : "model";
            }
        }

        public ChatMessage ToChatMessage(VendorGoogleChatRequest? request, ChatRequest? chatRequest, VendorGoogleChatResult.VendorGoogleChatResultMessage nativeResult)
        {
            ChatMessage msg = new ChatMessage
            {
                Parts = []
            };

            StringBuilder sb = new StringBuilder();
            bool roleSolved = false;
            bool contentSolved = false;
            
            foreach (VendorGoogleChatRequestMessagePart x in Parts)
            {
                if (x.FunctionCall is not null)
                {
                    msg.ToolCalls ??= [];
                    msg.ToolCalls.Add(x.ToToolCall());
                }
                else
                {
                    if (request?.GenerationConfig?.ResponseMimeType is "application/json")
                    {
                        string? fnName = chatRequest?.ResponseFormat?.Schema?.Name ?? request.ToolConfig?.FunctionConfig?.AllowedFunctionNames?.FirstOrDefault();

                        if (fnName is null)
                        {
                            Tool? firstTool = chatRequest?.Tools?.FirstOrDefault();

                            if (firstTool is not null)
                            {
                                fnName = firstTool.ToolName;
                                fnName ??= firstTool.Function?.Name;
                            }
                        }
                        
                        msg.ToolCalls ??= [];
                        msg.ToolCalls.Add(new ToolCall
                        {
                            Id = fnName ?? string.Empty,
                            FunctionCall = new FunctionCall
                            {
                                Name = fnName ?? string.Empty,
                                Arguments = x.Text ?? string.Empty
                            }
                        });

                        contentSolved = true;
                    }
                    else
                    {
                        msg.Parts.Add(x.ToMessagePart(sb));      
                    }
                }
            }

            if (!contentSolved)
            {
                msg.Content = sb.ToString();   
            }

            if (nativeResult.GroundingMetadata?.GroundingSupports.Count > 0)
            {
                ChatMessagePart? lastPart = msg.Parts.LastOrDefault(x => x.Type is ChatMessageTypes.Text) ?? msg.Parts.LastOrDefault();
 
                if (lastPart is not null)
                {
                    lastPart.Citations ??= [];

                    foreach (VendorGoogleChatResultGroundingSupport nativeCitation in nativeResult.GroundingMetadata.GroundingSupports)
                    {
                        List<VendorGoogleChatResultGroundingChunk> matchingSources = nativeResult.GroundingMetadata.GroundingChunks.Where((x, i) => nativeCitation.GroundingChunkIndices.Contains(i)).ToList();
                        
                        lastPart.Citations.Add(new ChatMessagePartCitationWebGrounding
                        {
                            Sources = matchingSources.Select(x => new ChatMessagePartCitationWebGroundingSource
                            {
                                Url = x.Web.Uri,
                                Title = x.Web.Title
                            }).ToList(),
                            CitedText = nativeCitation.Segment.Text,
                            NativeObject = nativeCitation
                        });
                    }
                }
            }
            
            msg.Role = Role is "user" ? ChatMessageRoles.User : ChatMessageRoles.Assistant;
            return msg;
        }
    }
    
    internal class VendorGoogleChatRequestSafetySetting
    {
        /// <summary>
        /// HARM_CATEGORY_UNSPECIFIED, HARM_CATEGORY_DEROGATORY, HARM_CATEGORY_TOXICITY, HARM_CATEGORY_VIOLENCE,
        /// HARM_CATEGORY_SEXUAL, HARM_CATEGORY_MEDICAL, HARM_CATEGORY_DANGEROUS, HARM_CATEGORY_HARASSMENT,
        /// HARM_CATEGORY_HATE_SPEECH, HARM_CATEGORY_SEXUALLY_EXPLICIT, HARM_CATEGORY_DANGEROUS_CONTENT
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; }
        
        /// <summary>
        /// HARM_BLOCK_THRESHOLD_UNSPECIFIED, BLOCK_LOW_AND_ABOVE, BLOCK_MEDIUM_AND_ABOVE, BLOCK_ONLY_HIGH, BLOCK_NONE
        /// </summary>
        [JsonProperty("threshold")]
        public string Threshold { get; set; }

        internal VendorGoogleChatRequestSafetySetting()
        {
            
        }

        internal VendorGoogleChatRequestSafetySetting(string category, string threshold)
        {
            Category = category;
            Threshold = threshold;
        }
        
        /// <summary>
        /// Commented values are listed in docs but including them in the request results in 400 response
        /// </summary>
        internal static readonly List<string> AllCategories =
        [
            /*"HARM_CATEGORY_DEROGATORY",*/ /*"HARM_CATEGORY_TOXICITY", "HARM_CATEGORY_VIOLENCE",*/
            /*"HARM_CATEGORY_SEXUAL", "HARM_CATEGORY_MEDICAL",*/ /*"HARM_CATEGORY_DANGEROUS",*/ "HARM_CATEGORY_HARASSMENT",
            "HARM_CATEGORY_HATE_SPEECH", "HARM_CATEGORY_SEXUALLY_EXPLICIT", "HARM_CATEGORY_DANGEROUS_CONTENT"
        ];

        internal static readonly List<VendorGoogleChatRequestSafetySetting> DisableAll = [];

        static VendorGoogleChatRequestSafetySetting()
        {
            foreach (string str in AllCategories)
            {
                DisableAll.Add(new VendorGoogleChatRequestSafetySetting(str, "BLOCK_NONE"));
            }
        }
    }

    internal class VendorGoogleChatToolFunctionDeclaration
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; }
        
        /// <summary>
        /// JSON schema subset: https://ai.google.dev/api/rest/v1beta/Schema
        /// </summary>
        [JsonProperty("parameters")]
        public object? Parameters { get; set; }

        public VendorGoogleChatToolFunctionDeclaration()
        {
            
        }

        public VendorGoogleChatToolFunctionDeclaration(ToolFunction tool)
        {
            Name = tool.Name;
            Description = tool.Description;
            Parameters = tool.Parameters;
        }
    }

    internal class VendorGoogleChatTool
    {
        [JsonProperty("functionDeclarations")] 
        public List<VendorGoogleChatToolFunctionDeclaration> FunctionDeclarations { get; set; } = [];

        [JsonProperty("googleSearchRetrieval")]
        public ChatRequestVendorGoogleSearchRetrieval? GoogleSearchRetrieval { get; set; }
        
        [JsonProperty("codeExecution")]
        public ChatRequestVendorGoogleCodeExecution? CodeExecution { get; set; }
        
        [JsonProperty("googleSearch")]
        public ChatRequestVendorGoogleSearch? GoogleSearch { get; set; }
        
        [JsonProperty("urlContext")]
        public ChatRequestVendorGoogleUrlContext? UrlContext { get; set; }
        
        public VendorGoogleChatTool()
        {
            
        }

        public VendorGoogleChatTool(Tool tool)
        {
            if (tool.Function is not null)
            {
                FunctionDeclarations.Add(new VendorGoogleChatToolFunctionDeclaration(tool.Function));
            }
        }
    }

    /// <summary>
    /// At least one member needs to be set, otherwise the request crashes if we include this field.
    /// </summary>
    internal class VendorGoogleChatToolConfigFunctionConfig
    {
        /// <summary>
        /// AUTO - Default model behavior, model decides to predict either a function call or a natural language response.
        /// ANY - Model is constrained to always predicting a function call only. If "allowedFunctionNames" are set, the predicted function call will be limited to any one of "allowedFunctionNames", else the predicted function call will be any one of the provided "functionDeclarations".
        /// NONE - Model will not predict any function call. Model behavior is same as when not passing any function declarations.
        /// </summary>
        [JsonProperty("mode")]
        public string? Mode { get; set; } = "AUTO";
        
        /// <summary>
        /// This should only be set when the Mode is ANY. Function names should match [FunctionDeclaration.name]. With mode set to ANY, model will predict a function call from the set of function names provided.
        /// </summary>
        [JsonProperty("allowedFunctionNames")]
        public List<string>? AllowedFunctionNames { get; set; }
    }

    internal class VendorGoogleChatToolConfig
    {
        [JsonProperty("functionCallingConfig")]
        public VendorGoogleChatToolConfigFunctionConfig? FunctionConfig { get; set; }

        public static readonly VendorGoogleChatToolConfig Default = new VendorGoogleChatToolConfig
        {
            FunctionConfig = new VendorGoogleChatToolConfigFunctionConfig
            {
                Mode = "AUTO"
            }
        };
    }
    
    [JsonProperty("contents")] 
    public List<VendorGoogleChatRequestMessage> Contents { get; set; } = [];
    
    [JsonProperty("tools")]
    public List<VendorGoogleChatTool>? Tools { get; set; }
    
    [JsonProperty("toolConfig")]
    public VendorGoogleChatToolConfig? ToolConfig { get; set; }
    
    [JsonProperty("safetySettings")]
    public List<VendorGoogleChatRequestSafetySetting>? SafetySettings { get; set; }
    
    [JsonProperty("generationConfig")]
    public VendorGoogleChatRequestGenerationConfig? GenerationConfig { get; set; }
    
    [JsonProperty("systemInstruction")]
    public VendorGoogleChatRequestMessage? SystemInstruction { get; set; }
    
    [JsonProperty("cachedContent")]
    public string? CachedContent { get; set; }
    
    public VendorGoogleChatRequest()
    {
        
    }

    public enum VendorGoogleRequestToolsResponseMode
    {
        Default,
        StructuredJson,
        Json
    }

    public static Tuple<List<VendorGoogleChatTool>?, VendorGoogleChatToolConfig?, VendorGoogleChatRequestGenerationConfig?> GetToolsAndToolChoice(ChatRequestResponseFormats? responseFormat, List<Tool>? tools, OutboundToolChoice? outboundToolChoice)
    {
        if (tools is null || tools.Count is 0)
        {
            return new Tuple<List<VendorGoogleChatTool>?, VendorGoogleChatToolConfig?, VendorGoogleChatRequestGenerationConfig?>(null, null, null);
        }
        
        List<VendorGoogleChatTool>? localTools = [];
        VendorGoogleChatToolConfig? localToolConfig = null;
        
        VendorGoogleChatRequestGenerationConfig? localConfig = null;
        bool anyStrictTool = false;
        
        foreach (Tool tool in tools)
        {
            localTools.Add(new VendorGoogleChatTool(tool));
        }
        
        if (outboundToolChoice is not null)
        {
            localToolConfig = VendorGoogleChatToolConfig.Default;
            localToolConfig.FunctionConfig ??= new VendorGoogleChatToolConfigFunctionConfig();
            
            switch (outboundToolChoice.Mode)
            {
                case OutboundToolChoiceModes.Auto or OutboundToolChoiceModes.Legacy:
                {
                    localToolConfig.FunctionConfig.Mode = "AUTO";
                    break;
                }
                case OutboundToolChoiceModes.Required:
                {
                    localToolConfig.FunctionConfig.Mode = "ANY";
                    break;
                }
                case OutboundToolChoiceModes.None:
                {
                    localToolConfig.FunctionConfig.Mode = "NONE";
                    break;
                }
                case OutboundToolChoiceModes.ToolFunction:
                {
                    localToolConfig.FunctionConfig.Mode = "ANY";
                    localToolConfig.FunctionConfig.AllowedFunctionNames = [ outboundToolChoice.Function?.Name ?? string.Empty ];

                    Tool? match = tools.FirstOrDefault(x => x.Function?.Name == outboundToolChoice.Function?.Name);

                    if ((match?.Strict ?? false) && match.Function?.Parameters is not null)
                    {
                        bool canUpcast = false;
                        object? schemaObj = match.Function.Parameters;
                        
                        // check if we have compatible data in params
                        if (match.Function.Parameters is JObject jObject)
                        {
                            JToken? type = jObject["type"];
                            
                            if (type?.Type is JTokenType.String)
                            {
                                canUpcast = true;
                            }
                            else
                            {
                                JToken? pars = jObject["parameters"];

                                if (pars?.Type is JTokenType.Object)
                                {
                                    canUpcast = true;
                                    schemaObj = pars;
                                }
                            }
                        }
                        else
                        {
                            canUpcast = true;
                        }

                        if (canUpcast)
                        {
                            localConfig = new VendorGoogleChatRequestGenerationConfig
                            {
                                ResponseMimeType = "application/json",
                                ResponseSchema = schemaObj
                            };

                            // if we force strict json mode, these two fields must be cleared, otherwise the api throws due to these having precedence over responseMimeType
                            localTools = null;
                            localToolConfig = null;    
                        }
                    }
                    
                    break;
                }
            }
        }

        return new Tuple<List<VendorGoogleChatTool>?, VendorGoogleChatToolConfig?, VendorGoogleChatRequestGenerationConfig?>(localTools, localToolConfig, localConfig);
    }
    
    public VendorGoogleChatRequest(ChatRequest request, IEndpointProvider provider)
    {
        request.OverrideUrl($"{provider.ApiUrl(CapabilityEndpoints.Chat, null)}/{request.Model?.Name}:{(request.StreamResolved ? "streamGenerateContent" : "generateContent")}");
        
        IList<ChatMessage>? msgs = request.Messages;

        if (msgs is not null)
        {
            ChatMessage? sysMsg = msgs.FirstOrDefault(x => x.Role is ChatMessageRoles.System);

            if (sysMsg is not null)
            {
                if (request.Model is not null && ChatModelGoogle.ModelsWithDisabledDeveloperMessage.Contains(request.Model))
                {
                    // system prompt is unsupported
                }
                else
                {
                    SystemInstruction = new VendorGoogleChatRequestMessage(sysMsg);   
                }
            }
            
            foreach (ChatMessage msg in msgs)
            {
                if (msg.Role is ChatMessageRoles.System)
                {
                    continue;
                }
                
                Contents.Add(new VendorGoogleChatRequestMessage(msg));
            }
        }

        GenerationConfig = new VendorGoogleChatRequestGenerationConfig
        {
            Temperature = request.Temperature is null ? null : (request.Temperature ?? 0).Clamp(0, 2),
            TopP = request.TopP,
            MaxOutputTokens = request.MaxTokens,
            StopSequences = request.MultipleStopSequences is not null ? request.MultipleStopSequences.Take(5).ToList() : request.StopSequence is not null ? [ request.StopSequence ] : null,
            ResponseLogprobs = request.Logprobs,
            Logprobs = request.TopLogprobs
        };

        // thinkingConfig is not supported for non-thinking models
        if (request.Model is not null && ChatModelGoogle.ReasoningModels.Contains(request.Model))
        {
            int? clamped = request.Model.ClampReasoningTokens(request.ReasoningBudget);
            
            GenerationConfig.ThinkingConfig = new VendorGoogleChatRequestThinkingConfig
            {
                ThinkingBudget = clamped,
                IncludeThoughts = clamped is not 0 && (request.VendorExtensions?.Google?.IncludeThoughts ?? true)
            };
        } 

        if (request.Modalities?.Count > 0 && request.Model is not null)
        {
            if (request.Modalities.Contains(ChatModelModalities.Image) && ChatModelGoogle.ImageModalitySupportingModels.Contains(request.Model))
            {
                GenerationConfig.ResponseModalities ??= [];
                GenerationConfig.ResponseModalities.Add("IMAGE");
            }

            if (request.Modalities.Contains(ChatModelModalities.Text))
            {
                GenerationConfig.ResponseModalities ??= [];
                GenerationConfig.ResponseModalities.Add("TEXT");
            }
            
            if (request.Modalities.Contains(ChatModelModalities.Audio))
            {
                GenerationConfig.ResponseModalities ??= [];
                GenerationConfig.ResponseModalities.Add("AUDIO");
            }
        }
        
        if (request.Tools?.Count > 0)
        {
            Tuple<List<VendorGoogleChatTool>?, VendorGoogleChatToolConfig?, VendorGoogleChatRequestGenerationConfig?> configUpdate = GetToolsAndToolChoice(request.ResponseFormat, request.Tools, request.ToolChoice);

            Tools = configUpdate.Item1;
            ToolConfig = configUpdate.Item2;

            if (configUpdate.Item3 is not null)
            {
                GenerationConfig.ResponseMimeType = configUpdate.Item3.ResponseMimeType;
                GenerationConfig.ResponseSchema = configUpdate.Item3.ResponseSchema;
            }
        }
        else
        {
            if (request.ResponseFormat?.Type is ChatRequestResponseFormatTypes.StructuredJson or ChatRequestResponseFormatTypes.Json)
            {
                string fnName = request.ResponseFormat.Schema?.Name ?? string.Empty;
                
                Tuple<List<VendorGoogleChatTool>?, VendorGoogleChatToolConfig?, VendorGoogleChatRequestGenerationConfig?> configUpdate = GetToolsAndToolChoice(request.ResponseFormat, request.ResponseFormat.Type is ChatRequestResponseFormatTypes.Json ? null : [
                    new Tool(new ToolFunction(fnName, string.Empty, request.ResponseFormat.Schema?.Schema ?? new
                    {
                        
                    }), request.ResponseFormat.Type is ChatRequestResponseFormatTypes.StructuredJson)
                ], request.ToolChoice ?? (request.ResponseFormat.Type is ChatRequestResponseFormatTypes.StructuredJson ? new OutboundToolChoice(OutboundToolChoiceModes.ToolFunction)
                {
                    Function = new OutboundToolCallFunction
                    {
                        Name = fnName
                    }
                } : null));
                
                Tools = configUpdate.Item1;
                ToolConfig = null; // configUpdate.Item2;

                if (configUpdate.Item3 is not null)
                {
                    GenerationConfig.ResponseMimeType = configUpdate.Item3.ResponseMimeType;
                    GenerationConfig.ResponseSchema = configUpdate.Item3.ResponseSchema;
                }
            }
        }

        if (request.VendorExtensions?.Google is not null)
        {
            VendorGoogleChatTool? builtInTool = null;
            
            if (request.VendorExtensions.Google.CodeExecution is not null)
            {
                builtInTool ??= new VendorGoogleChatTool();
                builtInTool.CodeExecution = request.VendorExtensions.Google.CodeExecution;
            }
            
            if (request.VendorExtensions.Google.GoogleSearchRetrieval is not null)
            {
                builtInTool ??= new VendorGoogleChatTool();
                builtInTool.GoogleSearchRetrieval = request.VendorExtensions.Google.GoogleSearchRetrieval;
            }
            
            if (request.VendorExtensions.Google.GoogleSearch is not null)
            {
                builtInTool ??= new VendorGoogleChatTool();
                builtInTool.GoogleSearch = request.VendorExtensions.Google.GoogleSearch;
            }
            
            if (request.VendorExtensions.Google.UrlContext is not null)
            {
                builtInTool ??= new VendorGoogleChatTool();
                builtInTool.UrlContext = request.VendorExtensions.Google.UrlContext;
            }

            if (builtInTool is not null)
            {
                Tools ??= [];
                Tools.Add(builtInTool);
            }
            
            if (request.VendorExtensions.Google.CachedContent is not null)
            {
                CachedContent = request.VendorExtensions.Google.CachedContent;
            }

            if (request.VendorExtensions.Google.ResponseSchema?.Function is not null)
            {
                GenerationConfig.ResponseMimeType = "application/json";
                GenerationConfig.ResponseSchema = request.VendorExtensions.Google.ResponseSchema.Function.Parameters;
            }

            if (request.VendorExtensions.Google.SpeechConfig is not null)
            {
                GenerationConfig.SpeechConfig = new VendorGoogleChatRequestSpeechConfig
                {
                    LanguageCode = request.VendorExtensions.Google.SpeechConfig.LanguageCode,
                    VoiceConfig = request.VendorExtensions.Google.SpeechConfig.VoiceName is not null
                        ? new VendorGoogleChatRequestVoiceConfig
                        {
                            PrebuiltVoiceConfig = new VendorGoogleChatRequestPrebuiltVoiceConfig
                            {
                                VoiceName = request.VendorExtensions.Google.SpeechConfig.VoiceName.VoiceName?.ToString().ToLowerInvariant()
                            }
                        }
                        : null,
                    MultiSpeakerVoiceConfig = request.VendorExtensions.Google.SpeechConfig.MultiSpeaker is not null
                        ? new VendorGoogleChatRequestMultiSpeakerVoiceConfig
                        {
                            SpeakerVoiceConfigs = request.VendorExtensions.Google.SpeechConfig.MultiSpeaker.Speakers.Select(x => new VendorGoogleChatRequestMultiSpeakerVoiceConfigSpeaker
                            {
                                Speaker = x.Speaker,
                                PrebuiltVoiceConfig = x.Voice is null ? null : new VendorGoogleChatRequestPrebuiltVoiceConfigWrapper
                                {
                                    VoiceConfig = new VendorGoogleChatRequestPrebuiltVoiceConfig
                                    {
                                        VoiceName = x.Voice.VoiceName?.ToString().ToLowerInvariant()   
                                    }
                                }
                            }).ToList()
                        }
                        : null
                };
            }

            if (request.VendorExtensions.Google.SafetyFilters is not null)
            {
                if (request.VendorExtensions.Google.SafetyFilters == ChatRequestVendorGoogleSafetyFilters.Default)
                {
                    
                }
                else if (request.VendorExtensions.Google.SafetyFilters == ChatRequestVendorGoogleSafetyFilters.Minimal)
                {
                    SafetySettings = VendorGoogleChatRequestSafetySetting.DisableAll;   
                }
                else
                {
                    SafetySettings = [];

                    if (request.VendorExtensions.Google.SafetyFilters.SexuallyExplicit is not null)
                    {
                        SafetySettings.Add(new VendorGoogleChatRequestSafetySetting("HARM_CATEGORY_SEXUALLY_EXPLICIT", HarmFilter(request.VendorExtensions.Google.SafetyFilters.SexuallyExplicit)));
                    }
                    
                    if (request.VendorExtensions.Google.SafetyFilters.Harassment is not null)
                    {
                        SafetySettings.Add(new VendorGoogleChatRequestSafetySetting("HARM_CATEGORY_HARASSMENT", HarmFilter(request.VendorExtensions.Google.SafetyFilters.Harassment)));
                    }
                    
                    if (request.VendorExtensions.Google.SafetyFilters.DangerousContent is not null)
                    {
                        SafetySettings.Add(new VendorGoogleChatRequestSafetySetting("HARM_CATEGORY_DANGEROUS_CONTENT", HarmFilter(request.VendorExtensions.Google.SafetyFilters.DangerousContent)));
                    }
                    
                    if (request.VendorExtensions.Google.SafetyFilters.HateSpeech is not null)
                    {
                        SafetySettings.Add(new VendorGoogleChatRequestSafetySetting("HARM_CATEGORY_HATE_SPEECH", HarmFilter(request.VendorExtensions.Google.SafetyFilters.HateSpeech)));
                    }
                }
            }
            else
            {
                SafetySettings = VendorGoogleChatRequestSafetySetting.DisableAll;   
            }
        }
        else
        {
            SafetySettings = VendorGoogleChatRequestSafetySetting.DisableAll;   
        }
    }

    static string HarmFilter(GoogleSafetyFilterTypes? val)
    {
        return val switch
        {
            GoogleSafetyFilterTypes.BlockNone => "BLOCK_NONE",
            GoogleSafetyFilterTypes.BlockFew => "BLOCK_ONLY_HIGH",
            GoogleSafetyFilterTypes.BlockSome => "BLOCK_MEDIUM_AND_ABOVE",
            GoogleSafetyFilterTypes.BlockMost => "BLOCK_LOW_AND_ABOVE",
            GoogleSafetyFilterTypes.Default => "HARM_BLOCK_THRESHOLD_UNSPECIFIED",
            _ => "BLOCK_NONE"
        };
    }
 }