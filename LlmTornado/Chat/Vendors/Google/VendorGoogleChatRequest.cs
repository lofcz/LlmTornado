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
    
    internal class VendorGoogleChatRequestMessagePart
    {
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string? Text { get; set; }
        
        [JsonProperty("inlineData", NullValueHandling = NullValueHandling.Ignore)]
        public VendorGoogleChatRequestMessagePartInlineData? InlineData { get; set; }

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
                        InlineData = new VendorGoogleChatRequestMessagePartInlineData
                        {
                            MimeType = "image/png", // todo
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
                msg.Parts.Add(x.ToMessagePart(sb));
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
    
    // todo: tools, toolConfig
    
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
        request.OverrideUrl($"{provider.ApiUrl(CapabilityEndpoints.Chat, null)}/{request.Model?.Name}:generateContent");
        
        IList<ChatMessage>? msgs = request.Messages;

        if (msgs is not null)
        {
            ChatMessage? sysMsg = msgs.FirstOrDefault(x => x.Role is ChatMessageRoles.System);

            if (sysMsg is not null)
            {
                SystemInstruction = new VendorGoogleChatRequestMessage(sysMsg);
            }
            
            foreach (ChatMessage msg in msgs.Where(x => x.Role is not ChatMessageRoles.System))
            {
                Contents.Add(new VendorGoogleChatRequestMessage(msg));
            }
        }
        
        SafetySettings = VendorGoogleChatRequestSafetySetting.DisableAll;
    }
 }