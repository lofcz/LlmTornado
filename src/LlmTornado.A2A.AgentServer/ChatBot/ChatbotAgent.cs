using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Embedding;
using LlmTornado.Embedding.Models;
using LlmTornado.Infra;
using LlmTornado.Moderation;
using LlmTornado.Responses;

using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace LlmTornado.A2A.AgentServer;

public class ChatBotAgent : OrchestrationRuntimeConfiguration
{
    public ChatBotAgent()
    {
        TornadoApi client = new TornadoApi(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

        BuildSimpleAgent(client, true, "AgentV10.json");
    }

    public OrchestrationRuntimeConfiguration BuildSimpleAgent(TornadoApi client, bool streaming = false, string conversationFile = "SimpleAgent.json")
    {
        ModeratorRunnable inputModerator = new ModeratorRunnable(client, this);

        AgentRunnable simpleAgentRunnable = new AgentRunnable(client, this, streaming);

        return new OrchestrationBuilder(this)
           .SetEntryRunnable(inputModerator)
           .SetOutputRunnable(simpleAgentRunnable)
           .WithRuntimeInitializer((config) =>
           {
               simpleAgentRunnable.OnAgentRunnerEvent += (sEvent) =>
               {
                   // Forward agent runner events (including streaming) to runtime
                   config.OnRuntimeEvent?.Invoke(new ChatRuntimeAgentRunnerEvents(sEvent, config.Runtime?.Id ?? string.Empty));
               };
               return ValueTask.CompletedTask;
           })
           .WithRuntimeProperty("LatestUserMessage", "")
           .WithChatMemory(conversationFile)
           .AddAdvancer<ChatMessage>(inputModerator, simpleAgentRunnable)
           .AddExitPath<ChatMessage>(simpleAgentRunnable, _ => true)
           .CreateDotGraphVisualization("SimpleChatBotAgent.dot").Build();
    }
}


public class ModeratorRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
{
    TornadoApi Client { get; set; }
    public ModeratorRunnable(TornadoApi client, Orchestration orchestrator) : base(orchestrator)
    {
        Client = client;
    }

    public override async ValueTask<ChatMessage> Invoke(RunnableProcess<ChatMessage, ChatMessage> input)
    {
        await ThrowOnModeratedInput(input.Input, Client);

        try
        {
            Orchestrator?.RuntimeProperties.AddOrUpdate("LatestUserMessage", (newValue) => input.Input.Content ?? "", (key, Value) => input.Input.Content ?? "");
        }
        catch(Exception e) {
            Console.WriteLine(e.Message);
            throw;
        }

        return input.Input;
    }

    private async Task ThrowOnModeratedInput(ChatMessage Input, TornadoApi Client)
    {
        // Moderate input content by OpenAI Moderation API Standards
        if (Input.Content is not null)
        {
            ModerationResult modResult = await Client.Moderation.CreateModeration(Input.Content);
            if (modResult.Results.FirstOrDefault()?.Flagged == true)
            {
                throw new Exception("Input content was flagged by moderation.");
            }
        }

        foreach (ChatMessagePart part in Input.Parts ?? [])
        {
            if (part.Text is not null)
            {
                ModerationResult modResult = await Client.Moderation.CreateModeration(part.Text);
                if (modResult.Results.FirstOrDefault()?.Flagged == true)
                {
                    throw new Exception("Input content was flagged by moderation.");
                }
            }
        }
    }
}

public class AgentRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
{
    TornadoAgent Agent;
    public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }

    Conversation _conv;
    OrchestrationRuntimeConfiguration _runtimeConfiguration;
    public AgentRunnable(TornadoApi client, OrchestrationRuntimeConfiguration orchestrator, bool streaming = false) : base(orchestrator)
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

        List<ChatMessage> messages = _runtimeConfiguration.GetMessages();
        messages.Add(process.Input);

        _conv = await Agent.RunAsync(
            appendMessages: messages,
            streaming: Agent.Streaming,
            onAgentRunnerEvent: (sEvent) =>
            {
                OnAgentRunnerEvent?.Invoke(sEvent);
                return ValueTask.CompletedTask;
            });

        return _conv.Messages.Last();
    }
}

