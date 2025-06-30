using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Responses;

namespace LlmTornado.Demo;

public class ResponsesDemo
{
    [TornadoTest]
    public static async Task ResponseSimple()
    {
        ResponseResult? result = await Program.Connect().Responses.CreateResponse(new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt4.Turbo,
            InputString = "how are you?"
        });

        var x = result.Output;
    }
}