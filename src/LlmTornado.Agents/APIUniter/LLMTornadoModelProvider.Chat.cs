using LlmTornado.Chat;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Responses;
using LlmTornado.Responses.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents
{
    public delegate void TornadoStreamingCallbacks(IResponseEvent streamingResult);
    public partial class TornadoClient
    {
        /// <summary>
        /// Configures the specified <see cref="Conversation"/> instance with the provided messages and options.
        /// </summary>
        /// <remarks>This method sets up the conversation by adding tools, configuring the response
        /// format, and setting the reasoning effort level based on the provided options. It also converts the messages
        /// into a format suitable for the conversation provider.</remarks>
        /// <param name="chat">The <see cref="Conversation"/> instance to be configured.</param>
        /// <param name="messages">A list of <see cref="ModelItem"/> objects representing the messages to be included in the conversation.</param>
        /// <param name="options">The <see cref="ModelResponseOptions"/> containing configuration settings such as tools, output format, and
        /// reasoning options.</param>
        /// <returns>The configured <see cref="Conversation"/> instance with updated request parameters based on the provided
        /// options.</returns>
        public Conversation SetupClient(Conversation chat, List<ModelItem> messages, ModelResponseOptions options)
        {
            //Convert Tools here
            foreach (BaseTool tool in options.Tools)
            {
                if (chat.RequestParameters.Tools == null) chat.RequestParameters.Tools = new List<LlmTornado.Common.Tool>();
                chat.RequestParameters.Tools?.Add(
                    new LlmTornado.Common.Tool(
                        new LlmTornado.Common.ToolFunction(
                            tool.ToolName,
                            tool.ToolDescription,
                            tool.ToolParameters.ToString()), true
                        )
                    );
            }

            //Convert Text Format Here
            if (options.OutputFormat != null)
            {
                dynamic? responseFormat = JsonConvert.DeserializeObject<dynamic>(options.OutputFormat.JsonSchema.ToString());
                chat.RequestParameters.ResponseFormat = ChatRequestResponseFormats.StructuredJson(options.OutputFormat.JsonSchemaFormatName, responseFormat);
            }

            if (options.ReasoningOptions != null)
            {
                chat.RequestParameters.ResponseRequestParameters = new ResponseRequest
                {
                    Reasoning = new ReasoningConfiguration
                    {
                        Effort = ResponseReasoningEfforts.Medium,
                        Summary = ResponseReasoningSummaries.Auto
                    }
                };
            }

            chat = ConvertToProviderItems(messages, chat);

            return chat;
        }

        public async Task<ModelResponse> HandleStreaming(Conversation chat, List<ModelItem> messages, ModelResponseOptions options, TornadoStreamingCallbacks streamingCallback = null)
        {
            ModelResponse ResponseOutput = new();
            ResponseOutput.Model = options.Model;
            ResponseOutput.OutputFormat = options.OutputFormat ?? null;
            ResponseOutput.OutputItems = new List<ModelItem>();
            ResponseOutput.Messages = messages;

            //Create Open response
            await chat.StreamResponseRich(new ChatStreamEventHandler
            {
                OnResponseEvent = (data) =>
                {
                    streamingCallback?.Invoke(data);

                    if (data is ResponseEventCreated ResponseEvent)
                    {
                        ResponseOutput.Id = ResponseEvent.Response.Id ?? ResponseEvent.Response.PreviousResponseId;
                    }
                    else if (data is ResponseEventOutputItemDone itemDone)
                    {
                        ResponseOutput.OutputItems.Add(ConvertFromProviderOutputItem(itemDone.Item));
                    }
                    else if (data is ResponseEventCompleted completed)
                    {
                        ResponseOutput.Id = completed.Response.Id ?? completed.Response.PreviousResponseId;
                    }

                    return ValueTask.CompletedTask;
                },
                MessageTokenExHandler = (exText) =>
                {
                    //Call the streaming callback for text
                    return ValueTask.CompletedTask;
                },
                ImageTokenHandler = (image) =>
                {
                    //Call the streaming callback for image
                    return ValueTask.CompletedTask;
                },
                MessageTokenHandler = (text) =>
                {
                    streamingCallback?.Invoke(new ResponseEventOutputTextDelta() { ContentIndex=0,OutputIndex=0,SequenceNumber=0,Delta=text});
                    return ValueTask.CompletedTask;
                },
                ReasoningTokenHandler = (reasoning) =>
                {
                    return ValueTask.CompletedTask;
                },
                BlockFinishedHandler = (message) =>
                {
                    //Call the streaming callback for completion
                    streamingCallback?.Invoke(new ResponseEventCompleted());
                    ResponseOutput.OutputItems.Add(ConvertFromProviderItem(message));
                    return ValueTask.CompletedTask;
                },
                MessagePartHandler = (part) =>
                {
                    return ValueTask.CompletedTask;
                },
                FunctionCallHandler = (toolCall) =>
                {
                    foreach (FunctionCall call in toolCall)
                    {
                        //Add the tool call to the response output
                        ResponseOutput.OutputItems.Add(new ModelFunctionCallItem(
                            call.ToolCall?.Id!,
                            call.ToolCall?.Id!,
                            call.Name,
                            ModelStatus.InProgress,
                            BinaryData.FromString(call.Arguments)
                            ));
                    }
                    return ValueTask.CompletedTask;
                },
                MessageTypeResolvedHandler = (messageType) =>
                {
                    return ValueTask.CompletedTask;
                },
                MutateChatRequestHandler = (request) =>
                {
                    streamingCallback?.Invoke(new ResponseEventCreated());
                    //Mutate the request if needed
                    return ValueTask.FromResult(request);
                },
                HttpExceptionHandler = (exception) =>
                {
                    new ResponseEventError() { Message = exception.Exception.Message, Code = exception.Result.Code.ToString()};
                    //Handle any exceptions that occur during streaming
                    return ValueTask.CompletedTask;
                }
            });

            return ResponseOutput;
        }

    }
}
