using LlmTornado.Chat.Models;
using LlmTornado.Infra;


namespace LlmTornado.Demo;

public class ToolkitDemo : DemoBase
{
    class DemoAggregatedItem
    {
        public string Name { get; set; }
        public string KnownName { get; set; }
        public int Quantity { get; set; }
    }
    
    [TornadoTest]
    [Flaky("manual test")]
    public static async Task ToolkitChatFunction()
    {
        await ToolkitChat.GetSingleResponse(Program.Connect(), ChatModel.Google.Gemini.Gemini2Flash001, ChatModel.OpenAi.Gpt41.V41Mini, "aggregate items by type", new ChatFunction([
            new ToolParam("items", new ToolParamList("aggregated items", [
                new ToolParam("name", "name of the item", ToolParamAtomicTypes.String),
                new ToolParam("quantity", "aggregated quantity", ToolParamAtomicTypes.Int),
                new ToolParam("known_name", new ToolParamEnum("known name of the item", [ "apple", "cherry", "orange", "other" ]))
            ]))
        ], async (args, ctx) =>
        {
            if (!args.ParamTryGet("items", out List<DemoAggregatedItem>? items) || items is null)
            {
                return new ChatFunctionCallResult(ChatFunctionCallResultParameterErrors.MissingRequiredParameter, "items");
            }
            
            Console.WriteLine("Aggregated items:");

            foreach (DemoAggregatedItem item in items)
            {
                Console.WriteLine($"{item.Name}: {item.Quantity}");
            }
            
            return new ChatFunctionCallResult();
        }), "three apples, one cherry, two apples, one orange, one orange");
    }
}