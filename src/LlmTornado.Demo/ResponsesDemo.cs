using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Images;
using LlmTornado.Images.Models;
using LlmTornado.Responses;
using LlmTornado.Responses.Events;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Demo;

public class ResponsesDemo : DemoBase
{
    [TornadoTest]
    public static async Task ResponseSimpleText()
    {
        ResponseResult result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41Mini,
            Instructions = "You are a helpful assistant",
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, "how are you?")
            ],
            Include = [ 
                ResponseIncludeFields.MessageOutputTextLogprobs
            ]
        });

        ResponseOutputMessageItem itm = result.Output.OfType<ResponseOutputMessageItem>().FirstOrDefault();
        Assert.That(result.Output.OfType<ResponseOutputMessageItem>().Count(), Is.EqualTo(1));

        ResponseOutputTextContent? text = itm.Content.OfType<ResponseOutputTextContent>().FirstOrDefault();
        Console.WriteLine(text.Text);
    }
    
    public struct math_reasoning
    {
        public math_step[] steps { get; set; }
        public string final_answer { get; set; }

        public void ConsoleWrite()
        {
            Console.WriteLine($"Final answer: {final_answer}");
            Console.WriteLine("Reasoning steps:");
            foreach (math_step step in steps)
            {
                Console.WriteLine($"  - Explanation: {step.explanation}");
                Console.WriteLine($"    Output: {step.output}");
            }
        }
    }

    public struct math_step
    {
        public string explanation { get; set; }
        public string output { get; set; }
    }
    
    [TornadoTest]
    public static async Task ResponseStructuredOutput()
    {
        ResponseResult response = await Program.Connect().Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Instructions = "You are an assistant specialized on solving math problems.",
            Text = ResponseTextFormatConfiguration.CreateJsonSchema(new
            {
                type = "object",
                properties = new
                {
                    final_answer = new
                    {
                        type = "string",
                        description = "final answer to the problem"
                    },
                    steps = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "object",
                            properties = new
                            {
                                explanation = new
                                {
                                    type = "string",
                                    description = "explanation of the step, curt"
                                },
                                output = new
                                {
                                    type = "string",
                                    description = "output of the step"
                                }
                            },
                            required = new List<string> { "explanation", "output" },
                            additionalProperties = false
                        }
                    }
                },
                required = new List<string> { "final_answer", "steps" },
                additionalProperties = false
            }, "math_solver", strict: true),
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, [
                    new ResponseInputContentText("2x + 4 - x = 8")
                ])
            ]
        });
        
        foreach (IResponseOutputItem outputItem in response.Output)
        {
            if (outputItem is ResponseOutputMessageItem msg)
            {
                foreach (IResponseOutputContent part in msg.Content)
                {
                    if (part is ResponseOutputTextContent text)
                    {
                        // the output JSON
                        Console.WriteLine(text.Text);
                    }
                }
            }
        }
    }
    
    [TornadoTest]
    public static async Task ResponseSimpleTextUsingChat()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
            {
                Model = ChatModel.OpenAi.Gpt41.V41Mini,
                MaxTokens = 2000,
                ResponseRequestParameters = new ResponseRequest()
            })
            .AppendSystemMessage("You are a helpful assistant")
            .AppendUserInput([
                new ChatMessagePart("What kind of dog breed is this?"),
                new ChatMessagePart(
                    "https://as2.ftcdn.net/v2/jpg/05/94/28/01/1000_F_594280104_vZXB6JZANIRywkZcUQntU07p5KGpuZ7S.jpg",
                    ImageDetail.Auto)
            ]);

        TornadoRequestContent z = chat.Serialize();
        
        RestDataOrException<ChatRichResponse> response = await chat.GetResponseRichSafe();
        
        Console.WriteLine(response.Data.Text);
    }
    
    [TornadoTest]
    public static async Task ResponseStructuredJsonUsingChat()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
            {
                Model = ChatModel.OpenAi.Gpt41.V41Mini,
                MaxTokens = 2000,
                ResponseRequestParameters = new ResponseRequest(),
                ResponseFormat = ChatRequestResponseFormats.StructuredJson("get_translation", new
                {
                    type = "object",
                    properties = new
                    {
                        translation = new
                        {
                            type = "string",
                        }
                    },
                    required = new List<string> { "translation" },
                    additionalProperties = false
                })
            })
            .AppendSystemMessage("You are a helpful assistant that translates from English to French")
            .AppendUserInput([new ChatMessagePart("Hello, how are you?")]);
        
        RestDataOrException<ChatRichResponse> response = await chat.GetResponseRichSafe();
        
        Console.WriteLine(response.Data.Text);
    }
    
    [TornadoTest]
    public static async Task ResponseJsonUsingChat()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
            {
                Model = ChatModel.OpenAi.Gpt41.V41Mini,
                MaxTokens = 2000,
                ResponseRequestParameters = new ResponseRequest(),
                ResponseFormat = ChatRequestResponseFormats.Json
            })
            .AppendSystemMessage("You are a helpful assistant that translates from English to French")
            .AppendUserInput([new ChatMessagePart("Hello, how are you? (respond in JSON format in property french_translation)")]);
        
        RestDataOrException<ChatRichResponse> response = await chat.GetResponseRichSafe();
        
        Console.WriteLine(response.Data.Text);
    }
    
    [TornadoTest]
    public static async Task StreamResponseSimpleTextUsingChat()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
            {
                Model = ChatModel.OpenAi.O4.V4Mini,
                MaxTokens = 4000,
                ResponseRequestParameters = new ResponseRequest
                {
                    Reasoning = new ReasoningConfiguration
                    {
                        Effort = ResponseReasoningEfforts.Medium,
                        Summary = ResponseReasoningSummaries.Auto
                    }
                }
            })
            .AppendSystemMessage("You are a helpful assistant")
            .AppendUserInput([
                new ChatMessagePart("How to explain theory of relativity to a 15 years old student?")
            ]);
        
        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            OnResponseEvent = (evt) =>
            {
                // "evt" is IResponseEvent
                return ValueTask.CompletedTask;
            },
            MessageTokenHandler = (delta) =>
            {
                Console.Write(delta);
                return ValueTask.CompletedTask;
            },
            ReasoningTokenHandler = (reasoningDelta) =>
            {
                Console.Write(reasoningDelta.Content);
                return ValueTask.CompletedTask;
            }
        });
    }
    
    [TornadoTest]
    public static async Task ResponseStreamingToolsUsingChat()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Tools =
            [
                new Tool(new ToolFunction("get_weather", "gets the current weather in a given city", new
                {
                    type = "object",
                    properties = new
                    {
                        location = new
                        {
                            type = "string",
                            description = "The city for which the weather information is required."
                        }
                    },
                    required = new List<string> { "city" }
                }))
            ],
            ResponseRequestParameters = new ResponseRequest()
        });
        
        chat.OnAfterToolsCall = async (result) =>
        {
            Console.WriteLine();
            await GetNextResponse();
        };
        
        chat.AppendUserInput([
            new ChatMessagePart("Check the weather today in Paris and Prague")
        ]);

        await GetNextResponse();

        async Task GetNextResponse()
        {
            await chat.StreamResponseRich(new ChatStreamEventHandler
            {
                FunctionCallHandler = (fns) =>
                {
                    foreach (FunctionCall fn in fns)
                    {
                        fn.Result = fn.Arguments.Contains("Prague") ? new FunctionResult(fn.Name, "Sunny all day with occasionally clouds") : new FunctionResult(fn.Name, "A mild rain is expected around noon.");
                    }
                
                    return ValueTask.CompletedTask;
                },
                MessageTokenHandler = (token) =>
                {
                    Console.Write(token);
                    return ValueTask.CompletedTask;
                },
                BlockFinishedHandler = (block) =>
                {
                    Console.WriteLine();
                    return ValueTask.CompletedTask;
                }
            });
        }
    }
    
    [TornadoTest]
    public static async Task ResponseChatToolsUsingChat()
    {
        Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            ResponseRequestParameters = new ResponseRequest(),
            Tools = [
                new Tool(new ToolFunction("get_weather", "gets the current weather", new
                {
                    type = "object",
                    properties = new
                    {
                        location = new
                        {
                            type = "string",
                            description = "The location for which the weather information is required."
                        }
                    },
                    required = new List<string> { "location" }
                }), false)
            ],
            ToolChoice = new OutboundToolChoice("get_weather")
        });

        chat.OnAfterToolsCall = async (result) =>
        {
            chat.RequestParameters.ToolChoice = null; // stop forcing the model to use the get_weather tool
            RestDataOrException<ChatRichResponse> responseResult = await chat.GetResponseRichSafe();
            Console.WriteLine(responseResult.Data.Text);;
        };
        
        chat.AppendMessage(ChatMessageRoles.System, "You are a helpful assistant");
        Guid msgId = Guid.NewGuid();
        chat.AppendMessage(ChatMessageRoles.User, "Fetch the weather information for Prague and Paris.", msgId);

        RestDataOrException<ChatRichResponse> response = await chat.GetResponseRichSafe(functions =>
        {
            foreach (FunctionCall fn in functions)
            {
                fn.Result = fn.Arguments.Contains("Prague") ? new FunctionResult(fn.Name, "Sunny all day") : new FunctionResult(fn.Name, "A mild rain is expected around noon.");
            }

            return ValueTask.CompletedTask;
        });

        string r = response.Data.GetText();
        Console.WriteLine(r);
    }
    
    [TornadoTest]
    public static async Task ResponseSimpleTool()
    {
        ResponseResult result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, "What is the weather in prague?")
            ],
            Tools =
            [
                new ResponseFunctionTool
                {
                    Name = "get_weather",
                    Description = "fetches weather in a given city",
                    Parameters = JObject.FromObject(new
                    {
                        type = "object",
                        properties = new
                        {
                            location = new
                            {
                                type = "string",
                                description = "name of the location"
                            }
                        },
                        required = new List<string> { "location" },
                        additionalProperties = false
                    }),
                    Strict = true
                }
            ]
        });

        ResponseFunctionToolCallItem? fn = result.Output.OfType<ResponseFunctionToolCallItem>().FirstOrDefault();
        Assert.That(fn, Is.NotNull);
        
        result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
        {
            PreviousResponseId = result.Id,
            Model = ChatModel.OpenAi.Gpt41.V41,
            InputItems =
            [
                new FunctionToolCallOutput(fn.CallId, new
                {
                    weather = "sunny, no rain, mild fog, humididy: 65%",
                    confidence = "very_high"
                }.ToJson())
            ]
        });

        ResponseOutputMessageItem? itm = result.Output.OfType<ResponseOutputMessageItem>().FirstOrDefault();
        Assert.That(itm, Is.NotNull);

        ResponseOutputTextContent? text = itm.Content.OfType<ResponseOutputTextContent>().FirstOrDefault();
        Assert.That(text, Is.NotNull);
        
        Console.WriteLine(text.Text);
    }

    [TornadoTest]
    public static async Task ResponseSimpleTextStream()
    {
        await Program.Connect().Responses.StreamResponseRich(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41Mini,
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, "How are you?")
            ]
        }, new ResponseStreamEventHandler
        {
            OnEvent = (data) =>
            {
                if (data is ResponseEventOutputTextDelta delta)
                {
                    Console.Write(delta.Delta);
                }
                
                return ValueTask.CompletedTask;
            }
        });
    }
    
    [TornadoTest]
    public static async Task ResponseSimpleFunctionsStream()
    {
        string fnCallId = string.Empty;
        
        ResponsesSession session = Program.Connect().Responses.CreateSession(new ResponseStreamEventHandler
        {
            OnEvent = (data) =>
            {
                if (data is ResponseEventOutputTextDelta delta)
                {
                    Console.Write(delta.Delta);
                }

                if (data is ResponseEventOutputItemDone itemDone)
                {
                    if (itemDone.Item is ResponseFunctionToolCallItem fn)
                    {
                        // call the function
                        fnCallId = fn.CallId;
                    }
                }
                
                return ValueTask.CompletedTask;
            }
        });

        await session.StreamResponseRich(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, "What is the weather in prague?")
            ],
            Tools =
            [
                new ResponseFunctionTool
                {
                    Name = "get_weather",
                    Description = "fetches weather in a given city",
                    Parameters = JObject.FromObject(new
                    {
                        type = "object",
                        properties = new
                        {
                            location = new
                            {
                                type = "string",
                                description = "name of the location"
                            }
                        },
                        required = new List<string> { "location" },
                        additionalProperties = false
                    }),
                    Strict = true
                }
            ]
        });
        
        await session.StreamResponseRich(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            InputItems = [
                new FunctionToolCallOutput(fnCallId, new
                {
                    weather = "sunny, no rain, mild fog, humididy: 65%",
                    confidence = "very_high"
                }.ToJson())
            ]
        });
    }
    
    [TornadoTest]
    public static async Task ResponseDeepResearchBackground()
    {
        ResponseResult result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.O4.V4MiniDeepResearch,
            Background = true,
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, "Research detailed information about latest development in the Ukraine war and predict how long will Pokrovsk hold.")
            ],
            Tools = [
                new ResponseWebSearchTool(),
                new ResponseCodeInterpreterTool()
            ]
        });

        int z = 0;
    }
    
    [TornadoTest]
    public static async Task ResponseBackground()
    {
        ResponseResult result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.O4.V4Mini,
            Background = true,
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, "2+2=?")
            ]
        });

        Console.WriteLine(result.Id);
    }
    
    [TornadoTest]
    public static async Task ResponseComputerTool()
    {
        EndpointBase.SetRequestsTimeout(20000);
        
        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/empty.jpg");
        string base64 = $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
        
        ResponseResult result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Codex.ComputerUsePreview,
            Background = false,
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, [
                    new ResponseInputContentText("Check the latest OpenAI news on google.com."),
                    ResponseInputContentImage.CreateImageUrl(base64),
                ])
            ],
            Tools = [
                new ResponseComputerUseTool
                {
                    DisplayWidth = 2560,
                    DisplayHeight = 1440,
                    Environment = ResponseComputerEnvironment.Windows
                }
            ],
            Reasoning = new ReasoningConfiguration
            {
                Summary = ResponseReasoningSummaries.Concise
            },
            Truncation = ResponseTruncationStrategies.Auto
        });

        int z = 0;
    }
    
    [TornadoTest]
    public static async Task ResponseFileSearch()
    {
        EndpointBase.SetRequestsTimeout(20000);
        
        ResponseResult result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Background = false,
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, [
                    new ResponseInputContentText("Summarize all available files. Do not ask for further input."),
                ])
            ],
            Include = [ 
                ResponseIncludeFields.FileSearchCallResults
            ],
            Tools = [
                new ResponseFileSearchTool
                {
                    VectorStoreIds = [ "vs_6869bbe2a93481919d52952ac7773144" ]
                }
            ]
        });

        Console.WriteLine(result.OutputText);
        
        int z = 0;
    }
    
    [TornadoTest]
    public static async Task ResponseReasoning()
    {
        EndpointBase.SetRequestsTimeout(20000);
        
        ResponseResult result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.O4.V4Mini,
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, [
                    new ResponseInputContentText("Write a bash script that takes a matrix represented as a string with format \"[1,2],[3,4],[5,6]\" and prints the transpose in the same format.")
                ])
            ],
            Reasoning = new ReasoningConfiguration
            {
                Effort = ResponseReasoningEfforts.Medium
            },
            Include = [ 
                ResponseIncludeFields.ReasoningEncryptedContent 
            ],
            Store = false
        });

        Console.WriteLine(result.OutputText);
        int z = 0;
    }
    
    [TornadoTest]
    public static async Task ResponseReasoningStreaming()
    {
        EndpointBase.SetRequestsTimeout(20000);

        await Program.Connect().Responses.StreamResponseRich(new ResponseRequest
        {
            Model = ChatModel.OpenAi.O4.V4Mini,
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, [
                    new ResponseInputContentText("Write a bash script that takes a matrix represented as a string with format \"[1,2],[3,4],[5,6]\" and prints the transpose in the same format.")
                ])
            ],
            Reasoning = new ReasoningConfiguration
            {
                Effort = ResponseReasoningEfforts.Medium
            },
            Include =
            [
                ResponseIncludeFields.ReasoningEncryptedContent
            ],
            Store = false
        }, new ResponseStreamEventHandler
        {
            OnEvent = (data) =>
            {
                if (data.EventType is ResponseEventTypes.ResponseOutputTextDelta && data is ResponseEventOutputTextDelta delta)
                {
                    Console.Write(delta.Delta);
                }
                
                return ValueTask.CompletedTask;
            }
        });

        int z = 0;
    }
    
    [TornadoTest, Flaky("long running")]
    public static async Task ResponseDeepResearchMcp()
    {
        EndpointBase.SetRequestsTimeout(20000);
        
        ResponseResult result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41,
            Background = false,
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, "Research detailed information about latest development in the Ukraine war and predict how long will Pokrovsk hold. Create some images describing the situation. Run python code for quantitative analysis. Please send e-mail analysis to EMAIL using the MCP.")
            ],
            Tools = [
                new ResponseWebSearchTool(),
                new ResponseCodeInterpreterTool(),
                new ResponseMcpTool
                {
                    ServerLabel = "mailgun_mcp",
                    ServerUrl = "https://mcp.pipedream.net/id/mailgun",
                    RequireApproval = ResponseMcpRequireApprovalOption.Never
                },
                new ResponseImageGenerationTool
                {
                    Model = ImageModel.OpenAi.Gpt.V1
                }
            ]
        });

        int z = 0;
    }

    [TornadoTest]
    public static async Task ResponseLocalShellTool()
    {
        ResponseRequest req = new ResponseRequest
        {
            Model = ChatModel.OpenAi.Codex.MiniLatest,
            Background = false,
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, "List files in the current directory?")
            ],
            Tools = [
                new ResponseLocalShellTool()
            ]
        };

        ResponseResult result = await Program.Connect().Responses.CreateResponse(req);

        Assert.That(result.Output.OfType<ResponseLocalShellToolCallItem>().Count(), Is.GreaterThan(0));

        ResponseRequest req2 = new ResponseRequest
        {
            Model = ChatModel.OpenAi.Codex.MiniLatest,
            Background = false,
            InputItems = [
                new LocalShellCallOutput(result.Output.OfType<ResponseLocalShellToolCallItem>().First().CallId, "helloWorld.txt")
            ],
            Tools = [
                new ResponseLocalShellTool()
            ],
            PreviousResponseId = result.Id
        };

        ResponseResult result2 = await Program.Connect().Responses.CreateResponse(req2);

        int z = 0;
    }

    [TornadoTest, Flaky("only for dev")]
    public static async Task Deserialize()
    {
        string text = await File.ReadAllTextAsync("Static/Json/Sensitive/response1.json");
        ResponseResult result = text.JsonDecode<ResponseResult>();
        string data = result.ToJson();
        int z = 0;
    }
    
    [TornadoTest, Flaky("only for dev")]
    public static async Task ResponseDeepResearchBackgroundGet()
    {
        ResponseResult? result = await Program.Connect().Responses.GetResponse("<id>");

        int z = 0;
    }
    
    [TornadoTest]
    public static async Task ResponseDelete()
    {
        TornadoApi api = Program.Connect();
     
        ResponseResult createResult = await api.Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.O4.V4Mini,
            Background = true,
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, "2+2=?")
            ]
        });
        
        ResponseDeleted result = await api.Responses.DeleteResponse(createResult.Id);
        Assert.That(result.Deleted, Is.True);
    }
    
    [TornadoTest]
    public static async Task ResponseCancel()
    {
        TornadoApi api = Program.Connect();
     
        ResponseResult createResult = await api.Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.O4.V4Mini,
            Background = true,
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, "2+2=?")
            ]
        });
        
        ResponseResult result = await api.Responses.CancelResponse(createResult.Id);
        int z = 0;
    }
    
    [TornadoTest]
    public static async Task ResponseListItems()
    {
        TornadoApi api = Program.Connect();
        ResponseRequest request = new ResponseRequest
        {
            Model = ChatModel.OpenAi.O4.V4Mini,
            Background = true,
            InputItems =
            [
                new ResponseInputMessage(ChatMessageRoles.User, [
                    new ResponseInputContentText("2+2")
                ])
            ]
        };
        
        ResponseResult createResult = await api.Responses.CreateResponse(request);
        ListResponse<ResponseInputItem> result = await api.Responses.ListResponseInputItems(createResult.Id, new ListQuery(100));
        int z = 0;
    }
    
    [TornadoTest]
    public static async Task ResponseReusablePromptList()
    {
        TornadoApi api = Program.Connect();
        ResponseRequest request = new ResponseRequest
        {
            Model = ChatModel.OpenAi.O4.V4Mini,
            Background = true,
            Instructions = "You are a helpful assistant",
            Prompt = new PromptConfiguration
            {
                Id = "pmpt_686bb61c674081979cef4c95e2baaa570e95896814dffabf",
                Variables = new Dictionary<string, IPromptVariable>
                {
                    { "imagename", new PromptVariableString("cat") },
                    { "image", new PromptVariableString("test") }
                }
            },
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, [
                    new ResponseInputContentText("Can you describe it?")
                ])
            ]
        };
        
        ResponseResult createResult = await api.Responses.CreateResponse(request);
        ListResponse<ResponseInputItem> result = await api.Responses.ListResponseInputItems(createResult.Id, new ListQuery(100));
    }
    
    [TornadoTest]
    public static async Task ResponseReusablePrompt()
    {
        TornadoApi api = Program.Connect();
        ResponseRequest request = new ResponseRequest
        {
            Model = ChatModel.OpenAi.O4.V4Mini,
            Prompt = new PromptConfiguration
            {
                Id = "pmpt_686bb61c674081979cef4c95e2baaa570e95896814dffabf",
                Variables = new Dictionary<string, IPromptVariable>
                {
                    { "imagename", new PromptVariableString("cats") },
                    { "image", new PromptVariableString(string.Empty) }
                },
                Version = "2"
            },
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, [
                    new ResponseInputContentText("What are you expert on?")
                ])
            ]
        };
        
        ResponseResult createResult = await api.Responses.CreateResponse(request);
        Console.WriteLine(createResult.OutputText);
    }
    
    [TornadoTest]
    public static async Task ResponseReusablePromptComplex()
    {
        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/catBoi.jpg");
        string base64 = $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
        
        TornadoApi api = Program.Connect();
        ResponseRequest request = new ResponseRequest
        {
            Model = ChatModel.OpenAi.O4.V4Mini,
            Prompt = new PromptConfiguration
            {
                Id = "pmpt_686bb61c674081979cef4c95e2baaa570e95896814dffabf",
                Variables = new Dictionary<string, IPromptVariable>
                {
                    { "imagename", new PromptVariableString("cats") },
                    { 
                        "image", 
                        new ResponseInputContentImage
                        {
                            ImageUrl = base64
                        } 
                    }
                },
                Version = "2"
            },
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, [
                    new ResponseInputContentText("Can you describe the image?")
                ])
            ]
        };
        
        ResponseResult createResult = await api.Responses.CreateResponse(request);
        Console.WriteLine(createResult.OutputText);
    }
}