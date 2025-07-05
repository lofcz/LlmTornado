using LlmTornado.Chat.Models;
using LlmTornado.Code;
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
        ResponseResult? result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt41.V41Mini,
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
    
    [TornadoTest]
    public static async Task ResponseSimpleTool()
    {
        ResponseResult? result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
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
                if (data is ResponseOutputTextDeltaEvent delta)
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
                if (data is ResponseOutputTextDeltaEvent delta)
                {
                    Console.Write(delta.Delta);
                }

                if (data is ResponseOutputItemDoneEvent itemDone)
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
        ResponseResult? result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
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
    public static async Task ResponseComputerTool()
    {
        EndpointBase.SetRequestsTimeout(20000);
        
        byte[] bytes = await File.ReadAllBytesAsync("Static/Images/empty.jpg");
        string base64 = $"data:image/jpeg;base64,{Convert.ToBase64String(bytes)}";
        
        ResponseResult? result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Codex.ComputerUsePreview,
            Background = false,
            InputItems = [
                new ResponseInputMessage(ChatMessageRoles.User, [
                    new ResponseInputContentText("Check the latest OpenAI news on google.com."),
                    ResponseInputContentImage.CreateImageUrl(base64)
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
    
    [TornadoTest, Flaky("long running")]
    public static async Task ResponseDeepResearchMcp()
    {
        EndpointBase.SetRequestsTimeout(20000);
        
        ResponseResult? result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
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
}