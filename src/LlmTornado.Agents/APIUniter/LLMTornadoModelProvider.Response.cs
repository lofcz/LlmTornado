using LlmTornado.Responses;
using LlmTornado.Responses.Events;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Agents
{
    public partial class TornadoClient
    {
        /// <summary>
        /// Configures and returns a <see cref="ResponseRequest"/> object based on the provided messages and options.
        /// </summary>
        /// <remarks>This method sets up the response client by converting the last message into a
        /// provider response item, configuring tools, and applying options such as output format and reasoning. It also
        /// considers additional settings like computer use and web search capabilities.</remarks>
        /// <param name="messages">A list of <see cref="ModelItem"/> objects representing the messages to be processed. The last message in the
        /// list is used for the response.</param>
        /// <param name="options">A <see cref="ModelResponseOptions"/> object containing various configuration options for the response, such
        /// as instructions and tools.</param>
        /// <returns>A <see cref="ResponseRequest"/> object configured with the specified messages and options, ready for
        /// processing.</returns>
        public ResponseRequest SetupResponseClient(List<ModelItem> messages, ModelResponseOptions options)
        {
            var InputItems = ConvertToProviderResponseItems(new List<ModelItem>([messages.Last()]));

            ResponseRequest request = new ResponseRequest
            {
                Model = CurrentModel,
                InputItems = InputItems,
                Instructions = options.Instructions
            };

            if (request.Tools == null) request.Tools = new List<ResponseTool>();

            request.PreviousResponseId = options.PreviousResponseId ?? null;
            //Convert Tools here
            foreach (BaseTool tool in options.Tools)
            {
                dynamic? responseFormat = JsonConvert.DeserializeObject<dynamic>(tool.ToolParameters.ToString());

                ResponseFunctionTool rftool = new()
                {
                    Name = tool.ToolName,
                    Description = tool.ToolDescription,
                    Parameters = responseFormat,
                    Strict = tool.FunctionSchemaIsStrict
                };

                request.Tools.Add(rftool);
            }

            //if(VectorSearchOptions != null)
            //{
            //    request.Tools.Add(new ResponseFileSearchTool
            //    {
            //        VectorStoreIds = VectorSearchOptions.VectorIDs
            //    });
            //}

            if (AllowComputerUse)
            {
                Size screenSize = ComputerToolUtility.GetScreenSize();
                request.Tools.Add(new ResponseComputerUseTool
                {
                    DisplayWidth = screenSize.Width,
                    DisplayHeight = screenSize.Height,
                    Environment = ResponseComputerEnvironment.Windows
                });
                request.Truncation = ResponseTruncationStrategies.Auto;

                request.Reasoning = new ReasoningConfiguration()
                {
                    Summary = ResponseReasoningSummaries.Concise
                };
                request.Background = false;
            }

            //Convert Text Format Here
            if (options.OutputFormat != null)
            {
                dynamic? responseFormat = JsonConvert.DeserializeObject<dynamic>(options.OutputFormat.JsonSchema.ToString());

                var config1 = ResponseTextFormatConfiguration.CreateJsonSchema(
                    responseFormat,
                    options.OutputFormat.JsonSchemaFormatName,
                    strict: true);

                request.Text = config1;
            }

            if (options.ReasoningOptions != null)
            {
                request.Reasoning = options.ReasoningOptions;
            }

            if (EnableWebSearch)
            {
                request.Tools.Add(new ResponseWebSearchTool());
            }

            //Need to add Convertion methods to model provider converts for ResponseConverter
            //if (EnableCodeInterpreter)
            //{
            //    request.Tools.Add(new ResponseCodeInterpreterTool());
            //}

            return request;
        }


        public async Task<ModelResponse> StreamingResponseAPIAsync(List<ModelItem> messages, ModelResponseOptions options, StreamingCallbacks streamingCallback = null)
        {
            ResponseRequest request = SetupResponseClient(messages, options);

            ModelResponse ResponseOutput = new();
            ResponseOutput.Model = options.Model;
            ResponseOutput.OutputFormat = options.OutputFormat ?? null;
            ResponseOutput.OutputItems = new List<ModelItem>();
            ResponseOutput.Messages = messages;

            await Client.Responses.StreamResponseRich(request, new ResponseStreamEventHandler
            {
                OnEvent = (data) =>
                {
                    if (data is ResponseEventCreated ResponseEvent)
                    {
                        streamingCallback?.Invoke(new ModelStreamingCreatedEvent(ResponseEvent.SequenceNumber, ResponseEvent.Response.Id));
                        ResponseOutput.Id = ResponseEvent.Response.Id ?? ResponseEvent.Response.PreviousResponseId;
                    }
                    else if (data is ResponseEventReasoningSummaryPartAdded reasoningPartAdded)
                    {
                        streamingCallback?.Invoke(new ModelStreamingReasoningPartAddedEvent(reasoningPartAdded.SequenceNumber, reasoningPartAdded.OutputIndex, reasoningPartAdded.SummaryIndex, reasoningPartAdded.ItemId, reasoningPartAdded.Part.Text));
                    }
                    else if (data is ResponseEventReasoningSummaryPartDone reasoningPartDone)
                    {
                        streamingCallback?.Invoke(new ModelStreamingReasoningPartDoneEvent(reasoningPartDone.SequenceNumber, reasoningPartDone.OutputIndex, reasoningPartDone.SummaryIndex, reasoningPartDone.ItemId, reasoningPartDone.Part.Text));
                    }
                    else if (data is ResponseEventOutputTextDelta delta)
                    {
                        streamingCallback?.Invoke(new ModelStreamingOutputTextDeltaEvent(delta.SequenceNumber, delta.OutputIndex, delta.ContentIndex, delta.Delta, delta.ItemId));
                    }
                    else if (data is ResponseEventOutputItemDone itemDone)
                    {
                        streamingCallback?.Invoke(new ModelStreamingOutputItemDoneEvent(itemDone.SequenceNumber, itemDone.OutputIndex));
                        ResponseOutput.OutputItems.Add(ConvertFromProviderOutputItem(itemDone.Item));
                    }
                    else if (data is ResponseEventCompleted completed)
                    {
                        streamingCallback?.Invoke(new ModelStreamingCompletedEvent(completed.SequenceNumber, completed.Response.PreviousResponseId ?? ""));
                        ResponseOutput.Id = completed.Response.Id ?? completed.Response.PreviousResponseId;
                    }
                    else if (data is ResponseEventError error)
                    {
                        streamingCallback?.Invoke(new ModelStreamingErrorEvent(error.SequenceNumber, error.Message, error.Code));
                    }


                    return ValueTask.CompletedTask;
                }

            });

            return ResponseOutput;
        }
    }
}
