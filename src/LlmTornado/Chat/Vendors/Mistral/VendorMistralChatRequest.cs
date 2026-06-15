using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Chat.Vendors.Mistral;

/// <summary>
/// https://docs.mistral.ai/api/#tag/chat/operation/chat_completion_v1_chat_completions_post
/// </summary>
internal class VendorMistralChatRequest
{
    public VendorMistralChatRequestData ExtendedRequest { get; set; }

    [JsonIgnore]
    public ChatMessage? TempMessage { get; set; }

    [JsonIgnore]
    public ChatRequest SourceRequest { get; set; }

    public JObject Serialize(JsonSerializerSettings settings)
    {
        JsonSerializer serializer = JsonSerializer.CreateDefault(settings);
        JObject jsonPayload = JObject.FromObject(ExtendedRequest, serializer);

        if (TempMessage is not null)
        {
            SourceRequest.Messages?.Remove(TempMessage);
        }

        return jsonPayload;
    }

    internal class VendorMistralChatRequestData : ChatRequest
    {
        [JsonProperty("safe_prompt")]
        public bool? SafePrompt { get; set; }

        [JsonProperty("prediction")]
        public Prediction? Prediction { get; set; }

        [JsonProperty("prompt_mode")]
        public MistralPromptMode? PromptMode { get; set; }

        [JsonProperty("random_seed")]
        public int? RandomSeed { get; set; }

        [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
        public new List<VendorMistralTool>? Tools { get; set; }

        public VendorMistralChatRequestData(ChatRequest request) : base(request)
        {
            // Mistral's strict API rejects properties it doesn't support
            Verbosity = null;

            if (request.Tools is { Count: > 0 })
            {
                List<VendorMistralTool> tools = [];
                foreach (Tool t in request.Tools)
                {
                    if (t.Function is not null)
                    {
                        tools.Add(new VendorMistralTool
                        {
                            Function = new VendorMistralToolFunction
                            {
                                Name = t.Function.Name,
                                Description = t.Function.Description,
                                Parameters = t.Function.Parameters,
                                Strict = t.Strict
                            }
                        });
                    }
                }
                Tools = tools;
                base.Tools = null;
            }
        }
    }

    internal class VendorMistralTool
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "function";

        [JsonProperty("function")]
        public VendorMistralToolFunction? Function { get; set; }
    }

    internal class VendorMistralToolFunction
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }

        [JsonProperty("parameters", NullValueHandling = NullValueHandling.Ignore)]
        public object? Parameters { get; set; }

        [JsonProperty("strict", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Strict { get; set; }
    }

    internal class Prediction
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "content";

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public VendorMistralChatRequest(ChatRequest request, IEndpointProvider provider)
    {
        // not supported
        request.StreamOptions = null;

        SourceRequest = request;
        ChatRequestVendorMistralExtensions? extensions = request.VendorExtensions?.Mistral;

        ExtendedRequest = new VendorMistralChatRequestData(request);

        if (extensions is not null)
        {
            if (extensions.SafePrompt is not null)
            {
                ExtendedRequest.SafePrompt = extensions.SafePrompt;
            }

            if (extensions.PromptMode is not null)
            {
                ExtendedRequest.PromptMode = extensions.PromptMode;
            }

            if (extensions.Prediction is not null)
            {
                ExtendedRequest.Prediction = new Prediction
                {
                    Content = extensions.Prediction
                };
            }

            ExtendedRequest.RandomSeed = extensions.RandomSeed;

            if (extensions.Prefix is not null)
            {
                ChatMessage? lastMessage = request.Messages?.LastOrDefault();

                if (lastMessage?.Role is ChatMessageRoles.User)
                {
                    TempMessage = new ChatMessage(ChatMessageRoles.Assistant, extensions.Prefix)
                    {
                        Prefix = true
                    };
                    request.Messages?.Add(TempMessage);
                }
            }
        }
    }
}