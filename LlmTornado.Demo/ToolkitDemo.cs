
using LlmTornado.Chat.Models;

namespace LlmTornado.Demo;

public class ToolkitDemo
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
            new ChatFunctionParam("items", new ChatFunctionTypeListTypedObject("aggregated items", true, [
                new ChatFunctionParam("name", "name of the item", true, ChatFunctionAtomicParamTypes.String),
                new ChatFunctionParam("quantity", "aggregated quantity", true, ChatFunctionAtomicParamTypes.Int),
                new ChatFunctionParam("known_name", new ChatFunctionTypeEnum("known name of the item", true, [ "apple", "cherry", "orange", "other" ]))
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