using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Demo.ExampleAgents.ChatBot;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LlmTornado.Demo;

public class DotNetLiveDemos : DemoBase
{
    [TornadoTest]
    public static async Task BasicTornadoAgentRun()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, instructions: "You are a useful assistant.");

        Conversation result = await agent.RunAsync("What is 2+2?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    #region Streaming
    //Streaming callback to handle the new ModelStreamingEvents system
    public static ValueTask runStreamingEventHandler(AgentRunnerEvents runEvent)
    {
        switch (runEvent.EventType)
        {
            //Some of the event types you might want to handle
            case AgentRunnerEventTypes.ToolInvoked:break;
            case AgentRunnerEventTypes.GuardRailTriggered:break;
            case AgentRunnerEventTypes.Error: break;
            case AgentRunnerEventTypes.Streaming:
                if (runEvent is AgentRunnerStreamingEvent streamingEvent)
                {
                    if (streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                    {
                        Console.Write(deltaTextEvent.DeltaText); // Write the text delta directly
                    }
                }
                break;
            default:
                break;
        }
        return ValueTask.CompletedTask;
    }


    [TornadoTest]
    public static async Task RunHelloWorldStreaming()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, instructions: "You are a useful assistant.");

        Conversation result = await agent.RunAsync("Hello Streaming World!", streaming: true, onAgentRunnerEvent: runStreamingEventHandler);
    }

    #endregion

    #region Structured Output
    [Description("Explain the solution steps to a math problem")]
    public struct MathReasoning
    {
        [Description("Steps to complete the Math Problem")]
        public MathStep[] Steps { get; set; }

        [Description("Final Result to math problem")]
        public string FinalAnswer { get; set; }

        public void ConsoleWrite()
        {
            Console.WriteLine($"Final answer: {FinalAnswer}");
            Console.WriteLine("Reasoning steps:");
            foreach (MathStep step in Steps)
            {
                Console.WriteLine($"  - Explanation: {step.Explanation}");
                Console.WriteLine($"    Output: {step.Output}");
            }
        }
    }

    [Description("bad description")]
    public struct MathStep
    {
        [Description("Explanation of the math step")]
        public string Explanation { get; set; }

        [Description("Result of the step")]
        public string Output { get; set; }
    }

    //Output schema is used to define the expected output format from the model
    //Automatically handles output parsing and validation
    [TornadoTest]
    public static async Task RunBasicStructuredOutputExample()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, instructions: "Have fun", outputSchema: typeof(MathReasoning));

        Conversation result = await agent.RunAsync("How can I solve 8x + 7 = -23?");

        MathReasoning mathResult = result.Messages.Last().Content.JsonDecode<MathReasoning>();

        mathResult.ConsoleWrite();
    }
    #endregion

    #region Tools

    [JsonConverter(typeof(StringEnumConverter))]
    public enum Unit
    {
        Celsius,
        Fahrenheit
    }

    [Description("Get the current weather in a given location")]
    public static string GetCurrentWeather(
        [Description("The city and state, e.g. Boston, MA")] string location,
        [Description("unit of temperature measurement in C or F")] Unit unit = Unit.Celsius)
    {
        // Call the weather API here.
        return $"31 C";
    }

    [TornadoTest]
    public static async Task RunBasicTornadoToolUse()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(),
            ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant.",
            tools: [GetCurrentWeather]);

        Conversation result = await agent.RunAsync("What is the weather in boston?");

        Console.WriteLine(result.Messages.Last().Content);
    }
    #endregion

    #region Runtime

    #region Handoff Agent
    [TornadoTest]
    public static async Task BasicHandoffRuntimeDemo()
    {
        HandoffAgent translatorAgent = new HandoffAgent(
            client: Program.Connect(),
            name: "SpanishAgent",
            model: ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant. Please only respond in spanish",
            description: "Use this Agent for spanish speaking response");

        HandoffAgent usefulAgent = new HandoffAgent(
             client: Program.Connect(),
             name: "EnglishAgent",
             model: ChatModel.OpenAi.Gpt41.V41Mini,
             instructions: "You are a useful assistant. Please only respond in english",
             description: "Use this Agent for english speaking response",
             handoffs: [translatorAgent]);

        translatorAgent.HandoffAgents = [usefulAgent];

        HandoffRuntimeConfiguration runtimeConfiguration = new HandoffRuntimeConfiguration(usefulAgent);

        ChatRuntime runtime = new ChatRuntime(runtimeConfiguration);

        ChatMessage report = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, "¿cuanto es 2+2?"));

        Console.WriteLine(report.Content);
    }
    #endregion


    #region Orchestration Agent Runtime

    public class SimpleAgentRunnable : OrchestrationRunnable<ChatMessage, ChatMessage>
    {
        TornadoAgent Agent;
        public Action<AgentRunnerEvents>? OnAgentRunnerEvent { get; set; }

        OrchestrationRuntimeConfiguration _runtimeConfiguration;

        Conversation _conv;

        public SimpleAgentRunnable(TornadoApi client, OrchestrationRuntimeConfiguration orchestrator, bool streaming = false) : base(orchestrator)
        {
            string instructions = @"You are a friendly chatbot. Given the following context and users prompt generate a response to the user that is helpful and informative.";

            Agent = new TornadoAgent(
                client: client,
                model: ChatModel.OpenAi.Gpt5.V5Mini,
                name: "Assistant",
                instructions: instructions,
                streaming: streaming);

            _runtimeConfiguration = orchestrator;
        }

        public override async ValueTask<ChatMessage> Invoke(RunnableProcess<ChatMessage, ChatMessage> process)
        {
            process.RegisterAgent(Agent);

            _conv = await Agent.RunAsync(
                appendMessages: _runtimeConfiguration.GetMessages(),
                streaming: Agent.Streaming,
                onAgentRunnerEvent: (sEvent) =>
                {
                    OnAgentRunnerEvent?.Invoke(sEvent);
                    return ValueTask.CompletedTask;
                });

            return _conv.Messages.Last();
        }
    }


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

            SimpleAgentRunnable simpleAgentRunnable = new SimpleAgentRunnable(client, this, streaming);

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
        #endregion

        #endregion

    }
