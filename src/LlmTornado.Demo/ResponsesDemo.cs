using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
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
        
        Assert.That(result.Output.OfType<OutputMessageItem>().Count(), Is.EqualTo(1));
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
        ResponsesSession session = Program.Connect().Responses.CreateSession(new ResponseRequest
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
        }, new ResponseStreamEventHandler
        {
            OnSse = (sse) =>
            {
                return ValueTask.CompletedTask;
            },
            OnEvent = (data) =>
            {
                if (data is ResponseOutputTextDeltaEvent delta)
                {
                    Console.Write(delta.Delta);
                }

                if (data is ResponseOutputItemDoneEvent itemDone)
                {
                    if (itemDone.Item is FunctionToolCallItem fn)
                    {
                        // call the function
                    }
                }
                
                return ValueTask.CompletedTask;
            }
        });

        await session.StreamNext();
        //await session.StreamNext();
    }
}