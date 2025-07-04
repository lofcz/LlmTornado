using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Responses;
using LlmTornado.Responses.Events;


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
}