using LlmTornado.Chat.Models;
using LlmTornado.Code;
using NUnit.Framework;
using static LlmTornado.Demo.TornadoTextFixture;

namespace LlmTornado.Agents
{
    
    public class LTBasicGuardRailExample
    {
        [Test]
        public async Task Run()
        {
            await TestPassingGuardRail();

        }

        public async Task TestPassingGuardRail()
        {
            TornadoAgent agent = new(
                new TornadoApi([new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]),
                ChatModel.OpenAi.Gpt41.V41Mini,
                "You are a useful assistant.");

            var result = await TornadoRunner.RunAsync(agent, "What is 2+2?", guard_rail: MathGuardRail);

            Console.WriteLine(result.Messages.Last().Content);
        }

        async Task TestFailingGuardRail()
        {
            TornadoAgent agent = new(
               new TornadoApi([new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]),
               ChatModel.OpenAi.Gpt41.V41Mini,
               "You are a useful assistant.");

            var result = await TornadoRunner.RunAsync(agent, "What is the weather in boston?", guard_rail: MathGuardRail);

            Console.WriteLine(result.Messages.Last().Content);
        }

        public struct IsMath
        {
            public string Reasoning {  get; set; }
            public bool is_math_request {  get; set; }
        }

        public async Task<GuardRailFunctionOutput> MathGuardRail(string input = "")
        {
            string oInfo = "";
            bool trigger = false;

            if (string.IsNullOrWhiteSpace(input)) return new GuardRailFunctionOutput(oInfo, trigger);

            TornadoAgent weather_guardrail = new(
               new TornadoApi([new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY")!),]),
               ChatModel.OpenAi.Gpt41.V41Mini,
                "Check if the user is asking you a Math related question.", 
                _output_schema: typeof(IsMath));

            var result = await TornadoRunner.RunAsync(weather_guardrail, input, single_turn:true);

            IsMath? isMath = result.Messages.Last().Content.ParseJson<IsMath>();

            return new GuardRailFunctionOutput(isMath?.Reasoning ?? "", !isMath?.is_math_request ?? false);
        }
    }
}
