using LlmTornado.Agents;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using System.ComponentModel;
using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.Orchestration;
using LlmTornado.Agents.Orchestration.Core;

namespace LlmTornado.Demo;

public class AgentsDemo : DemoBase
{
    [TornadoTest]
    public static async Task BasicTornadoRun()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, "You are a useful assistant.");

        Conversation result = await agent.RunAsync("What is 2+2?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    [TornadoTest]
    public static async Task RunHelloWorldStreaming()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, "Have fun");

        // Enhanced streaming callback to handle the new ModelStreamingEvents system
        ValueTask StreamingHandler(ModelStreamingEvents streamingEvent)
        {
            switch (streamingEvent.EventType)
            {
                case ModelStreamingEventType.OutputTextDelta:
                    if (streamingEvent is ModelStreamingOutputTextDeltaEvent deltaEvent)
                    {
                        Console.Write(deltaEvent.DeltaText); // Write the text delta directly
                    }
                    break;
                default:
                    break;
            }
            return ValueTask.CompletedTask;
        }

        Conversation result = await agent.RunAsync("Hello Streaming World!", streaming: true, streamingCallback: StreamingHandler);
    }

    [TornadoTest]
    public static async Task BasicAgentAsToolExample()
    {
        TornadoAgent agentTranslator = new TornadoAgent(
            Program.Connect(),
            ChatModel.OpenAi.Gpt41.V41Mini,
            "You only translate english input to spanish output. Do not answer or respond, only translate.");

        TornadoAgent agent = new TornadoAgent(
            Program.Connect(),
            ChatModel.OpenAi.Gpt41.V41Mini,
            "You are a useful assistant that when asked to translate you only can rely on the given tools to translate language.",
            tools: [agentTranslator.AsTool]);

        Conversation result = await agent.RunAsync("What is 2+2? and can you provide the result to me in spanish?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    public struct IsMath
    {
        public string Reasoning { get; set; }
        public bool IsMathRequest { get; set; }
    }
    
    [TornadoTest]
    public static async Task<GuardRailFunctionOutput> MathGuardRail()
    {
        TornadoAgent mathGuardrail = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, "Check if the user is asking you a Math related question.", outputSchema: typeof(IsMath));

        Conversation result = await TornadoRunner.RunAsync(mathGuardrail, "What is the weather?", singleTurn: true);

        IsMath? isMath = result.Messages.Last().Content.JsonDecode<IsMath>();

        return new GuardRailFunctionOutput(isMath?.Reasoning ?? "", !isMath?.IsMathRequest ?? false);
    }

    [Description("Explain the solution steps to a math problem")]
    public struct MathReasoning
    {
        [Description("Steps to complete the Math Problem")]
        public MathStep[] Steps { get; set; }

        [Description("Final Result to math problem")]
        public string FinalAnswer { get; set; }

        public void ConsoleWrite()
        {
            Console.WriteLine($"Final answer: {FinalAnswer}");
            Console.WriteLine("Reasoning steps:");
            foreach (MathStep step in Steps)
            {
                Console.WriteLine($"  - Explanation: {step.Explanation}");
                Console.WriteLine($"    Output: {step.Output}");
            }
        }
    }

    [Description("bad description")]
    public struct MathStep
    {
        [Description("Explanation of the math step")]
        public string Explanation { get; set; }

        [Description("Result of the step")]
        public string Output { get; set; }
    }

    [TornadoTest]
    public static async Task RunBasicStructuredOutputExample()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, "Have fun", outputSchema: typeof(MathReasoning));

        Conversation result = await TornadoRunner.RunAsync(agent, "How can I solve 8x + 7 = -23?");

        //The easy way
        //Helper function to avoid doing the hard way
        MathReasoning mathResult = result.Messages.Last().Content.JsonDecode<MathReasoning>();
        mathResult.ConsoleWrite();
    }

    [TornadoTest]
    public static async Task RunBasicTornadoToolUse()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(),
            ChatModel.OpenAi.Gpt41.V41Mini,
            "You are a useful assistant.",
            tools: [GetCurrentWeather],
            outputSchema: typeof(MathReasoning));

        Conversation result = await TornadoRunner.RunAsync(agent, "What is the weather in boston?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    [TornadoTest]
    public static async Task RunBasicTornadoAgentToolValueTaskUse()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(),
            ChatModel.OpenAi.Gpt41.V41Mini,
            "You are a useful assistant.",
            tools: [GetCurrentWeatherValueTask],
            outputSchema: typeof(MathReasoning));

        Conversation result = await TornadoRunner.RunAsync(agent, "What is the weather in boston?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    public enum Unit
    {
        Celsius, 
        Fahrenheit
    }

    [Description("Get the current weather in a given location")]
    public static string GetCurrentWeather(
        [Description("The city and state, e.g. Boston, MA")] string location,
        [SchemaIgnore] Unit unit = Unit.Celsius)
    {
        // Call the weather API here.
        return $"31 C";
    }

    public static async ValueTask<string> GetCurrentWeatherValueTask(
        [Description("The city and state, e.g. Boston, MA")] string location,
        [SchemaIgnore] Unit unit = Unit.Celsius)
    {
        // Call the weather API here.
        return $"31 C";
    }

    [TornadoTest]
    public static async Task HandoffTest()
    {
        TornadoAgent TechnicalExpertAgent = new TornadoAgent(Program.Connect(),ChatModel.OpenAi.Gpt41.V41Mini, name: "GeneralSupport", "You are a technical expert for the Sales team.",  description: "Handoff If the customer ask a technical question");
        TornadoAgent BillingAgent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, name: "BillingExpert", "You are a billing expert for the Sales team.",description: "Handoff if the customer has a billing request");

        TornadoAgent GeneralSupportAgent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41, 
            instructions: $"""You are General support for the Sales team""",
            handoffs: [
                TechnicalExpertAgent,
                TechnicalExpertAgent
                ]);

        HandoffOrchestration handoffOrchestrator = new(GeneralSupportAgent);
        // Example task to run through the state machine
        string task = "What is the status of my technical support ticket?";
        Console.WriteLine($"[User]: {task}");
        Console.Write("[Agent]: ");
        
        // Run the state machine and aget the result
        await handoffOrchestrator.Invoke(task);

        Console.Write(handoffOrchestrator.Results.LastOrDefault().Parts.LastOrDefault().Text ?? "No response from agent");
    }
}