using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Chat.Vendors.Mistral;

internal class MistralChatMessageConverter : JsonConverter<ChatMessage>
{
    public override ChatMessage? ReadJson(JsonReader reader, Type objectType, ChatMessage? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JObject obj = JObject.Load(reader);

        ChatMessage msg = new ChatMessage
        {
            Role = ParseRole(obj["role"]?.ToString())
        };

        // tool calls
        JToken? toolCallsToken = obj["tool_calls"];
        msg.ToolCalls = ParseToolCalls(toolCallsToken as JArray);

        // content can be string or array of blocks
        JToken? contentToken = obj["content"];
        if (contentToken is JArray contentArr)
        {
            StringBuilder reasoningBuilder = new StringBuilder();
            StringBuilder textBuilder = new StringBuilder();
            List<ChatMessagePart> parts = [];

            foreach (JToken block in contentArr)
            {
                string? type = block["type"]?.ToString();
                switch (type)
                {
                    case "thinking":
                        if (block["thinking"] is JArray thinkingArr)
                        {
                            foreach (JToken t in thinkingArr)
                            {
                                string? txt = t["text"]?.ToString();
                                if (!string.IsNullOrEmpty(txt))
                                {
                                    if (reasoningBuilder.Length > 0) reasoningBuilder.AppendLine().AppendLine();
                                    reasoningBuilder.Append(txt);
                                    parts.Add(new ChatMessagePart
                                    {
                                        Type = ChatMessageTypes.Reasoning,
                                        Reasoning = new ChatMessageReasoningData { Content = txt }
                                    });
                                }
                            }
                        }
                        break;
                    case "text":
                    {
                        string? txt = block["text"]?.ToString();
                        if (!string.IsNullOrEmpty(txt))
                        {
                            AppendText(textBuilder, parts, txt);
                        }
                        break;
                    }
                    case "image_url":
                    {
                        string? url = ExtractUrl(block["image_url"]);
                        if (!string.IsNullOrEmpty(url))
                        {
                            parts.Add(new ChatMessagePart(new ChatImage(url)));
                        }
                        break;
                    }
                    case "document_url":
                    {
                        string? docUrl = block["document_url"]?.ToString();
                        if (!string.IsNullOrEmpty(docUrl))
                        {
                            parts.Add(new ChatMessagePart(new ChatMessagePartFileLinkData(docUrl)));
                        }
                        break;
                    }
                    case "reference":
                    {
                        if (block["reference_ids"] is JArray refs && refs.Count > 0)
                        {
                            msg.References = refs.Select(x => (int?)x).Where(x => x is not null).Select(x => x!.Value).ToList();
                        }
                        break;
                    }
                    case "file":
                    {
                        string? fileId = block["file_id"]?.ToString();
                        if (!string.IsNullOrEmpty(fileId))
                        {
                            parts.Add(new ChatMessagePart(new ChatMessagePartFileLinkData(fileId)));
                        }
                        break;
                    }
                    case "input_audio":
                    {
                        string? audio = block["input_audio"]?.ToString();
                        if (!string.IsNullOrEmpty(audio))
                        {
                            ChatAudio chatAudio = new ChatAudio
                            {
                                Data = audio
                            };
                            parts.Add(new ChatMessagePart
                            {
                                Type = ChatMessageTypes.Audio,
                                Audio = chatAudio
                            });
                        }
                        break;
                    }
                    default:
                    {
                        // Unknown chunk: keep JSON for visibility
                        parts.Add(new ChatMessagePart(block.ToString(Formatting.None)));
                        break;
                    }
                }
            }

            msg.ReasoningContent = reasoningBuilder.Length > 0 ? reasoningBuilder.ToString() : null;
            msg.Content = textBuilder.Length > 0 ? textBuilder.ToString() : null;
            if (parts.Count > 0)
            {
                msg.Parts = parts;
            }
        }
        else if (contentToken is JValue val && val.Type != JTokenType.Null)
        {
            msg.Content = val.ToString(CultureInfo.InvariantCulture);
        }

        return msg;
    }

    public override void WriteJson(JsonWriter writer, ChatMessage? value, JsonSerializer serializer)
    {
        // Use default serialization for outbound; not needed here.
        serializer.Serialize(writer, value);
    }

    public override bool CanWrite => true;

    private static ChatMessageRoles ParseRole(string? role)
    {
        return role?.ToLowerInvariant() switch
        {
            "system" => ChatMessageRoles.System,
            "assistant" => ChatMessageRoles.Assistant,
            "user" => ChatMessageRoles.User,
            "tool" => ChatMessageRoles.Tool,
            _ => ChatMessageRoles.Unknown
        };
    }

    private static List<ToolCall>? ParseToolCalls(JArray? toolCallsArray)
    {
        if (toolCallsArray is null || toolCallsArray.Count == 0)
        {
            return null;
        }

        List<ToolCall> toolCalls = [];

        foreach (JToken tc in toolCallsArray)
        {
            ToolCall toolCall = new ToolCall
            {
                Id = tc["id"]?.ToString(),
                Type = tc["type"]?.ToString(),
                FunctionCall = tc["function_call"] is JObject fn
                    ? new FunctionCall
                    {
                        Name = fn["name"]?.ToString(),
                        Arguments = SerializeArguments(fn["arguments"])
                    }
                    : null
            };

            if (toolCall.FunctionCall is not null)
            {
                toolCall.FunctionCall.ToolCall = toolCall;
            }

            toolCalls.Add(toolCall);
        }

        return toolCalls;
    }

    private static string? SerializeArguments(JToken? args)
    {
        if (args is null) return null;
        return args.Type switch
        {
            JTokenType.Object or JTokenType.Array => args.ToString(Formatting.None),
            _ => args.ToString()
        };
    }

    private static string? ExtractUrl(JToken? token)
    {
        if (token is null) return null;
        if (token.Type == JTokenType.String) return token.ToString();
        if (token is JObject obj) return obj["url"]?.ToString();
        return token.ToString();
    }

    private static void AppendText(StringBuilder textBuilder, List<ChatMessagePart> parts, string? txt)
    {
        if (string.IsNullOrEmpty(txt)) return;
        if (textBuilder.Length > 0) textBuilder.AppendLine().AppendLine();
        textBuilder.Append(txt);
        parts.Add(new ChatMessagePart(txt));
    }
}
