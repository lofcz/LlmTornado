using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Agents
{
    public class BasicTornadoRunner
    {
        public async Task BasicTornadoRun()
        {
            LLMTornadoModelProvider client = 
                new(ChatModel.OpenAi.Gpt41.V41Mini,[new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),], true);

            Agent agent = new Agent(client, "You are a useful assistant.");

            RunResult result = await Runner.RunAsync(agent, "what is 2+2");

            Console.WriteLine(result.Text);
        }
    }
}
