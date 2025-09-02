using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;
using LlmTornado.Agents.DataModels;
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
    public static async Task MagenticOneAgentRuntimeDemo()
    {
        MagenticOneConfiguration RuntimeConfiguration = new MagenticOneConfiguration(Program.Connect());

        ChatRuntime runtime = new ChatRuntime(RuntimeConfiguration);

        Console.WriteLine("[Assistant]: What task would you like the Magentic-One agents to accomplish?");
        Console.Write("[User]: ");
        string task = Console.ReadLine();
        ChatMessage result = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, task ?? "Research the latest trends in AI and create a summary document."));

        Console.WriteLine(result.Content);
    }

    [TornadoTest]
    public static async Task MagenticOneAgentStreamingDemo()
    {
        MagenticOneConfiguration RuntimeConfiguration = new MagenticOneConfiguration(Program.Connect());

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

        ChatMessage result = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, "Write a Python script to analyze a CSV file and save the results to a new file."));

        Console.WriteLine("\n\nFinal Result:");
        Console.WriteLine(result.Content);
    }

    [TornadoTest]
    public static async Task MagenticOneAgentVisualizationDemo()
    {
        MagenticOneConfiguration RuntimeConfiguration = new MagenticOneConfiguration(Program.Connect());
        Console.WriteLine(OrchestrationVisualization.ToDotGraph<ChatMessage, ChatMessage>(RuntimeConfiguration, "MagenticOneOrchestration"));
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
    public static async Task BasicOrchestrationRuntimeChatbotStreamingDemo()
    {
        ChatbotAgent chatbotConfig = new ChatbotAgent(Program.Connect(), true);

        ChatRuntime runtime = new ChatRuntime(chatbotConfig);

        chatbotConfig.OnRuntimeEvent = async (evt) =>
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


