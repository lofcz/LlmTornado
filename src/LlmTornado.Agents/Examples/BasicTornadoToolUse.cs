using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Chat.Models;
using NUnit.Framework;
using System.ComponentModel;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;
namespace LlmTornado.Agents
{
    internal class BasicTornadoToolUse
    {
        [Test]
        public async Task RunBasicTornadoToolUse()
        {

            TornadoAgent agent = new TornadoAgent(new TornadoApi([new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]),
                ChatModel.OpenAi.Gpt41.V41Mini,
                "You are a useful assistant.",
                _tools: [GetCurrentWeather]);

            var result = await TornadoRunner.RunAsync(agent, "What is the weather in boston?");

            Console.WriteLine(result.Messages.Last().Content);
        }

        public enum Unit { celsius, fahrenheit }

        [Description("Get the current weather in a given location")]
        public static string GetCurrentWeather(
            [Description("The city and state, e.g. Boston, MA")] string location,
            [Description("The temperature unit to use. Infer this from the specified location.")] Unit unit = Unit.celsius)
        {
            // Call the weather API here.
            return $"31 C";
        }
    }
}
