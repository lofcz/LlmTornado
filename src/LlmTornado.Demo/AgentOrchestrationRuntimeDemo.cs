using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.Samples;
using LlmTornado.Agents.Samples.ChatBot;
using LlmTornado.Agents.Utility;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Demo.ExampleAgents;
using LlmTornado.Demo.ExampleAgents.ChatBot;
using LlmTornado.Demo.ExampleAgents.CSCodingAgent;
using LlmTornado.Demo.ExampleAgents.MagenticOneAgent;
using LlmTornado.Demo.ExampleAgents.ResearchAgent;
using LlmTornado.Demo.ExampleAgents.SimpleAgent;
using LlmTornado.Responses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LlmTornado.Demo;

public class AgentOrchestrationRuntimeDemo : DemoBase
{
    [TornadoTest]
    [Flaky("manual interaction")]
    public static async Task MemoryChatBotAgentRuntimeDemo()
    {
        MemoryChatBot RuntimeConfiguration = new MemoryChatBot(Program.Connect());

        ChatRuntime runtime = new ChatRuntime(RuntimeConfiguration);

        RuntimeConfiguration.OnRuntimeEvent = async (evt) =>
        {
            if (evt.EventType == ChatRuntimeEventTypes.AgentRunner)
            {
                if (evt is ChatRuntimeAgentRunnerEvents runnerEvt)
                {
                    if (runnerEvt.AgentRunnerEvent is AgentRunnerStreamingEvent streamEvt)
                    {
                        if (streamEvt.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                        {
                            Console.Write(deltaTextEvent.DeltaText);
                        }
                    }
                }
            }
            await ValueTask.CompletedTask;
        };

        Console.WriteLine("Ask a Question");

        string topic = "";
        while (topic != "exit")
        {
            Console.Write("\n[User]: ");
            topic = Console.ReadLine();
            if (topic == "exit") break;
            Console.Write("\n[Assistant]: ");
            ChatMessage report = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, topic));
        }
    }

    #region ResearchAgent
    [TornadoTest]
    public static async Task ResearchAgentRuntimeDemo()
    {
        ResearchAgentConfiguration RuntimeConfiguration = new ResearchAgentConfiguration(Program.Connect());

        ChatRuntime runtime = new ChatRuntime(RuntimeConfiguration);
        RuntimeConfiguration.OnRuntimeEvent = async (evt) =>
        {
            Console.WriteLine($"Event: {evt.EventType}");
            if (evt.EventType == ChatRuntimeEventTypes.AgentRunner)
            {
                if (evt is ChatRuntimeAgentRunnerEvents runnerEvt)
                {
                    if (runnerEvt.AgentRunnerEvent is AgentRunnerStreamingEvent streamEvt)
                    {
                        if (streamEvt.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                        {
                            Console.Write(deltaTextEvent.DeltaText);
                        }
                    }
                }
            }
            await ValueTask.CompletedTask;
        };
        Console.WriteLine("[Assistant]: What do you want to research?");
        Console.Write("[User]: ");
        string topic = Console.ReadLine();
        ChatMessage report = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, topic ?? "Write a report about the benefits of using AI agents."));

        Console.WriteLine(report.Parts.Last().Text);
    }

    [TornadoTest]
    public static async Task BasicOrchestrationRuntimeResearchStreamingDemo()
    {
        ResearchAgentConfiguration RuntimeConfiguration = new ResearchAgentConfiguration();
        
        RuntimeConfiguration.RecordSteps = true;

        ChatRuntime runtime = new ChatRuntime(RuntimeConfiguration);

        RuntimeConfiguration.OnRuntimeEvent = async (evt) =>
        {
            if (evt.EventType == ChatRuntimeEventTypes.AgentRunner)
            {
                if (evt is ChatRuntimeAgentRunnerEvents runnerEvt)
                {
                    if (runnerEvt.AgentRunnerEvent is AgentRunnerStreamingEvent streamEvt)
                    {
                        if (streamEvt.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                        {
                            Console.Write(deltaTextEvent.DeltaText);
                        }
                    }
                }
            }
            await ValueTask.CompletedTask;
        };
        Console.WriteLine("[Assistant]: What do you want to research?");
        Console.Write("[User]: ");
        string topic = Console.ReadLine();
        Console.Write("[Assistant]: ");
        ChatMessage report = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, topic));

        RunnerRecordVisualizationUtility.SaveRunnerRecordDotGraphToFileAsync(RuntimeConfiguration.RunSteps.ToDictionary(), "ResearchAgentRecord.dot", "ResearchAgentRecord");
    }

    [TornadoTest]
    public static async Task SimpleChatbotDemo()
    {
        ChatBotAgent chatbotConfig = new ChatBotAgent();
        OrchestrationRuntimeConfiguration config = chatbotConfig.BuildSimpleAgent(Program.Connect(), streaming: true, conversationFile: "Conversation1.json");
        ChatRuntime runtime = new ChatRuntime(config);

        runtime.RuntimeConfiguration.OnRuntimeEvent += async (evt) =>
        {
            if (evt.EventType == ChatRuntimeEventTypes.AgentRunner)
            {
                if (evt is ChatRuntimeAgentRunnerEvents runnerEvt)
                {
                    if (runnerEvt.AgentRunnerEvent is AgentRunnerStreamingEvent streamEvt)
                    {
                        if (streamEvt.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                        {
                            Console.Write(deltaTextEvent.DeltaText);
                        }
                    }
                }
            }
            else if (evt.EventType == ChatRuntimeEventTypes.Orchestration)
            {
                if (evt is ChatRuntimeOrchestrationEvent orchestrationEvt)
                {
                    if (orchestrationEvt.OrchestrationEventData is OnVerboseOrchestrationEvent verbose)
                    {
                        Console.WriteLine(verbose.Message);
                    }
                }
            }
                await ValueTask.CompletedTask;
        };

        Console.WriteLine("[Assistant]: Hello");
        string topic = "";
        while (topic != "exit")
        {
            Console.Write("[User]: ");
            topic = Console.ReadLine();
            if (topic == "exit") break;
            Console.Write("[Assistant]: ");
            ChatMessage report = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, topic));
            Console.WriteLine();
        }
    }

    [TornadoTest]
    public static async Task ComplexChatbotDemo()
    {
        ChatBotAgent chatbotConfig = new ChatBotAgent();

        OrchestrationRuntimeConfiguration config = chatbotConfig.BuildComplexAgent(
            client: Program.Connect(),
            streaming: true, 
            chromaUri:"http://localhost:8001/api/v2/", 
            conversationFile: "Conversation1.json",
            withLongtermMemoryID: "AgentV10");

        ChatRuntime runtime = new ChatRuntime(config);

        runtime.RuntimeConfiguration.OnRuntimeEvent += async (evt) =>
        {
            if (evt.EventType == ChatRuntimeEventTypes.AgentRunner)
            {
                if (evt is ChatRuntimeAgentRunnerEvents runnerEvt)
                {
                    if (runnerEvt.AgentRunnerEvent is AgentRunnerStreamingEvent streamEvt)
                    {
                        if (streamEvt.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                        {
                            Console.Write(deltaTextEvent.DeltaText);
                        }
                    }
                }
            }
            //Hide debug info for cleaner chat
            //else if (evt.EventType == ChatRuntimeEventTypes.Orchestration)
            //{
            //    if (evt is ChatRuntimeOrchestrationEvent orchestrationEvt)
            //    {
            //        if (orchestrationEvt.OrchestrationEventData is OnVerboseOrchestrationEvent verbose)
            //        {
            //            Console.WriteLine(verbose.Message);
            //        }
            //    }
            //}

            await ValueTask.CompletedTask;
        };

        Console.WriteLine("[Assistant]: Hello");
        string topic = "";
        int loopCount = 0;
        while (topic != "exit")
        {
            Console.Write("[User]: ");
            topic = Console.ReadLine();
            if (topic == "exit") break;
            Console.Write("[Assistant]: ");
            ChatMessage report = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, topic));
            Console.WriteLine();
            RunnerRecordVisualizationUtility.SaveRunnerRecordDotGraphToFileAsync(config.RunSteps.ToDictionary(), $"ResearchAgentRecord{loopCount}.dot", "ResearchAgentRecord");
            loopCount++;
        }  
    }

    [TornadoTest]
    public static async Task BasicOrchestrationRuntimeStreamingDemo()
    {
        SimpleAgentConfiguration RuntimeConfiguration = new SimpleAgentConfiguration(Program.Connect(), true);

        ChatRuntime runtime = new ChatRuntime(RuntimeConfiguration);

        RuntimeConfiguration.OnRuntimeEvent = async (evt) =>
        {
            if (evt.EventType == ChatRuntimeEventTypes.AgentRunner)
            {
                if (evt is ChatRuntimeAgentRunnerEvents runnerEvt)
                {
                    if (runnerEvt.AgentRunnerEvent is AgentRunnerStreamingEvent streamEvt)
                    {
                        if (streamEvt.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                        {
                            Console.Write(deltaTextEvent.DeltaText);
                        }
                    }
                }
            }
            await ValueTask.CompletedTask;
        };
        Console.WriteLine("[Assistant]: Hello?");
        Console.Write("[User]: ");
        string topic = Console.ReadLine();
        Console.Write("[Assistant]: ");
        ChatMessage report = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, topic));

        RunnerRecordVisualizationUtility.SaveRunnerRecordDotGraphToFileAsync(RuntimeConfiguration.RunSteps.ToDictionary(), "ResearchAgentRecord.dot", "ResearchAgentRecord");
    }

    [TornadoTest]
    public static async Task BasicOrchestrationRuntimeDemo()
    {
        SimpleAgentConfiguration RuntimeConfiguration = new SimpleAgentConfiguration(Program.Connect());

        ChatRuntime runtime = new ChatRuntime(RuntimeConfiguration);

        Console.WriteLine("[Assistant]: What do you want to research?");
        Console.Write("[User]: ");
        string topic = Console.ReadLine();
        ChatMessage result = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, topic));

        RunnerRecordVisualizationUtility.SaveRunnerRecordDotGraphToFileAsync(RuntimeConfiguration.RunSteps.ToDictionary(), "ResearchAgentRecord.dot", "ResearchAgentRecord");

        Console.WriteLine(result.Content);
    }

    #endregion


    [TornadoTest]
    [Flaky("manual interaction")]
    public static async Task CodingAgentRuntimeDemo()
    {

        CodingAgentConfiguration RuntimeConfiguration = new CodingAgentConfiguration();

        ChatRuntime runtime = new ChatRuntime(RuntimeConfiguration);

        Console.WriteLine("[Assistant]: What do you want to code?");
        Console.Write("[User]: ");
        string topic = Console.ReadLine();
        ChatMessage report = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, topic ?? "Write a hello world script"));

        Console.WriteLine(report.Content);
    }

    [TornadoTest]
    public static async Task CodingAgentDotVisualizationDemo()
    {
        CodingAgentConfiguration RuntimeConfiguration = new CodingAgentConfiguration();
        Console.WriteLine(OrchestrationVisualization.ToDotGraph<ChatMessage,ChatMessage>(RuntimeConfiguration, "CodingAgentOrchestration"));
    }
}


