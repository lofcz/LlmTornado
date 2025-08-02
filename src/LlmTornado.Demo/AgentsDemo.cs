using LlmTornado.Agents;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LlmTornado.Demo
{
    public class AgentsDemo : DemoBase
    {
        [TornadoTest]
        public static async Task BasicTornadoRun()
        {
            TornadoAgent agent = new(
                Program.Connect(),
                ChatModel.OpenAi.Gpt41.V41Mini,
                "You are a useful assistant.");

            var result = await TornadoRunner.RunAsync(agent, "What is 2+2?");

            Console.WriteLine(result.Messages.Last().Content);
        }

        [TornadoTest]
        public static async Task RunHelloWorldStreaming()
        {
            TornadoAgent agent = new(
                Program.Connect(),
                ChatModel.OpenAi.Gpt41.V41Mini,
                "Have fun");

            // Enhanced streaming callback to handle the new ModelStreamingEvents system
            void StreamingHandler(ModelStreamingEvents streamingEvent)
            {
                switch (streamingEvent.EventType)
                {
                    case ModelStreamingEventType.Created:
                        Console.WriteLine($"[STREAMING] Stream created (Sequence: {streamingEvent.SequenceId})");
                        break;

                    case ModelStreamingEventType.OutputTextDelta:
                        if (streamingEvent is ModelStreamingOutputTextDeltaEvent deltaEvent)
                        {
                            Console.Write(deltaEvent.DeltaText); // Write the text delta directly
                        }
                        break;

                    case ModelStreamingEventType.Completed:
                        Console.WriteLine($"\n[STREAMING] Stream completed (Sequence: {streamingEvent.SequenceId})");
                        break;

                    case ModelStreamingEventType.Error:
                        if (streamingEvent is ModelStreamingErrorEvent errorEvent)
                        {
                            Console.WriteLine($"\n[STREAMING] Error: {errorEvent.ErrorMessage}");
                        }
                        break;

                    case ModelStreamingEventType.ReasoningPartAdded:
                        if (streamingEvent is ModelStreamingReasoningPartAddedEvent reasoningEvent)
                        {
                            Console.WriteLine($"\n[REASONING] {reasoningEvent.DeltaText}");
                        }
                        break;

                    default:
                        Console.WriteLine($"[STREAMING] Event: {streamingEvent.EventType} (Status: {streamingEvent.Status})");
                        break;
                }
            }

            var result = await TornadoRunner.RunAsync(agent, "Hello Streaming World!", streaming: true, streamingCallback: StreamingHandler);
        }

        [TornadoTest]
        public static async Task BasicAgentAsToolExample()
        {
            TornadoAgent agent_translator = new TornadoAgent(
                 Program.Connect(),
                 ChatModel.OpenAi.Gpt41.V41Mini,
                "You only translate english input to spanish output. Do not answer or respond, only translate.");

            TornadoAgent agent = new TornadoAgent(
                 Program.Connect(),
                 ChatModel.OpenAi.Gpt41.V41Mini,
                "You are a useful assistant that when asked to translate you only can rely on the given tools to translate language.",
                tools: [agent_translator.AsTool]);

            Conversation result = await TornadoRunner.RunAsync(agent, "What is 2+2? and can you provide the result to me in spanish?");

            Console.WriteLine(result.Messages.Last().Content);
        }

        public struct IsMath
        {
            public string Reasoning { get; set; }
            public bool is_math_request { get; set; }
        }


        [TornadoTest]
        public static async Task<GuardRailFunctionOutput> MathGuardRail()
        {
            TornadoAgent math_guardrail = new(
               Program.Connect(),
               ChatModel.OpenAi.Gpt41.V41Mini,
                "Check if the user is asking you a Math related question.",
                outputSchema: typeof(IsMath));

            var result = await TornadoRunner.RunAsync(math_guardrail, "What is the weather?", singleTurn: true);

            IsMath? isMath = result.Messages.Last().Content.ParseJson<IsMath>();

            return new GuardRailFunctionOutput(isMath?.Reasoning ?? "", !isMath?.is_math_request ?? false);
        }

        [Description("Explain the solution steps to a math problem")]
        public struct math_reasoning
        {
            [Description("Steps to complete the Math Problem")]
            public math_step[] steps { get; set; }

            [Description("Final Result to math problem")]
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

        [Description("bad description")]
        public struct math_step
        {
            [Description("Explanation of the math step")]
            public string explanation { get; set; }

            [Description("Result of the step")]
            public string output { get; set; }
        }

        [TornadoTest]
        public static async Task RunBasicStructuredOutputExample()
        {
            TornadoAgent agent = new(
                Program.Connect(),
                ChatModel.OpenAi.Gpt41.V41Mini,
                "Have fun",
                outputSchema: typeof(math_reasoning));

            var result = await TornadoRunner.RunAsync(agent, "How can I solve 8x + 7 = -23?");

            //The easy way
            //Helper function to avoid doing the hard way
            math_reasoning mathResult = result.Messages.Last().Content.ParseJson<math_reasoning>();
            Console.WriteLine(mathResult.ToString());
        }

       
        public async Task RunBasicTornadoToolUse()
        {

            TornadoAgent agent = new TornadoAgent(Program.Connect(),
                ChatModel.OpenAi.Gpt41.V41Mini,
                "You are a useful assistant.",
                tools: [GetCurrentWeather]);

            var result = await TornadoRunner.RunAsync(agent, "What is the weather in boston?");

            Console.WriteLine(result.Messages.Last().Content);
        }

        public enum Unit { celsius, fahrenheit }

        [Description("Get the current weather in a given location")]
        public static string GetCurrentWeather(
            [Description("The city and state, e.g. Boston, MA")] string location,
            [IgnoreParam] Unit unit = Unit.celsius)
        {
            // Call the weather API here.
            return $"31 C";
        }

    }
}
