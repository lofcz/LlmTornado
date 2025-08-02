using LlmTornado.Chat.Models;
using LlmTornado.Code;
using NUnit.Framework;

namespace LlmTornado.Agents
{
    public class BasicTornadoRunner
    {
        [Test]
        public async Task BasicTornadoRun()
        {
            var client = new TornadoApi([new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]);

            TornadoAgent agent = new(
                client,
                ChatModel.OpenAi.Gpt41.V41Mini,
                "You are a useful assistant.");

            var result = await TornadoRunner.RunAsync(agent, "What is 2+2?");

            Console.WriteLine(result.Messages.Last().Content);
        }
    }
}
