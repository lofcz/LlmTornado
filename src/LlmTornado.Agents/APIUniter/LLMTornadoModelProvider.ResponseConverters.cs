using LlmTornado.Chat;
using LlmTornado.Code;
using LlmTornado.Responses;
using LlmTornado.Threads;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents
{
    public partial class TornadoClient
    {
        //Provider --> Model
        public List<ModelItem> ConvertOutputItems(List<IResponseOutputItem> outputItems)
        {
            return outputItems.ConvertAll(ConvertFromProviderOutputItem);
        }

        public ModelItem ConvertFromProviderOutputItem(IResponseOutputItem item)
        {
            if (item is ResponseWebSearchToolCallItem webSearchCall)
            {
                return new ModelWebCallItem(webSearchCall.Id, webSearchCall.Status
                    .ToString()
                    .TryParseEnum(out ModelWebSearchingStatus _status) ? _status : ModelWebSearchingStatus.Completed);
            }
            else if (item is ResponseFileSearchToolCallItem fileSearchCall)
            {
                List<FileSearchCallContent> resultContents = new List<FileSearchCallContent>();

                foreach (var file in fileSearchCall.Results)
                {
                    Console.WriteLine($" {file.Filename}");
                    resultContents.Add(new FileSearchCallContent(file.FileId, file.Text, file.Filename, (float?)file.Score));
                }

                return new ModelFileSearchCallItem(fileSearchCall.Id,
                    fileSearchCall.Queries.ToList(),
                    fileSearchCall.Status
                        .ToString()
                        .TryParseEnum(out ModelStatus status) ? status : ModelStatus.Completed
                        ,
                    resultContents);
            }
            else if (item is ResponseFunctionToolCallItem toolCall)
            {
                ModelStatus status = toolCall.Status.ToString().TryParseEnum(out ModelStatus ostatus) ? ostatus : ModelStatus.Completed;

                return new ModelFunctionCallItem(
                    toolCall.Id,
                    toolCall.CallId,
                    toolCall.Name,
                    status,
                    BinaryData.FromString(toolCall.Arguments)
                    );

            }
            else if (item is ResponseComputerToolCallItem computerCall)
            {
                return ConvertItemToModelComputerCall(computerCall);
            }
            else if (item is ResponseReasoningItem reasoningItem)
            {
                //They changed the reasoning item to not have an content, so we just return the ID and encrypted content
                return new ModelReasoningItem(reasoningItem.Id, reasoningItem.Summary.Select(item => item.Text).ToArray());
            }
            else if (item is ResponseOutputMessageItem message)
            {
                List<ModelMessageContent> messageContent = ConvertProviderOutputContentToModelContents(message.Content.ToList());

                ModelStatus status = message.Status.ToString().TryParseEnum(out ModelStatus ostat) ? ostat : ModelStatus.Completed;

                return new ModelMessageItem(
                    "",
                    message.Role.ToString(),
                    messageContent,
                    status
                    );
            }
            else
            {
                throw new ArgumentException($"Unknown ResponseItem type: {item.GetType().Name}", nameof(item));
            }
        }
        public ModelItem ConvertFromProviderInputItem(ResponseInputItem item)
        {
            if (item is WebSearchToolCallInput webSearchCall)
            {
                return new ModelWebCallItem(webSearchCall.Id, webSearchCall.Status
                    .ToString()
                    .TryParseEnum(out ModelWebSearchingStatus _status) ? _status : ModelWebSearchingStatus.Completed);
            }
            else if (item is FileSearchToolCallInput fileSearchCall)
            {
                List<FileSearchCallContent> resultContents = new List<FileSearchCallContent>();

                foreach (var file in fileSearchCall.Results)
                {
                    Console.WriteLine($" {file.Filename}");
                    resultContents.Add(new FileSearchCallContent(file.FileId, file.Text, file.Filename, (float?)file.Score));
                }

                return new ModelFileSearchCallItem(fileSearchCall.Id,
                    fileSearchCall.Queries.ToList(),
                    fileSearchCall.Status
                        .ToString()
                        .TryParseEnum(out ModelStatus status) ? status : ModelStatus.Completed
                        ,
                    resultContents);
            }
            else if (item is FunctionToolCallInput toolCall)
            {
                ModelStatus status = toolCall.Status.ToString().TryParseEnum(out ModelStatus ostatus)? ostatus : ModelStatus.Completed;

                return new ModelFunctionCallItem(
                    toolCall.Id,
                    toolCall.CallId,
                    toolCall.Name,
                    status,
                    BinaryData.FromString(toolCall.Arguments)
                    );

            }
            else if (item is FunctionToolCallOutput toolOutput)
            {
                ModelStatus status = toolOutput.Status.ToString().TryParseEnum(out ModelStatus ostatus) ? ostatus : ModelStatus.Completed;

                return new ModelFunctionCallOutputItem(
                    toolOutput.Id,
                    toolOutput.CallId,
                    toolOutput.Output,
                    status,
                    ""
                    );
            }
            else if (item is ComputerToolCallInput computerCall)
            {
                return ConvertToModelComputerCall(computerCall);
            }
            else if (item is ComputerToolCallOutput computerOutput)
            {
                return ConvertComputerOutputToModelItem(computerOutput);
            }
            else if (item is Reasoning reasoningItem)
            {
                //They changed the reasoning item to not have an content, so we just return the ID and encrypted content
                return new ModelReasoningItem(reasoningItem.Id, reasoningItem.Summary.Select(item=>item.Text).ToArray());
            }
            else if (item is ResponseInputMessage message)
            {
                List<ModelMessageContent> messageContent = ConvertProviderContentToModelContents(message.Content.ToList());

                ModelStatus status = message.Status.ToString().TryParseEnum(out ModelStatus ostat) ? ostat : ModelStatus.Completed;

                return new ModelMessageItem(
                    "",
                    message.Role.ToString(),
                    messageContent,
                    status
                    );
            }
            else
            {
                throw new ArgumentException($"Unknown ResponseItem type: {item.GetType().Name}", nameof(item));
            }
        }

        public List<ModelMessageContent> ConvertProviderContentToModelContents(List<ResponseInputContent> contentParts)
        {
            List<ModelMessageContent> messageContent = new List<ModelMessageContent>();
            foreach(var part in contentParts)
            {
                messageContent.Add(ConvertProviderContentToModelContent(part));
            }
            return messageContent;
        }

        public ModelMessageContent ConvertProviderContentToModelContent(ResponseInputContent contentPart)
        {
            switch (contentPart)
            {
                case ResponseInputContentText content:
                    return new ModelMessageResponseTextContent(content.Text);
                case ResponseInputContentImage image:
                    return new ModelMessageImageFileContent(image.ImageUrl);
                case ResponseInputContentFile file:
                        return new ModelMessageFileContent().CreateFileContentByID(file.FileId, file.Filename);
                default: throw new Exception($"Cannot convert from type {contentPart.GetType()}");
            }
        }
        public List<ModelMessageContent> ConvertProviderOutputContentToModelContents(List<IResponseOutputContent> contentParts)
        {
            List<ModelMessageContent> messageContent = new List<ModelMessageContent>();
            foreach (var part in contentParts)
            {
                messageContent.Add(ConvertProviderOutputContentToModelContent(part));
            }
            return messageContent;
        }

        public ModelMessageContent ConvertProviderOutputContentToModelContent(IResponseOutputContent contentPart)
        {
            switch (contentPart)
            {
                case ResponseOutputTextContent content:
                    return new ModelMessageResponseTextContent(content.Text);
                case RefusalContent content:
                    return new ModelMessageRefusalContent(content.Refusal);
                default: throw new Exception($"Cannot convert from type {contentPart.GetType()}");
            }
        }

        public ModelComputerCallItem ConvertItemToModelComputerCall(ResponseComputerToolCallItem computerCall)
        {
            if (computerCall?.Action is null)
                throw new ArgumentNullException(nameof(computerCall));

            // Local helper so we don’t repeat the constructor footprint eight times.
            ModelComputerCallItem Wrap(string id, string callId, ModelStatus status, ComputerToolAction action)
                => new(id, callId, ModelStatus.Completed, action);

            // Pattern‑matching switch keeps the code compact and type‑safe.
            return computerCall.Action switch
            {
                ClickAction clickAction =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionClick(
                            clickAction.X,
                            clickAction.Y,
                            clickAction.Button.ToString().TryParseEnum(out MouseButtons button) ? button : MouseButtons.Left)),

                DoubleClickAction clickAction =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionDoubleClick(clickAction.X, clickAction.Y)),

                DragAction drag =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionDrag(drag.Path[0].X, drag.Path[0].Y, drag.Path[1].X, drag.Path[1].Y)),
                KeyPressAction keyPress =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionKeyPress(keyPress.Keys)),
                MoveAction move =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionMove(move.X, move.Y)),
                ScreenshotAction screenshot =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionScreenShot()),
                ScrollAction scroll =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionScroll(scroll.ScrollY, scroll.ScrollY)),
                TypeAction type =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionType(type.Text)),
                WaitAction wait =>
                Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionWait()),

                _ => throw new NotSupportedException($"Cannot convert action of type {computerCall.Action.GetType().Name}")
            };
        }
        public ModelComputerCallItem ConvertToModelComputerCall(ComputerToolCallInput computerCall)
        {
            if (computerCall?.Action is null)
                throw new ArgumentNullException(nameof(computerCall));

            // Local helper so we don’t repeat the constructor footprint eight times.
            ModelComputerCallItem Wrap(string id, string callId, ModelStatus status, ComputerToolAction action)
                => new(id, callId, ModelStatus.Completed, action);

            // Pattern‑matching switch keeps the code compact and type‑safe.
            return computerCall.Action switch
            {
                ClickAction clickAction =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionClick(
                            clickAction.X,
                            clickAction.Y,
                            clickAction.Button.ToString().TryParseEnum(out MouseButtons button) ? button : MouseButtons.Left)),

                DoubleClickAction clickAction =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionDoubleClick(clickAction.X, clickAction.Y)),

                DragAction drag =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionDrag(drag.Path[0].X, drag.Path[0].Y, drag.Path[1].X, drag.Path[1].Y)),
                KeyPressAction keyPress =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionKeyPress(keyPress.Keys)),
                MoveAction move =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionMove(move.X, move.Y)),
                ScreenshotAction screenshot =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionScreenShot()),
                ScrollAction scroll =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionScroll(scroll.ScrollY, scroll.ScrollY)),
                TypeAction type =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionType(type.Text)),
                WaitAction wait =>
                Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        ModelStatus.Completed,
                        new ComputerToolActionWait()),

                _ => throw new NotSupportedException($"Cannot convert action of type {computerCall.Action.GetType().Name}")
            };
        }

        public ModelComputerCallOutputItem ConvertComputerOutputToModelItem(ComputerToolCallOutput computerCallOutput)
        {
            ModelComputerCallOutputItem computerToolCallOutput = 
                new ModelComputerCallOutputItem(
                    computerCallOutput.Id, 
                    computerCallOutput.CallId, 
                    ModelStatus.Completed,
                new ModelMessageImageFileContent(computerCallOutput.Output.ImageUrl));

            return computerToolCallOutput;
        }

        public List<ResponseInputContent> ConverModelContentToProviderContent(List<ModelMessageContent> contentParts)
        {
            List<ResponseInputContent> responseContents = new List<ResponseInputContent>();
            foreach (var part in contentParts)
            {
                responseContents.Add(ConvertModelContentToProviderContent(part));
            }
            return responseContents;
        }

        private ResponseInputContent ConvertModelContentToProviderContent(ModelMessageContent part)
        {
            if(part is ModelMessageResponseTextContent textContent)
            {
                return new ResponseInputContentText(textContent.Text);
            }
            if(part is ModelMessageRequestTextContent textRequestContent)
            {
                return new ResponseInputContentText(textRequestContent.Text);
            }
            else if (part is ModelMessageImageFileContent imageContent)
            {
                return ResponseInputContentImage.CreateImageUrl(imageContent.DataUri);
            }
            else if (part is ModelMessageFileContent fileContent)
            {
                return new ResponseInputContentFile(fileContent.FileId);
            }
            else if (part is ModelMessageSystemResponseTextContent systemTextContent)
            {
                return new ResponseInputContentText(systemTextContent.Text);
            }
            else if (part is ModelMessageResponseTextContent utextContent)
            {
                return  new ResponseInputContentText(utextContent.Text);
            }
            else
            {
                if(part.ContentType == ModelContentType.InputText || part.ContentType == ModelContentType.OutputText)
                {
                    var partText = part as ModelMessageResponseTextContent;
                    return new ResponseInputContentText(partText.Text);
                }
                throw new ArgumentException($"Unknown ModelMessageContent type: {part.GetType().Name}", nameof(part));
            }
        }

        //ModelItem -> Provider items
        public List<ResponseInputItem> ConvertToProviderResponseItems(IEnumerable messages)
        {
            List<ResponseInputItem> InputItems = new List<ResponseInputItem>();
            foreach (ModelItem item in messages)
            {
                if (item is ModelWebCallItem webSearchCall)
                {
                    InputItems.Add(new WebSearchToolCallInput(webSearchCall.Id, new WebSearchActionSearch() { Query = webSearchCall.Query }, ResponseMessageStatuses.Completed));
                }
                else if (item is ModelFileSearchCallItem fileSearchCall)
                {
                    InputItems.Add(new FileSearchToolCallInput(fileSearchCall.Id,fileSearchCall.Queries, ResponseMessageStatuses.Completed));
                }
                else if (item is ModelReasoningItem reasoning)
                {
                    Reasoning reasoningItem = new Reasoning();
                    reasoningItem.Id = reasoning.Id;
                    reasoningItem.EncryptedContent = reasoning.EncryptedContent;
                    foreach(var sum in reasoning.Summary)
                    {
                        var summary = new ReasoningSummaryText();
                        summary.Text = sum;
                        reasoningItem.Summary.Add(summary);
                    }
                    InputItems.Add(reasoningItem);
                }
                else if (item is ModelFunctionCallItem toolCall)
                {
                    var toolCallItem = new FunctionToolCallInput();
                    toolCallItem.CallId = toolCall.CallId;
                    toolCallItem.Id = toolCall.Id;
                    toolCallItem.Status = ResponseMessageStatuses.Completed;
                    toolCallItem.Arguments = toolCall.FunctionArguments?.ToString() ?? "";
                    toolCallItem.Name = toolCall.FunctionName;
                    InputItems.Add(toolCallItem);
                }
                else if (item is ModelFunctionCallOutputItem toolOutput)
                {
                    FunctionToolCallOutput functionToolCallOutput = new FunctionToolCallOutput(toolOutput.CallId, toolOutput.FunctionOutput);
                    functionToolCallOutput.Status = ResponseMessageStatuses.Completed;
                    //functionToolCallOutput.Id = toolOutput.Id;

                    InputItems.Add(functionToolCallOutput);
                }
                else if (item is ModelComputerCallItem computerCall)
                {
                    InputItems.Add(ConvertComputerAction(computerCall));
                }
                else if (item is ModelComputerCallOutputItem computerOutput)
                {
                    InputItems.Add(ConvertComputerOutputToProviderItem(computerOutput));
                }
                else if (item is ModelMessageItem message)
                {
                    ResponseInputMessage inMessage = new ResponseInputMessage(ChatMessageRoles.Assistant, ConverModelContentToProviderContent(message.Content));
                    inMessage.Status = ResponseMessageStatuses.Completed;
                    if (message.Role.ToUpper() == "ASSISTANT")
                    {
                        inMessage.Role = ChatMessageRoles.Assistant;
                    }
                    else if (message.Role.ToUpper() == "USER")
                    {
                        inMessage.Role = ChatMessageRoles.User;
                    }
                    else if (message.Role.ToUpper() == "SYSTEM")
                    {
                        inMessage.Role = ChatMessageRoles.System;
                    }
                    else if (message.Role.ToUpper() == "DEVELOPER")
                    {
                        inMessage.Role = ChatMessageRoles.Unknown;
                    }

                    InputItems.Add(inMessage);
                }
                else
                {
                    throw new ArgumentException($"Unknown ModelItem type: {item.GetType().Name}", nameof(messages));
                }
            }

            return InputItems;
        }

        public ComputerToolCallInput ConvertComputerAction(ModelComputerCallItem computerCall)
        {
            if (computerCall?.Action is null)
                throw new ArgumentNullException(nameof(computerCall));

            // Local helper so we don’t repeat the constructor footprint eight times.
            ComputerToolCallInput Wrap(string id, string callId, IComputerAction action, ResponseMessageStatuses status)
                => new(id, callId, action, ResponseMessageStatuses.Completed);
            
            // Pattern‑matching switch keeps the code compact and type‑safe.
            return computerCall.Action switch
            {
                // -------------------------------------------------------------------------
                // CLICK
                // -------------------------------------------------------------------------
                ComputerToolActionClick clickSrc =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        new ClickAction
                        {
                            Button = clickSrc.MouseButtonClick.ToString()
                                                      .TryParseEnum(out ResponseMouseButton btn)
                                ? btn : ResponseMouseButton.Left,
                            X = clickSrc.MoveCoordinates.X,
                            Y = clickSrc.MoveCoordinates.Y
                        }, 
                        ResponseMessageStatuses.Completed),
                        
                
                // -------------------------------------------------------------------------
                // DOUBLE‑CLICK (no button property in new model – assumes primary button)
                // -------------------------------------------------------------------------
                ComputerToolActionDoubleClick dblSrc =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        new DoubleClickAction
                        {
                            X = dblSrc.MoveCoordinates.X,
                            Y = dblSrc.MoveCoordinates.Y
                        },
                        ResponseMessageStatuses.Completed),

                // -------------------------------------------------------------------------
                // DRAG
                // -------------------------------------------------------------------------
                ComputerToolActionDrag dragSrc =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        new DragAction
                        {
                            Path = new List<DragAction.Coordinate>
                            {
                                new() { X = dragSrc.StartDragLocation.X, Y = dragSrc.StartDragLocation.Y },
                                new() { X = dragSrc.MoveCoordinates.X,   Y = dragSrc.MoveCoordinates.Y   }
                            }
                        },
                        ResponseMessageStatuses.Completed),

                // -------------------------------------------------------------------------
                // KEYPRESS
                // -------------------------------------------------------------------------
                ComputerToolActionKeyPress keySrc =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        new KeyPressAction { Keys = new List<string>(keySrc.KeysToPress) },
                        ResponseMessageStatuses.Completed),

                // -------------------------------------------------------------------------
                // MOVE
                // -------------------------------------------------------------------------
                ComputerToolActionMove moveSrc =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        new MoveAction { X = moveSrc.MoveCoordinates.X, Y = moveSrc.MoveCoordinates.Y },
                        ResponseMessageStatuses.Completed),

                // -------------------------------------------------------------------------
                // SCREENSHOT
                // -------------------------------------------------------------------------
                ComputerToolActionScreenShot =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        new ScreenshotAction(),
                        ResponseMessageStatuses.Completed),

                // -------------------------------------------------------------------------
                // SCROLL
                // -------------------------------------------------------------------------
                ComputerToolActionScroll scrollSrc =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        new ScrollAction
                        {
                            X       = scrollSrc.MoveCoordinates.X,
                            Y       = scrollSrc.MoveCoordinates.Y,
                            ScrollX = scrollSrc.ScrollHorOffset,
                            ScrollY = scrollSrc.ScrollVertOffset
                        },
                        ResponseMessageStatuses.Completed),

                // -------------------------------------------------------------------------
                // TYPE
                // -------------------------------------------------------------------------
                ComputerToolActionType typeSrc =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        new TypeAction { Text = typeSrc.TypeText },
                        ResponseMessageStatuses.Completed),

                // -------------------------------------------------------------------------
                // WAIT
                // -------------------------------------------------------------------------
                ComputerToolActionWait =>
                    Wrap(
                        computerCall.Id,
                        computerCall.CallId,
                        new WaitAction(),
                        ResponseMessageStatuses.Completed),

                // -------------------------------------------------------------------------
                // FALL‑THROUGH: unrecognised action
                // -------------------------------------------------------------------------
                _ => throw new NotSupportedException($"Cannot convert action of type {computerCall.Action.GetType().Name}")
            };
        }

        public ComputerToolCallOutput ConvertComputerOutputToProviderItem(ModelComputerCallOutputItem computerCallOutput)
        {
            ComputerScreenshot ss = new();
            ResponseInputContentImage ssContent = ResponseInputContentImage.CreateImageUrl(computerCallOutput.ScreenShot.ImageURL);
            ss.ImageUrl = ssContent.ImageUrl;
            //ss.FileId = ssContent.FileId;
          
            ComputerToolCallOutput computerToolCallOutput = new ComputerToolCallOutput(computerCallOutput.CallId, ss);
            //computerToolCallOutput.Id = computerCallOutput.Id;

            return computerToolCallOutput;
        }
    }
}
