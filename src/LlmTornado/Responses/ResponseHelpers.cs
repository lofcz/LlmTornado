using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat;
using LlmTornado.Code;

namespace LlmTornado.Responses;

public static class ResponseHelpers
{
    public static List<ResponseInputItem> ToReponseInputItems(List<ChatMessage> messages)
    {
        List<ResponseInputItem> items = [];
        
        foreach (ChatMessage chatMessage in messages)
        {
            if (chatMessage.Role is ChatMessageRoles.Assistant)
            {
                var outputMessage = new OutputMessageInput();
                
                if (chatMessage.Content is not null)
                {
                    outputMessage.Content.Add(new ResponseOutputTextContent
                    {
                        Text = chatMessage.Content
                    });
                }
                else if (chatMessage.Refusal is not null)
                {
                    outputMessage.Content.Add(new RefusalContent
                    {
                        Refusal = chatMessage.Refusal
                    });
                }
                
                items.Add(outputMessage);
            }
            else
            {
                var inputMessage = new ResponseInputMessage();
                
                if (chatMessage.Parts is not null)
                {
                    foreach (ChatMessagePart part in chatMessage.Parts)
                    {
                        switch (part.Type)
                        {
                            case ChatMessageTypes.Text:
                                if (part.Text is not null)
                                {
                                    inputMessage.Content.Add(new ResponseInputContentText(part.Text));
                                }

                                break;
                            case ChatMessageTypes.Image:
                                if (part.Image is not null)
                                {
                                    inputMessage.Content.Add(new ResponseInputContentImage
                                    {
                                        ImageUrl = part.Image.Url,
                                        Detail = part.Image.Detail
                                    });
                                }
                                
                                break;
                            case ChatMessageTypes.FileLink:
                                //TODO: Does the file work across all providers?
                                break;
                        }
                    }
                }
                
                items.Add(inputMessage);
            }
            
        }
        
        return items;
    }

    public static ResponseRequest ToResponseRequest(ResponseRequest request, ChatRequest chatRequest)
    {
        return new ResponseRequest()
        {
            Model = request.Model ?? chatRequest.Model,
            Background = request.Background,
            Instructions = request.Instructions ??
                           chatRequest.Messages?.FirstOrDefault(x => x.Role is ChatMessageRoles.System)?.Content ??
                           string.Empty,
            InputItems = request.InputItems ?? ToReponseInputItems(chatRequest.Messages ?? []),
            Prompt = request.Prompt,
            Reasoning = request.Reasoning,
            Stream = false,
            Temperature = request.Temperature ?? chatRequest.Temperature,
            Text = request.Text,
        };
    }

    public static ChatResult ToChatResult(ResponseResult result)
    {
        return new ChatResult
        {
            Id = result.Id,
            Model = result.Model ?? string.Empty,
            Choices = result.Output is not null ? [ToChatChoice(result.Output)] : []
        };
    }

    public static ChatChoice ToChatChoice(List<IResponseOutputItem> responseItems)
    {
        ChatChoice choice = new();

        foreach (IResponseOutputItem responseItem in responseItems)
        {
            choice.Message ??= new ChatMessage();
            switch (responseItem)
            {
                case ResponseOutputMessageItem messageItem:
                    choice.Message.Role = messageItem.Role;
                    
                    if (messageItem.Content.FirstOrDefault(x => x is ResponseOutputTextContent) is ResponseOutputTextContent outputText)
                    {
                        choice.Message.Content = outputText.Text;
                    }
                    else if (messageItem.Content.FirstOrDefault(x => x is RefusalContent) is RefusalContent refusalContent)
                    {
                        choice.Message.Refusal = refusalContent.Refusal;
                    }
                    break;
            }
        }
        
        return choice;
    }
}