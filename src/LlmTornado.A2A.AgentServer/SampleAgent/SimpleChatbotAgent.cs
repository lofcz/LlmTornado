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

public class SimpleChatbotAgent : OrchestrationRuntimeConfiguration
{
    public SimpleChatbotAgent()
    {
        TornadoApi client = new TornadoApi(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

        BuildSimpleAgent(client, true, "AgentV10.json");
    }

    public void BuildSimpleAgent(TornadoApi client, bool streaming = false, string conversationFile = "SimpleAgent.json")
    {

        SimpleAgentRunnable simpleAgentRunnable = new SimpleAgentRunnable(client, this, streaming);

       new OrchestrationBuilder(this)
           .SetEntryRunnable(simpleAgentRunnable)
           .SetOutputRunnable(simpleAgentRunnable)
           .WithRuntimeInitializer((config) =>
           {
               simpleAgentRunnable.OnAgentRunnerEvent += (sEvent) =>
               {
                   // Forward agent runner events (including streaming) to runtime
                   config.OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, config.Runtime?.Id ?? string.Empty));
               };

               simpleAgentRunnable.OnAgentToolPermissionEvent += async (tool) =>
               {
                   if (config.OnRuntimeRequestEvent != null)
                   {
                       return await config.OnRuntimeRequestEvent.Invoke(tool);
                   }
                   return false;
               };

               return ValueTask.CompletedTask;
           })
           .WithRuntimeProperty("LatestUserMessage", "")
           .WithChatMemory(conversationFile)
           .AddExitPath<ChatMessage>(simpleAgentRunnable, _ => true)
           .Build();
    }
}

public class SimpleAgentRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
{
    TornadoAgent Agent;
    public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }
    public Func<string, ValueTask<bool>>? OnAgentToolPermissionEvent { get; set; }

    Conversation _conv;
    OrchestrationRuntimeConfiguration _runtimeConfiguration;
    public SimpleAgentRunnable(TornadoApi client, OrchestrationRuntimeConfiguration orchestrator, bool streaming = false) : base(orchestrator)
    {
        string instructions = @"You are a conversational chatbot, be engaging and creative to have a playful and interesting conversation with the user.
Given the following context will include Vector Search Memory, Websearch Results, and Entity Memory to keep track of real world things.";

        Agent = new TornadoAgent(
            client: client,
            model: ChatModel.OpenAi.Gpt5.V5Mini,
            name: "Agent Runner",
            instructions: instructions,
            streaming: streaming);

        _conv = Agent.Client.Chat.CreateConversation(Agent.Options);

        Agent.ResponseOptions = new ResponseRequest() { Tools = [new ResponseWebSearchTool()] };
        _runtimeConfiguration = orchestrator;
    }


    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<ChatMessage, ChatMessage> process)
    {
        process.RegisterAgent(Agent);

        _conv = await RunAgent();

        return _conv.Messages.Last();
    }

    public async Task<Conversation> RunAgent()
    {
        List<ChatMessage> messages = _runtimeConfiguration.GetMessages(); //Includes latest user message
        return await Agent.RunAsync(
            appendMessages: messages,
            streaming: Agent.Streaming,
            onAgentRunnerEvent: (sEvent) =>
            {
                OnAgentRunnerEvent?.Invoke(sEvent);
                return ValueTask.CompletedTask;
            },

            toolPermissionHandle: OnAgentToolPermissionEvent
            );
    }
}

