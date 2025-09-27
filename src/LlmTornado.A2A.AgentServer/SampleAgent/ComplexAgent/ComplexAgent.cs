using LlmTornado.A2A.AgentServer.SampleAgent.ComplexAgent.States;
using LlmTornado.A2A.AgentServer.SampleAgent.States;
using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Moderation;
using LlmTornado.Responses;
using LlmTornado.Responses.Events;

namespace LlmTornado.A2A.AgentServer;

public class ComplexAgent : OrchestrationRuntimeConfiguration
{
    public ComplexAgent()
    {
        TornadoApi client = new TornadoApi(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

        BuildAgent(client, true, "AgentV10.json");
    }

    public void BuildAgent(TornadoApi client, bool streaming = false, string conversationFile = "SimpleAgent.json")
    {
        ModeratorRunnable inputModerator = new ModeratorRunnable(client, this);
        EnvironmentControllerRunnable environmentControllerRunnable = new EnvironmentControllerRunnable(client, this, streaming);
        AgentRunnable agentRunnable = new AgentRunnable(client, this, streaming);

       new OrchestrationBuilder(this)
           .SetEntryRunnable(inputModerator)
           .SetOutputRunnable(agentRunnable)
           .WithRuntimeInitializer((config) =>
           {
               agentRunnable.OnAgentRunnerEvent += (sEvent) =>
               {
                   // Forward agent runner events (including streaming) to runtime
                   config.OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, config.Runtime?.Id ?? string.Empty));
               };

               return ValueTask.CompletedTask;
           })
           .WithRuntimeProperty("LatestUserMessage", "")
           .WithChatMemory(conversationFile)
           .AddAdvancer<ChatMessage>(inputModerator, environmentControllerRunnable)
           .AddAdvancer<ChatMessage>(environmentControllerRunnable, agentRunnable)
           .AddExitPath<ChatMessage>(agentRunnable, _ => true)
           .Build();
    }
  
}

