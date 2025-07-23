using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Images;
using Microsoft.VisualBasic;
using System.Collections;
using System.Collections.Generic;

namespace LlmTornado.Agents
{
    public partial class TornadoClient
    {
        //Provider -> ModelItem conversion methods
        public IList<ModelItem> ConvertFromProviderItems(ChatRichResponse response, Conversation result)
        {
            List<ModelItem> responseItems = new List<ModelItem>();
            foreach (ChatMessage item in result.Messages)
            {
                responseItems.Add(ConvertFromProviderItem(item));
            }
            return responseItems;
        }

        public ModelItem ConvertLastFromProviderItems(Conversation result)
        {
            ChatMessage item = result.Messages.Last();

            return ConvertFromProviderItem(item);
        }

        public ModelItem ConvertFromProviderItem(ChatMessage item)
        {
            switch (item.Role)
            {
                case ChatMessageRoles.Unknown:
                    return new ModelMessageItem(
                        item.Id.ToString(),
                        ChatMessageRoles.System.ToString(),
                        [new ModelMessageSystemResponseTextContent(item.Content),],
                        ModelStatus.Completed
                        );
                case ChatMessageRoles.System:
                    return new ModelMessageItem(
                        item.Id.ToString(),
                        ChatMessageRoles.System.ToString(),
                        [new ModelMessageSystemResponseTextContent(item.Content),],
                        ModelStatus.Completed
                        );
                case ChatMessageRoles.User:
                    return new ModelMessageItem(
                        item.Id.ToString(),
                        ChatMessageRoles.User.ToString(),
                        [new ModelMessageUserResponseTextContent(item.Content),],
                        ModelStatus.Completed
                        );
                case ChatMessageRoles.Assistant:
                    if (item.ToolCalls != null)
                    {
                        return new ModelFunctionCallItem(
                                item.Id.ToString(),
                                item.ToolCalls[0].Id!,
                                item.ToolCalls[0].FunctionCall.Name,
                                ModelStatus.Completed,
                                BinaryData.FromString(item.ToolCalls[0].FunctionCall.Arguments)
                                );
                    }
                    else
                    {
                        string ReasonSummary = string.Empty;
                        //Quen3.5 and Qwen 14B use <think> tags to indicate reasoning
                        if (item.Content.ToLower().StartsWith("<think>"))
                        {
                            // Extract reasoning summary from content
                            int startIndex = item.Content.IndexOf("<think>");
                            int endIndex = item.Content.IndexOf("</think>") + "</think>".Length;
                            if (endIndex > startIndex)
                            {
                                ReasonSummary = item.Content.Substring(startIndex, endIndex - startIndex);
                            }

                            string outputContent = item.Content.Substring(endIndex).Trim();

                            return new ModelMessageItem(
                                    item.Id.ToString(),
                                    ChatMessageRoles.Assistant.ToString(),
                                    [new ModelMessageAssistantResponseTextContent(ReasonSummary), new ModelMessageAssistantResponseTextContent(outputContent),],
                                    ModelStatus.Completed
                                    );
                        }
                        else
                        {
                            return new ModelMessageItem(
                                    item.Id.ToString(),
                                    ChatMessageRoles.Assistant.ToString(),
                                    [new ModelMessageAssistantResponseTextContent(item.Content),],
                                    ModelStatus.Completed
                                    );
                        }

                    }
                case ChatMessageRoles.Tool:
                    return new ModelFunctionCallOutputItem(
                            item.Id.ToString(),
                            item.ToolCallId,
                            item.Content,
                            ModelStatus.Completed,
                            item.ToolCallId
                            );
                default: break;
            }

            return new ModelMessageItem(
                        item.Id.ToString(),
                        ChatMessageRoles.User.ToString(),
                        [new ModelMessageUserResponseTextContent(item.Content),],
                        ModelStatus.Completed
                        );
        }

        //ModelItem -> Provider items
        public List<ChatMessagePart> ConvertModelContentToProviderPart(List<ModelMessageContent> contents)
        {
            List<ChatMessagePart> parts = new List<ChatMessagePart>();
            foreach(var content in contents)
            {
                parts.Add(ConvertModelContentToProviderPart(content));
            }
            return parts;
        }

        public ChatMessagePart ConvertModelContentToProviderPart(ModelMessageContent content)
        {
            if (content is ModelMessageTextContent textContent)
            {
                return new ChatMessagePart(textContent.Text);
            }
            else if (content is ModelMessageImageFileContent imageContent)
            {
                return new ChatMessagePart(imageContent.DataUri, ImageDetail.Auto);
            }
            else if (content is ModelMessageFileContent fileContent)
            {
                return new ChatMessagePart(fileContent.DataUri, ImageDetail.Auto);
            }
            else if(content is ModelMessageSystemResponseTextContent systemContent)
            {
                return new ChatMessagePart(systemContent.Text);
            }
            else if (content is ModelMessageUserResponseTextContent userContent)
            {
                return new ChatMessagePart(userContent.Text);
            }
            else if (content is ModelMessageAssistantResponseTextContent assistantContent)
            {
                return new ChatMessagePart(assistantContent.Text);
            }
            else
            {
                throw new ArgumentException($"Unknown ModelMessageContent type: {content.GetType().Name}", nameof(content));
            }
        }

        public Conversation ConvertToProviderItems(IEnumerable messages, Conversation conv)
        {
            foreach (ModelItem item in messages)
            {
                if (item is ModelWebCallItem webSearchCall)
                {

                }
                else if (item is ModelFileSearchCallItem fileSearchCall)
                {

                }
                else if (item is ModelReasoningItem reasoning)
                {
                    ChatMessage chatMessage = new ChatMessage();
                    chatMessage.Role = ChatMessageRoles.Assistant;
                    chatMessage.Content = string.Join("\n", reasoning.Summary);
                    conv.AppendMessage(chatMessage);
                }
                else if (item is ModelFunctionCallItem toolCall)
                {
                    ChatMessage chatMessage = new ChatMessage();
                    chatMessage.Role = ChatMessageRoles.Assistant;
                    chatMessage.ToolCalls = new List<ToolCall>();

                    ToolCall call = new ToolCall();
                    call.Id = toolCall.CallId;
                    call.FunctionCall = new FunctionCall();
                    call.FunctionCall.Name = toolCall.FunctionName;
                    call.FunctionCall.Arguments = toolCall.FunctionArguments.ToString();

                    chatMessage.ToolCalls.Add(call);

                    conv.AppendMessage(chatMessage);
                }
                else if (item is ModelFunctionCallOutputItem toolOutput)
                {
                    ChatMessage chatMessage = new ChatMessage();
                    chatMessage.Role = ChatMessageRoles.Tool;
                    chatMessage.ToolCallId = toolOutput.CallId;
                    chatMessage.Content = toolOutput.FunctionOutput;
                    chatMessage.ToolCalls = new List<ToolCall>();

                    ToolCall call = new ToolCall();
                    call.Id = toolOutput.CallId;
                    call.FunctionCall = new FunctionCall();
                    call.FunctionCall.Name = toolOutput.FunctionName;

                    chatMessage.ToolCalls.Add(call);

                    conv.AppendMessage(chatMessage);
                }
                else if (item is ModelComputerCallItem computerCall)
                {

                }
                else if (item is ModelComputerCallOutputItem computerOutput)
                {

                }
                else if (item is ModelMessageItem message)
                {
                    if (message.Role.ToUpper() == "ASSISTANT")
                    {
                        conv.AppendMessage(new ChatMessage(ChatMessageRoles.Assistant, ConvertModelContentToProviderPart(message.Content)));
                    }
                    else if (message.Role.ToUpper() == "USER")
                    {
                        conv.AppendMessage(new ChatMessage(ChatMessageRoles.User, ConvertModelContentToProviderPart(message.Content)));
                    }
                    else if (message.Role.ToUpper() == "SYSTEM")
                    {
                        conv.AppendMessage(new ChatMessage(ChatMessageRoles.System, ConvertModelContentToProviderPart(message.Content)));
                    }
                    else if (message.Role.ToUpper() == "DEVELOPER")
                    {
                        conv.AppendMessage(new ChatMessage(ChatMessageRoles.Unknown, ConvertModelContentToProviderPart(message.Content)));
                    }
                }
                else
                {
                    throw new ArgumentException($"Unknown ModelItem type: {item.GetType().Name}", nameof(messages));
                }
            }

            return conv;
        }
    }
}
