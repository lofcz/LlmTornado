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
        public string MimeType { get; set; }
        
        [JsonProperty("fileUri")]
        public string FileUri { get; set; }
    }
    
    internal class VendorGoogleChatRequestMessagePart
    {
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string? Text { get; set; }
        
        [JsonProperty("inlineData", NullValueHandling = NullValueHandling.Ignore)]
        public VendorGoogleChatRequestMessagePartInlineData? InlineData { get; set; }
        
        [JsonProperty("functionCall", NullValueHandling = NullValueHandling.Ignore)]
        public VendorGoogleChatRequestMessagePartFunctionCall? FunctionCall { get; set; }
        
        [JsonProperty("functionResponse", NullValueHandling = NullValueHandling.Ignore)]
        public VendorGoogleChatRequestMessagePartFunctionResponse? FunctionResponse { get; set; }

        [JsonProperty("fileData", NullValueHandling = NullValueHandling.Ignore)]
        public VendorGoogleChatRequestMessagePartFileData? FileData { get; set; }
        
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
                        
                        InlineData = new VendorGoogleChatRequestMessagePartInlineData
                        {
                            MimeType = part.Image.MimeType,
                            Data = part.Image.Url
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

        public ChatMessage ToChatMessage()
        {
            ChatMessage msg = new ChatMessage
            {
                Parts = []
            };

            StringBuilder sb = new StringBuilder();
            
            foreach (VendorGoogleChatRequestMessagePart x in Parts)
            {
                if (x.FunctionCall is not null)
                {
                    msg.ToolCalls ??= [];
                    msg.ToolCalls.Add(x.ToToolCall());
                }
                else
                {
                    msg.Parts.Add(x.ToMessagePart(sb));   
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

    internal class VendorGoogleChatRequestGenerationConfig
    {
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
        
        [JsonProperty("candidateCount")]
        public int? CandidateCount { get; set; }
        
        [JsonProperty("maxOutputTokens")]
        public int? MaxOutputTokens { get; set; }
        
        /// <summary>
        /// Values can range from [0.0, 2.0].
        /// </summary>
        [JsonProperty("temperature")]
        public double? Temperature { get; set; }
        
        [JsonProperty("topP")]
        public double? TopP { get; set; }
        
        [JsonProperty("topK")]
        public int? TopK { get; set; }
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
    
    public VendorGoogleChatRequest()
    {
        
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

        if (request.Tools?.Count > 0)
        {
            Tools ??= [];

            foreach (Tool tool in request.Tools)
            {
                Tools.Add(new VendorGoogleChatTool(tool));
            }

            ToolConfig = new VendorGoogleChatToolConfig
            {
                FunctionConfig = new VendorGoogleChatToolConfigFunctionConfig
                {
                    Mode = "AUTO"
                }
            };

            if (request.ToolChoice is not null)
            {
                switch (request.ToolChoice.Mode)
                {
                    case OutboundToolChoiceModes.Auto or OutboundToolChoiceModes.Legacy:
                    {
                        ToolConfig.FunctionConfig.Mode = "AUTO";
                        break;
                    }
                    case OutboundToolChoiceModes.Required:
                    {
                        ToolConfig.FunctionConfig.Mode = "ANY";
                        break;
                    }
                    case OutboundToolChoiceModes.None:
                    {
                        ToolConfig.FunctionConfig.Mode = "NONE";
                        break;
                    }
                    case OutboundToolChoiceModes.ToolFunction:
                    {
                        ToolConfig.FunctionConfig.Mode = "ANY";
                        ToolConfig.FunctionConfig.AllowedFunctionNames = [ request.ToolChoice.Function?.Name ?? string.Empty ];
                        break;
                    }
                }
            }
        }
        
        SafetySettings = VendorGoogleChatRequestSafetySetting.DisableAll;
    }
 }