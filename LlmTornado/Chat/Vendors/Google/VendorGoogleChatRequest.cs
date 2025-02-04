using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Plugins;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Chat.Vendors.Cohere;

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
}

internal class VendorGoogleChatRequestMessagePart
{
    [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
    public string? Text { get; set; }
    
    [JsonProperty("inlineData", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartInlineData? InlineData { get; set; }
    
    [JsonProperty("functionCall", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartFunctionCall? FunctionCall { get; set; }
    
    [JsonProperty("functionResponse", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartFunctionResponse? FunctionResponse { get; set; }

    [JsonProperty("fileData", NullValueHandling = NullValueHandling.Ignore)]
    public VendorGoogleChatRequest.VendorGoogleChatRequestMessagePartFileData? FileData { get; set; }
    
    // todo: map executableCode, codeExecutionResult; https://ai.google.dev/api/caching#Part
    
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
        }
    }

    public ChatMessagePart ToMessagePart(StringBuilder sb)
    {
        ChatMessagePart part = new ChatMessagePart();
        
        if (Text is not null)
        {
            part.Type = ChatMessageTypes.Text;
            part.Text = Text;
            sb.Append(Text);
        }
        else if (InlineData is not null)
        {
            part.Type = ChatMessageTypes.Image;
            part.Image = new ChatImage(InlineData.Data);
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
        public List<VendorGoogleChatRequestMessagePart> Parts { get; set; } = [];
        
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
                            Name = call.FunctionCall.Name,
                            Args = call.FunctionCall.GetArguments()
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

        public ChatMessage ToChatMessage(VendorGoogleChatRequest? request)
        {
            ChatMessage msg = new ChatMessage
            {
                Parts = []
            };

            StringBuilder sb = new StringBuilder();
            bool roleSolved = false;
            
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
                        string? fnName = request.ToolConfig?.FunctionConfig?.AllowedFunctionNames?.FirstOrDefault();
                        
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
                    }
                    else
                    {
                        msg.Parts.Add(x.ToMessagePart(sb));      
                    }
                }
            }

            msg.Content = sb.ToString();
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
        [JsonProperty("function_declarations")] 
        public List<VendorGoogleChatToolFunctionDeclaration> FunctionDeclarations { get; set; } = [];

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

    public static Tuple<List<VendorGoogleChatTool>?, VendorGoogleChatToolConfig?, VendorGoogleChatRequestGenerationConfig?> GetToolsAndToolChoice(List<Tool>? tools, OutboundToolChoice? outboundToolChoice)
    {
        if (tools is null || tools.Count is 0)
        {
            return new Tuple<List<VendorGoogleChatTool>?, VendorGoogleChatToolConfig?, VendorGoogleChatRequestGenerationConfig?>(null, null, null);
        }
        
        List<VendorGoogleChatTool>? localTools = [];
        VendorGoogleChatToolConfig localToolConfig = new VendorGoogleChatToolConfig
        {
            FunctionConfig = new VendorGoogleChatToolConfigFunctionConfig
            {
                Mode = "AUTO"
            }
        };
        VendorGoogleChatRequestGenerationConfig? localConfig = null;

        bool anyStrictTool = false;
        
        foreach (Tool tool in tools)
        {
            localTools.Add(new VendorGoogleChatTool(tool));
        }

        if (outboundToolChoice is not null)
        {
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
                        localConfig = new VendorGoogleChatRequestGenerationConfig
                        {
                            ResponseMimeType = "application/json",
                            ResponseSchema = match.Function.Parameters
                        };

                        // if we force strict json mode, these two fields must be cleared, otherwise the api throws due to these having precedence over responseMimeType
                        localTools = null;
                        // ToolConfig = null; // we keep this in the request as they accept it and we can match the function name later with it
                    }
                    
                    break;
                }
            }
        }

        return new Tuple<List<VendorGoogleChatTool>?, VendorGoogleChatToolConfig?, VendorGoogleChatRequestGenerationConfig?>(localTools, localToolConfig, localConfig);
    }
    
    public VendorGoogleChatRequest(ChatRequest request, IEndpointProvider provider)
    {
        request.OverrideUrl($"{provider.ApiUrl(CapabilityEndpoints.Chat, null)}/{request.Model?.Name}:{(request.Stream ? "streamGenerateContent" : "generateContent")}");
        
        IList<ChatMessage>? msgs = request.Messages;

        if (msgs is not null)
        {
            ChatMessage? sysMsg = msgs.FirstOrDefault(x => x.Role is ChatMessageRoles.System);

            if (sysMsg is not null)
            {
                SystemInstruction = new VendorGoogleChatRequestMessage(sysMsg);
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
            Temperature = request.Temperature is null ? null : Math.Clamp((double)request.Temperature, 0, 2),
            TopP = request.TopP,
            MaxOutputTokens = request.MaxTokens,
            StopSequences = request.MultipleStopSequences is not null ? request.MultipleStopSequences.Take(5).ToList() : request.StopSequence is not null ? [ request.StopSequence ] : null
        };
        
        if (request.Tools?.Count > 0)
        {
            Tuple<List<VendorGoogleChatTool>?, VendorGoogleChatToolConfig?, VendorGoogleChatRequestGenerationConfig?> configUpdate = GetToolsAndToolChoice(request.Tools, request.ToolChoice);

            Tools = configUpdate.Item1;
            ToolConfig = configUpdate.Item2;

            if (configUpdate.Item3 is not null)
            {
                GenerationConfig.ResponseMimeType = configUpdate.Item3.ResponseMimeType;
                GenerationConfig.ResponseSchema = configUpdate.Item3.ResponseSchema;
            }
        }

        if (request.VendorExtensions?.Google is not null)
        {
            if (request.VendorExtensions.Google.CachedContent is not null)
            {
                CachedContent = request.VendorExtensions.Google.CachedContent;
            }

            if (request.VendorExtensions.Google.ResponseSchema is not null)
            {
                GenerationConfig.ResponseMimeType = "application/json";
                GenerationConfig.ResponseSchema = request.VendorExtensions.Google.ResponseSchema;
            }
        }
        
        SafetySettings = VendorGoogleChatRequestSafetySetting.DisableAll;
    }
 }