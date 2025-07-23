using LlmTornado.Chat.Models;
using LlmTornado.Code;
using System.Text.Json;
using System.ComponentModel;

namespace LlmTornado.Agents
{
    internal class LTBasicStructuredOutputsExample
    {
        public async Task RunBasicStructuredOutputExample()
        {
            LLMTornadoModelProvider client =
              new(ChatModel.OpenAi.Gpt41.V41Mini, [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),],true);

            Agent agent = new Agent(
                client,
                "Have fun",
                _output_schema: typeof(math_reasoning));

            RunResult result = await Runner.RunAsync(agent, "How can I solve 8x + 7 = -23?");

            //The easy way
            //Helper function to avoid doing the hard way
            math_reasoning mathResult = result.ParseJson<math_reasoning>();
            Console.WriteLine(mathResult.ToString());
            //The hard way (I mean I'm not telling you what to do..)
            math_reasoning mathResult2 = new math_reasoning();
            if (result.Response.OutputItems.LastOrDefault() is ModelMessageItem message)
            {
                Console.WriteLine($"[{message.Role}] {message?.Text}");

                using JsonDocument structuredJson = JsonDocument.Parse(message?.Text);
                mathResult2.final_answer = structuredJson.RootElement.GetProperty("final_answer").GetString()!;
                Console.WriteLine($"Final answer: {mathResult2.final_answer}");
                Console.WriteLine("Reasoning steps:");

                JsonElement.ArrayEnumerator steps = structuredJson.RootElement.GetProperty("steps").EnumerateArray();

                mathResult2.steps = new math_step[steps.Count()];
                int i = 0;
                foreach (JsonElement stepElement in steps)
                {
                    mathResult2.steps[i].explanation = stepElement.GetProperty("explanation").GetString() ?? "";
                    mathResult2.steps[i].output = stepElement.GetProperty("output").GetString() ?? "";
                    
                    Console.WriteLine($"  - Explanation: {mathResult2.steps[i].explanation}");
                    Console.WriteLine($"    Output: {mathResult2.steps[i].output}");

                    i++;
                }
            }
        }

        [System.ComponentModel.Description("Explain the solution steps to a math problem")]
        public struct math_reasoning
        {
            [System.ComponentModel.Description("Steps to complete the Math Problem")]
            public math_step[] steps { get; set; }

            [System.ComponentModel.Description("Final Result to math problem")]
            public string final_answer{ get; set; }

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

        [System.ComponentModel.Description("bad description")]
        public struct math_step
        {
            [System.ComponentModel.Description("Explanation of the math step")]
            public string explanation { get; set; }

            [System.ComponentModel.Description("Result of the step")]
            public string output { get; set; }
        }
    }
}
