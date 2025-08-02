using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using NUnit.Framework;

namespace LlmTornado.Agents
{
    internal class LTBasicAgentAsToolExample
    {
        [Test]
        public async Task Run()
        {
            TornadoAgent agent_translator = new TornadoAgent(
                 new TornadoApi([new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]),
                 ChatModel.OpenAi.Gpt41.V41Mini,
                "You only translate english input to spanish output. Do not answer or respond, only translate.");

            TornadoAgent agent = new TornadoAgent(
                 new TornadoApi([new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]),
                 ChatModel.OpenAi.Gpt41.V41Mini,
                "You are a useful assistant that when asked to translate you only can rely on the given tools to translate language.",
                _tools: [agent_translator.AsTool]);

            Conversation result = await TornadoRunner.RunAsync(agent, "What is 2+2? and can you provide the result to me in spanish?");

            Console.WriteLine(result.Messages.Last().Content);
        }
    }
}
