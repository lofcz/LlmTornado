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
            if (chatMessage.Content is not null)
            {
                var inputMessage = new ResponseInputMessage()
                {
                    Role = chatMessage.Role ?? ChatMessageRoles.User,
                };
                
                inputMessage.Content.Add(new ResponseInputContentText(chatMessage.Content));
            }
        }
        
        return items;
    }

    public static ResponseRequest ToResponseRequest(ResponseRequest request, ChatRequest fallback)
    {
        return new ResponseRequest();
    }

    public static ChatResult ToChatResult(ResponseResult result)
    {
        return new ChatResult
        {
            Id = result.Id,
            Model = result.Model ?? string.Empty,
            Choices = 
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
                    choice.Message.Role = Enum.Parse<ChatMessageRoles>(messageItem.Role);
                    
                    if (messageItem.Content.FirstOrDefault(x => x is ResponseOutputTextContent) is ResponseOutputTextContent outputText)
                    {
                        choice.Message.Content = outputText.Text;
                    }
                    else if (messageItem.Content.FirstOrDefault(x => x is RefusalContent) is RefusalContent refusalContent)
                    {
                        choice.Message.Refusal = refusalContent.Refusal;
                    }
                    
                    break;
                case 
            }
        }
        
        return choice;
    }
}