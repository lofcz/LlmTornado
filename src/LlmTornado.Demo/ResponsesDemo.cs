using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Responses;


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
}