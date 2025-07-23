using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Chat.Models;

namespace LlmTornado.Agents
{
    internal class BasicTornadoToolUse
    {
        public async Task RunBasicTornadoToolUse()
        {
            LLMTornadoModelProvider client = new(
                ChatModel.OpenAi.Gpt41.V41Mini,
                [new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], true);

            Agent agent = new Agent(client,
                "You are a useful assistant.",
                _tools: [GetCurrentWeather]);

            RunResult result = await Runner.RunAsync(agent, "What is the weather in boston?");

            Console.WriteLine(result.Text);
        }

        public enum Unit { celsius, fahrenheit }

        [Tool(
            Description = "Get the current weather in a given location",
            In_parameters_description = [
                "The city and state, e.g. Boston, MA",
                "The temperature unit to use. Infer this from the specified location."
                ])]
        public static string GetCurrentWeather(string location, Unit unit = Unit.celsius)
        {
            // Call the weather API here.
            return $"31 C";
        }
    }
}
