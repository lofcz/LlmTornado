using System.Drawing;
using LlmTornado.Chat;
using LlmTornado.Code;
using LlmTornado.ChatFunctions;
using LlmTornado.Common;
using Microsoft.Extensions.AI;
using ChatMessage = LlmTornado.Chat.ChatMessage;
using LlmTornado.Images;
#pragma warning disable MEAI001

namespace LlmTornado.Microsoft.Extensions.AI;

/// <summary>
/// Provides conversion methods between Microsoft.Extensions.AI types and LlmTornado types.
/// </summary>
internal static class TypeConverters
{
    /// <summary>
    /// Converts a Microsoft.Extensions.AI ChatMessage to a LlmTornado ChatMessage.
    /// </summary>
    public static ChatMessage ToLlmTornado(this global::Microsoft.Extensions.AI.ChatMessage message)
    {
        ChatMessageRoles role = message.Role.Value switch
        {
            "user" => ChatMessageRoles.User,
            "assistant" => ChatMessageRoles.Assistant,
            "system" => ChatMessageRoles.System,
            "tool" => ChatMessageRoles.Tool,
            _ => ChatMessageRoles.User
        };

        ChatMessage tornadoMessage = new ChatMessage(role);

        // Handle different content types
        if (message.Contents is { Count: > 0 })
        {
            List<ChatMessagePart> parts = [];

            foreach (AIContent content in message.Contents)
            {
                switch (content)
                {
                    case TextContent textContent:
                        if (!string.IsNullOrEmpty(textContent.Text))
                        {
                            parts.Add(new ChatMessagePart(textContent.Text));
                        }
                        break;

                    case UriContent imageContent:
                        if (!string.IsNullOrEmpty(imageContent.Uri.AbsolutePath))
                        {
                            parts.Add(new ChatMessagePart(imageContent.Uri));
                        }
                        break;

                    case DataContent dataContent:
                        if (!string.IsNullOrEmpty(dataContent.Uri))
                        {
                            parts.Add(new ChatMessagePart(new Uri(dataContent.Uri), ChatMessageTypes.Document));
                        }
                        else
                        {
                            string base64 = Convert.ToBase64String(dataContent.Data.ToArray());
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
        List<FunctionCallContent>? functionCalls = message.Contents?.OfType<FunctionCallContent>().ToList();
        if (functionCalls != null && functionCalls.Count > 0)
        {
            tornadoMessage.ToolCalls = functionCalls.Select(fc => new ToolCall
            {
                Id = fc.CallId,
                FunctionCall = new FunctionCall
                {
                    Name = fc.Name,
                    Arguments = fc.Arguments != null 
                        ? System.Text.Json.JsonSerializer.Serialize(fc.Arguments)
                        : "{}"
                }
            }).ToList();
        }

        // Handle function/tool results
        List<FunctionResultContent>? functionResults = message.Contents?.OfType<FunctionResultContent>().ToList();
        if (functionResults is { Count: > 0 } && role == ChatMessageRoles.Tool)
        {
            tornadoMessage.ToolCallId = functionResults.First().CallId;
            tornadoMessage.Content = functionResults.First().Result?.ToString();
        }

        return tornadoMessage;
    }

    /// <summary>
    /// Converts a LlmTornado ChatMessage to a Microsoft.Extensions.AI ChatMessage.
    /// </summary>
    public static global::Microsoft.Extensions.AI.ChatMessage ToMicrosoftAI(this ChatMessage message)
    {
        ChatRole role = message.Role switch
        {
            ChatMessageRoles.User => ChatRole.User,
            ChatMessageRoles.Assistant => ChatRole.Assistant,
            ChatMessageRoles.System => ChatRole.System,
            ChatMessageRoles.Tool => ChatRole.Tool,
            _ => ChatRole.User
        };

        List<AIContent> contents = [];

        // Add text content
        if (!string.IsNullOrEmpty(message.Content))
        {
            contents.Add(new TextContent(message.Content));
        }

        // Add parts if available
        if (message.Parts != null)
        {
            foreach (ChatMessagePart part in message.Parts)
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
                                string[] dataUrlParts = part.Image.Url.Split([','], 2);
                                if (dataUrlParts.Length == 2)
                                {
                                    string mimeType = dataUrlParts[0].Split([';'])[0].Replace("data:", "");
                                    string base64Data = dataUrlParts[1];
                                    byte[] bytes = Convert.FromBase64String(base64Data);
                                    contents.Add(new DataContent(bytes, mimeType));
                                }
                            }
                            else
                            {
                                contents.Add(new UriContent(part.Image.Url, part.Image.MimeType ?? string.Empty));
                            }
                        }
                        break;
                }
            }
        }

        // Add tool calls
        if (message.ToolCalls is { Count: > 0 })
        {
            foreach (ToolCall toolCall in message.ToolCalls)
            {
                if (toolCall.FunctionCall != null)
                {
                    Dictionary<string, object?>? args = toolCall.FunctionCall.Arguments != null
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
            contents.Add(new FunctionResultContent(message.ToolCallId, message.Content));
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

        if (options.StopSequences is { Count: > 0 })
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
        if (options.Tools is { Count: > 0 })
        {
            List<Tool> tools = [];
            
            foreach (AITool tool in options.Tools)
            {
                if (tool is AIFunction aiFunction)
                {                    
                    tools.Add(new Tool(aiFunction.ToToolFunction()));
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
    public static ChatResponse ToChatCompletion(this ChatResult result)
    {
        List<ChatChoice> choices = result.Choices ?? [];
        ChatChoice? choice = choices.FirstOrDefault();

        ChatMessage message = choice?.Message ?? new ChatMessage(ChatMessageRoles.Assistant, "");
        
        ChatResponse completion = new ChatResponse(message.ToMicrosoftAI())
        {
            ResponseId = result.Id,
            ModelId = result.Model,
            CreatedAt = result.Created,
            FinishReason = ConvertFinishReason(choice?.FinishReason),
        };
        
        // Set raw representation via additional properties
        completion.AdditionalProperties ??= new AdditionalPropertiesDictionary();
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
           name: function.Name,
           description: function.Description,
           parameters: function.JsonSchema);
    }
    
    /// <summary>
    /// Converts a Microsoft.Extensions.AI ImageGenerationRequest to a LlmTornado ImageGenerationRequest.
    /// </summary>
    public static Images.ImageGenerationRequest ToLlmTornado(this global::Microsoft.Extensions.AI.ImageGenerationRequest request, string model, global::Microsoft.Extensions.AI.ImageGenerationOptions? options)
    {
        Images.ImageGenerationRequest tornadoRequest = new Images.ImageGenerationRequest
        {
            Prompt = request.Prompt,
            Model = model,
            NumOfImages = options?.Count,
            ResponseFormat = options?.ResponseFormat != null 
                ? (options.ResponseFormat == ImageGenerationResponseFormat.Uri 
                    ? TornadoImageResponseFormats.Url 
                    : TornadoImageResponseFormats.Base64)
                : null
        };
        
        // Handle image size conversion
        if (options?.ImageSize is not null)
        {
            Size size = options.ImageSize.Value;
            string sizeString = $"{size.Width}x{size.Height}";
            
            // Try to find a matching enum value
            TornadoImageSizes? matchedSize = Images.ImageGenerationRequest.TryParseSizeString(sizeString);
            
            if (matchedSize.HasValue)
            {
                tornadoRequest.Size = matchedSize.Value;
            }
            else
            {
                // Custom size - use Custom enum with Width/Height properties
                tornadoRequest.Size = TornadoImageSizes.Custom;
                tornadoRequest.Width = size.Width;
                tornadoRequest.Height = size.Height;
            }
        }
        
        return tornadoRequest;
    }
    
    /// <summary>
    /// Converts a LlmTornado ImageResult to a Microsoft.Extensions.AI ImageGenerationResponse.
    /// </summary>
    public static global::Microsoft.Extensions.AI.ImageGenerationResponse ToMicrosoftAI(this ImageGenerationResult result)
    {
        List<AIContent> contents = [];

        if (result.Data != null)
        {
            foreach (TornadoGeneratedImage data in result.Data)
            {
                if (!string.IsNullOrEmpty(data.Url))
                {
                    contents.Add(new UriContent(new Uri(data.Url), "image/png"));
                }
                else if (!string.IsNullOrEmpty(data.Base64))
                {
                    byte[] bytes = Convert.FromBase64String(data.Base64);
                    contents.Add(new DataContent(bytes, "image/png"));
                }
            }
        }

        ImageGenerationResponse response = new ImageGenerationResponse(contents)
        {
            RawRepresentation = result
        };

        return response;
    }
}
