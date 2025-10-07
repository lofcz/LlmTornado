using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using LlmTornado.Chat;
using LlmTornado.Code;
using LlmTornado.ChatFunctions;
using LlmTornado.Common;
using LlmTornado.Infra;
using Microsoft.Extensions.AI;

namespace LlmTornado.Microsoft.Extensions.AI;

/// <summary>
/// Provides conversion methods between Microsoft.Extensions.AI types and LlmTornado types.
/// </summary>
internal static class TypeConverters
{
    /// <summary>
    /// Converts a Microsoft.Extensions.AI ChatMessage to a LlmTornado ChatMessage.
    /// </summary>
    public static LlmTornado.Chat.ChatMessage ToLlmTornado(this global::Microsoft.Extensions.AI.ChatMessage message)
    {
        var role = message.Role.Value switch
        {
            "user" => ChatMessageRoles.User,
            "assistant" => ChatMessageRoles.Assistant,
            "system" => ChatMessageRoles.System,
            "tool" => ChatMessageRoles.Tool,
            _ => ChatMessageRoles.User
        };

        var tornadoMessage = new LlmTornado.Chat.ChatMessage(role);

        // Handle different content types
        if (message.Contents != null && message.Contents.Count > 0)
        {
            var parts = new List<ChatMessagePart>();

            foreach (var content in message.Contents)
            {
                switch (content)
                {
                    case TextContent textContent:
                        if (!string.IsNullOrEmpty(textContent.Text))
                        {
                            parts.Add(new ChatMessagePart(textContent.Text));
                        }
                        break;

                    case ImageContent imageContent:
                        if (!string.IsNullOrEmpty(imageContent.Uri))
                        {
                            parts.Add(new ChatMessagePart(new Uri(imageContent.Uri)));
                        }
                        else if (imageContent.Data.HasValue)
                        {
                            var base64 = Convert.ToBase64String(imageContent.Data.Value.ToArray());
                            var dataUrl = $"data:{imageContent.MediaType ?? "image/png"};base64,{base64}";
                            parts.Add(new ChatMessagePart(dataUrl, Images.ImageDetail.Auto));
                        }
                        break;

                    case DataContent dataContent:
                        if (!string.IsNullOrEmpty(dataContent.Uri))
                        {
                            parts.Add(new ChatMessagePart(new Uri(dataContent.Uri), ChatMessageTypes.Document));
                        }
                        else if (dataContent.Data.HasValue)
                        {
                            var base64 = Convert.ToBase64String(dataContent.Data.Value.ToArray());
                            parts.Add(new ChatMessagePart(base64, DocumentLinkTypes.Base64));
                        }
                        break;

                    case FunctionCallContent functionCall:
                        // Function calls are handled separately
                        break;

                    case FunctionResultContent functionResult:
                        // Function results are handled separately
                        break;
                }
            }

            if (parts.Count > 0)
            {
                tornadoMessage.Parts = parts;
            }
        }

        // Handle function/tool calls
        var functionCalls = message.Contents?.OfType<FunctionCallContent>().ToList();
        if (functionCalls != null && functionCalls.Count > 0)
        {
            tornadoMessage.ToolCalls = functionCalls.Select(fc => new LlmTornado.ChatFunctions.ToolCall
            {
                Id = fc.CallId,
                FunctionCall = new LlmTornado.ChatFunctions.FunctionCall
                {
                    Name = fc.Name,
                    Arguments = fc.Arguments != null 
                        ? System.Text.Json.JsonSerializer.Serialize(fc.Arguments)
                        : "{}"
                }
            }).ToList();
        }

        // Handle function/tool results
        var functionResults = message.Contents?.OfType<FunctionResultContent>().ToList();
        if (functionResults != null && functionResults.Count > 0 && role == ChatMessageRoles.Tool)
        {
            tornadoMessage.ToolCallId = functionResults.First().CallId;
            tornadoMessage.Content = functionResults.First().Result?.ToString();
        }

        return tornadoMessage;
    }

    /// <summary>
    /// Converts a LlmTornado ChatMessage to a Microsoft.Extensions.AI ChatMessage.
    /// </summary>
    public static global::Microsoft.Extensions.AI.ChatMessage ToMicrosoftAI(this LlmTornado.Chat.ChatMessage message)
    {
        var role = message.Role switch
        {
            ChatMessageRoles.User => ChatRole.User,
            ChatMessageRoles.Assistant => ChatRole.Assistant,
            ChatMessageRoles.System => ChatRole.System,
            ChatMessageRoles.Tool => ChatRole.Tool,
            _ => ChatRole.User
        };

        var contents = new List<AIContent>();

        // Add text content
        if (!string.IsNullOrEmpty(message.Content))
        {
            contents.Add(new TextContent(message.Content));
        }

        // Add parts if available
        if (message.Parts != null)
        {
            foreach (var part in message.Parts)
            {
                switch (part.Type)
                {
                    case ChatMessageTypes.Text:
                        if (!string.IsNullOrEmpty(part.Text))
                        {
                            contents.Add(new TextContent(part.Text));
                        }
                        break;

                    case ChatMessageTypes.Image:
                        if (part.Image != null && !string.IsNullOrEmpty(part.Image.Url))
                        {
                            // Check if it's a data URL or regular URL
                            if (part.Image.Url.StartsWith("data:"))
                            {
                                // Parse data URL
                                var dataUrlParts = part.Image.Url.Split(new[] { ',' }, 2);
                                if (dataUrlParts.Length == 2)
                                {
                                    var mimeType = dataUrlParts[0].Split(new[] { ';' })[0].Replace("data:", "");
                                    var base64Data = dataUrlParts[1];
                                    var bytes = Convert.FromBase64String(base64Data);
                                    contents.Add(new ImageContent(bytes, mimeType));
                                }
                            }
                            else
                            {
                                contents.Add(new ImageContent(part.Image.Url));
                            }
                        }
                        break;
                }
            }
        }

        // Add tool calls
        if (message.ToolCalls != null && message.ToolCalls.Count > 0)
        {
            foreach (var toolCall in message.ToolCalls)
            {
                if (toolCall.FunctionCall != null)
                {
                    var args = toolCall.FunctionCall.Arguments != null
                        ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object?>>(toolCall.FunctionCall.Arguments)
                        : null;
                    
                    contents.Add(new FunctionCallContent(
                        toolCall.Id ?? Guid.NewGuid().ToString(),
                        toolCall.FunctionCall.Name ?? "",
                        args));
                }
            }
        }

        // Handle tool results
        if (role == ChatRole.Tool && !string.IsNullOrEmpty(message.ToolCallId))
        {
            contents.Add(new FunctionResultContent(message.ToolCallId, message.ToolCallId, message.Content));
        }

        return new global::Microsoft.Extensions.AI.ChatMessage(role, contents);
    }

    /// <summary>
    /// Converts Microsoft.Extensions.AI ChatOptions to LlmTornado ChatRequest.
    /// </summary>
    public static void ApplyToRequest(this ChatOptions? options, ChatRequest request)
    {
        if (options == null)
        {
            return;
        }

        if (options.Temperature.HasValue)
        {
            request.Temperature = options.Temperature.Value;
        }

        if (options.TopP.HasValue)
        {
            request.TopP = options.TopP.Value;
        }

        if (options.MaxOutputTokens.HasValue)
        {
            request.MaxTokens = options.MaxOutputTokens.Value;
        }

        if (options.FrequencyPenalty.HasValue)
        {
            request.FrequencyPenalty = options.FrequencyPenalty.Value;
        }

        if (options.PresencePenalty.HasValue)
        {
            request.PresencePenalty = options.PresencePenalty.Value;
        }

        if (options.StopSequences != null && options.StopSequences.Count > 0)
        {
            if (options.StopSequences.Count == 1)
            {
                request.StopSequence = options.StopSequences[0];
            }
            else
            {
                request.MultipleStopSequences = options.StopSequences.ToArray();
            }
        }

        // Convert tools/functions
        if (options.Tools != null && options.Tools.Count > 0)
        {
            var tools = new List<LlmTornado.Common.Tool>();
            
            foreach (var tool in options.Tools)
            {
                if (tool is AIFunction aiFunction)
                {                    
                    tools.Add(new LlmTornado.Common.Tool(aiFunction.ToToolFunction()));
                }
            }
            
            if (tools.Count > 0)
            {
                request.Tools = tools;
            }
        }
    }

    /// <summary>
    /// Converts LlmTornado ChatResult to Microsoft.Extensions.AI ChatCompletion.
    /// </summary>
    public static global::Microsoft.Extensions.AI.ChatCompletion ToChatCompletion(this ChatResult result)
    {
        var choices = result.Choices ?? new List<LlmTornado.Chat.ChatChoice>();
        var choice = choices.FirstOrDefault();

        var message = choice?.Message ?? new LlmTornado.Chat.ChatMessage(ChatMessageRoles.Assistant, "");
        
        var completion = new global::Microsoft.Extensions.AI.ChatCompletion(message.ToMicrosoftAI())
        {
            CompletionId = result.Id,
            ModelId = result.Model,
            CreatedAt = result.Created,
            FinishReason = ConvertFinishReason(choice?.FinishReason),
        };
        
        // Set raw representation via additional properties
        completion.AdditionalProperties ??= new global::Microsoft.Extensions.AI.AdditionalPropertiesDictionary();
        completion.AdditionalProperties["RawRepresentation"] = result;

        // Add usage information
        if (result.Usage != null)
        {
            completion.Usage = new UsageDetails
            {
                InputTokenCount = result.Usage.PromptTokens,
                OutputTokenCount = result.Usage.CompletionTokens,
                TotalTokenCount = result.Usage.TotalTokens
            };
        }

        return completion;
    }

    /// <summary>
    /// Converts finish reason from LlmTornado to Microsoft.Extensions.AI.
    /// </summary>
    private static ChatFinishReason? ConvertFinishReason(ChatMessageFinishReasons? finishReason)
    {
        return finishReason switch
        {
            ChatMessageFinishReasons.EndTurn => ChatFinishReason.Stop,
            ChatMessageFinishReasons.StopSequence => ChatFinishReason.Stop,
            ChatMessageFinishReasons.Length => ChatFinishReason.Length,
            ChatMessageFinishReasons.ToolCalls => ChatFinishReason.ToolCalls,
            ChatMessageFinishReasons.ContentFilter => ChatFinishReason.ContentFilter,
            _ => null
        };
    }


    public static ToolFunction ToToolFunction(this AIFunction function)
    {
        return new ToolFunction(
           name: function.Metadata.Name,
           description: function.Metadata.Description,
           parameters: $"{{ \"type\": \"object\","+
                                $"\"properties\": {{{GenerateProertiesFromMetaData(function)}}},"+
                                "\"additionalProperties\": false,"+
                                $"\"required\": {GenerateRequiredFromMetaData(function)} }}"
            );
    }

    private static string GenerateRequiredFromMetaData(AIFunction function)
    {
        if (function.Metadata.Parameters == null || function.Metadata.Parameters.Count == 0)
        {
            return "[ \"input\" ]";
        }
        var required = function.Metadata.Parameters.Where(p => p.IsRequired || !p.HasDefaultValue).Select(p => $"\"{p.Name}\"");
        string jsonStringArray = "[" + string.Join(", ", required) + "]";
        return jsonStringArray;
    }

    private static string GenerateProertiesFromMetaData(AIFunction function)
    {
        if (function.Metadata.Parameters == null || function.Metadata.Parameters.Count == 0)
        {
            return "{\"input\" : {\"type\" : \"string\"}";
        }
        var props = function.Metadata.Parameters.Select(kvp =>
        {
            return $"\"{kvp.Name}\" : {kvp.Schema}";
        });
        return string.Join(", ", props);
    }
}
