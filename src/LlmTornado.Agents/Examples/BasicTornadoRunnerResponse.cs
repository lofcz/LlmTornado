using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Responses;
using NUnit.Framework;
using DescriptionAttribute = System.ComponentModel.DescriptionAttribute;

namespace LlmTornado.Agents
{
    public class BasicTornadoRunnerResponse
    {
        [Test]
        public async Task BasicTornadoRunWithResponse()
        {
            var client = new TornadoApi([new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            TornadoAgent agent = new(
                client,
                ChatModel.OpenAi.Gpt41.V41Mini,
                "You are a useful assistant.");

            agent.ResponseOptions = new ResponseRequest();

            var result = await TornadoRunner.RunAsync(agent, "What is 2+2?");

            Console.WriteLine(result.Messages.Last().Content);
        }

        [Test]
        public async Task BasicTornadoRunWithResponseUsingTools()
        {
            var client = new TornadoApi([new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            TornadoAgent agent = new(
                client,
                ChatModel.OpenAi.Gpt41.V41Mini,
                "You are a useful assistant.",
                tools: [GetCurrentWeather]);

            agent.ResponseOptions = new ResponseRequest();

            var result = await TornadoRunner.RunAsync(agent, "What is 2+2?");

            Console.WriteLine(result.Messages.Last().Content);
        }

        [Description("Get the current weather in a given location")]
        public static string GetCurrentWeather(
            [Description("The city and state, e.g. Boston, MA")] string location)
        {
            // Call the weather API here.
            return $"31 C";
        }
    }
}
