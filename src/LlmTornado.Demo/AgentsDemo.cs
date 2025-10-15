using LlmTornado.A2A;
using LlmTornado.Agents;
using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.Utility;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Mcp;
using LlmTornado.Responses;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace LlmTornado.Demo;

public class AgentsDemo : DemoBase
{
    [TornadoTest]
    public static async Task BasicTornadoAgentRun()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, instructions:"You are a useful assistant.");

        Conversation result = await agent.RunAsync("What is 2+2?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    [TornadoTest]
    public static async Task BasicAgentChatBotStreaming()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, instructions: "You are a useful assistant.", streaming:true);

        Conversation conv = agent.Client.Chat.CreateConversation(agent.Options);

        Console.WriteLine("[Assistant]: Hello");
        string topic = "";
        while (topic != "exit")
        {
            Console.Write("[User]: ");
            topic = Console.ReadLine();
            if (topic == "exit") break;
            Console.Write("[Assistant]: ");
            conv = await agent.RunAsync(topic, appendMessages: conv.Messages.ToList(), streaming: true, onAgentRunnerEvent: runEventHandler);
            Console.WriteLine();
        }
    }

    [TornadoTest]
    public static async Task BasicAgentChatBot()
    {

        TornadoAgent agent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, instructions: "You are a useful assistant.");

        Conversation conv = agent.Client.Chat.CreateConversation(agent.Options);

        Console.Write("\n[Assistant]: Hello");
        string topic = "";
        while (topic != "exit")
        {
            Console.Write("\n[User]: ");
            topic = Console.ReadLine();
            if (topic == "exit") break;
            Console.Write("\n[Assistant]: ");
            conv = await agent.RunAsync(topic, appendMessages: conv.Messages.ToList());
            Console.Write(conv.Messages.Last().Content);
        }
    }

    public static ValueTask runEventHandler(AgentRunnerEvents runEvent)
    {
        switch (runEvent)
        {
            case AgentRunnerStreamingEvent streamingEvent:
                if (streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                {
                    Console.Write(deltaTextEvent.DeltaText); // Write the text delta directly
                }
                break;
            default:
                break;
        }
        return ValueTask.CompletedTask;
    }

    [TornadoTest]
    public static async Task TornadoAgentSaveConversation()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, instructions: "You are a useful assistant.", streaming:true);
        // Enhanced streaming callback to handle the new ModelStreamingEvents system
        ValueTask runEventHandler(AgentRunnerEvents runEvent)
        {
            switch (runEvent.EventType)
            {
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
        Console.WriteLine("[User]: My Name is john");
        Console.Write("[Agent]: ");
        Conversation result = await agent.RunAsync("My Name is john", onAgentRunnerEvent: runEventHandler);
        Console.Write("\n");
        Console.WriteLine("[User]: Can you help me with my homework?");
        Console.Write("[Agent]: ");
        result = await agent.RunAsync("Can you help me with my homework?", appendMessages: result.Messages.ToList(), onAgentRunnerEvent: runEventHandler);
        Console.Write("\n");
        Console.WriteLine("Saving conversation to conversation.json");
        result.Messages.ToList().SaveConversation("conversation.json");
        result.Clear();
        Console.WriteLine("Loading Saved conversation");
        List<ChatMessage> chatMessages = new List<ChatMessage>();
        await chatMessages.LoadMessagesAsync("conversation.json");
        result.LoadConversation(chatMessages);
        Console.WriteLine("Conversation loaded, resuming conversation");
        Console.WriteLine("[User]: What is my name?");
        Console.Write("[Agent]: ");
        result = await agent.RunAsync("What is my name?", appendMessages: result.Messages.ToList(), onAgentRunnerEvent: runEventHandler);
    }

    [TornadoTest]
    public static async Task TestJsonParsing()
    {
        string duplicateJson = """
        {
            "name": "John",
            "age": 30,
            "city": "New York"
        }
        {
            "name": "John",
            "age": 30,
            "city": "New York"
        }
        """;

        JsonUtility.CheckAndRepairIfAIGeneratedDuplicateJson(duplicateJson);
    }

    [TornadoTest]
    public static async Task RunHelloWorldStreaming()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, instructions:"Have fun");

        // Enhanced streaming callback to handle the new ModelStreamingEvents system
        ValueTask runEventHandler(AgentRunnerEvents runEvent)
        {
            switch (runEvent.EventType)
            {
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

        Conversation result = await agent.RunAsync("Hello Streaming World!", streaming: true, onAgentRunnerEvent: runEventHandler);
    }

    [TornadoTest]
    public static async Task BasicAgentAsToolExample()
    {
        TornadoAgent agentTranslator = new TornadoAgent(
            Program.Connect(),
            ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You only translate english input to spanish output. Do not answer or respond, only translate.");

        TornadoAgent agent = new TornadoAgent(
            Program.Connect(),
            ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant that when asked to translate you only can rely on the given tools to translate language.",
            tools: [agentTranslator.AsTool]);

        Conversation result = await agent.RunAsync("What is 2+2? and can you provide the result to me in spanish?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    public struct IsMath
    {
        public string Reasoning { get; set; }
        public bool IsMathRequest { get; set; }
    }
    
    public static async ValueTask<GuardRailFunctionOutput> MathGuardRail(string? input = "")
    {
        TornadoAgent mathGuardrail = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, instructions: "Check if the user is asking you a Math related question.", outputSchema: typeof(IsMath));

        Conversation result = await TornadoRunner.RunAsync(mathGuardrail, input);

        IsMath? isMath = result.Messages.Last().Content.JsonDecode<IsMath>();

        return new GuardRailFunctionOutput(isMath?.Reasoning ?? "", !isMath?.IsMathRequest ?? false);
    }


    [TornadoTest]
    public static async Task BasicGuardRailExample()
    {
        TornadoAgent agent= new TornadoAgent(
            Program.Connect(),
            ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful agent");

        Conversation result = await agent.RunAsync("What is the weather?", inputGuardRailFunction: MathGuardRail);

        Console.WriteLine(result.Messages.Last().Content);
    }

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

    [TornadoTest]
    public static async Task RunBasicStructuredOutputExample()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, instructions: "Have fun", outputSchema: typeof(MathReasoning));

        Conversation result = await agent.RunAsync("How can I solve 8x + 7 = -23?");

        MathReasoning mathResult = result.Messages.Last().Content.JsonDecode<MathReasoning>();

        mathResult.ConsoleWrite();
    }

    [TornadoTest]
    public static async Task RunBasicTornadoToolUse()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(),
            ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant.",
            tools: [(
                [ToolName("GetCurrentWeather")]( 
                    [Description("The city and state, e.g. Boston, MA")] string location,
                    [Description("unit of temperature measurement in C or F")] Unit unit = Unit.Celsius
                    ) => 
                    { 
                        return "31 C";
                    })
            ]);

        Conversation result = await agent.RunAsync("What is the weather in boston?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    [TornadoTest]
    public static async Task RunMCPToolExample()
    {
        string serverPath = Path.GetFullPath(Path.Join("..", "..", "..", "..", "LlmTornado.Mcp.Sample.Server"));

        var mcpServer = new MCPServer("weather-tool", command: "dotnet", arguments: new[] { "run", "--project", serverPath });

        TornadoAgent agent = new TornadoAgent(
            Program.Connect(),
            model: ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant.",
            mcpServers: [mcpServer]
                );

        Conversation result = await agent.RunAsync("What is the weather in boston?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    [TornadoTest]
    public static async Task RunMCPPuppeteerToolExample()
    {
        var mcpServer = new MCPServer("puppeteer",  command: "docker", arguments: new[] {
            "run",
            "-i",
            "--rm",
            "--init",
            "-e",
            "DOCKER_CONTAINER=true",
            "mcp/puppeteer" });

        TornadoAgent agent = new TornadoAgent(
            Program.Connect(),
            model: ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant.",
            mcpServers: [mcpServer]
                );

        Conversation result = await agent.RunAsync("What is the weather in boston?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    [TornadoTest]
    public static async Task MCPRemoteToolExample()
    {
        string serverPath = "https://api.githubcopilot.com/mcp";

        var mcpServer = new MCPServer("github", serverPath, additionalConnectionHeaders: new Dictionary<string, string>
        {
            { "Authorization", "Bearer ghp_your_key_here" }
        });

        TornadoAgent agent = new TornadoAgent(
            Program.Connect(),
            model: ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant.",
            mcpServers: [mcpServer]
                );

        Conversation result = await agent.RunAsync("What repos do i have?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    [TornadoTest]
    public static async Task A2AAgentAsTool()
    {
        A2ATornadoConnector a2ATornadoConnector = new A2ATornadoConnector(["http://localhost:5125"]);

        a2ATornadoConnector.A2ACards.ToList().ForEach(x => Console.WriteLine($"Agent Name: {x.Value.Name}, Description: {x.Value.Description}, Endpoint: {x.Value.Url}"));

        TornadoAgent agent = new TornadoAgent(
            Program.Connect(),
            model: ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant.",
            tools: [a2ATornadoConnector.GetAvailableAgentsTool,a2ATornadoConnector.SendMessageTool]
                );

        Conversation result = await agent.RunAsync("What repos do i have?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    [TornadoTest]
    public static async Task RunBasicTornadoAgentToolValueTaskUse()
    {
        TornadoAgent agent = new TornadoAgent(
            Program.Connect(),
            model:ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant.",
            tools: [GetCurrentWeatherValueTask]);

        Conversation result = await TornadoRunner.RunAsync(agent, "What is the weather in boston?");

        Console.WriteLine(result.Messages.Last().Content);
    }

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

    public static ValueTask<string> GetCurrentWeatherValueTask(
        [Description("The city and state, e.g. Boston, MA")] string location,
        [Description("unit of temperature measurement in C or F")] Unit unit = Unit.Celsius)
    {
        // Call the weather API here.
        return ValueTask.FromResult($"31 C");
    }


    [TornadoTest]
    public static async Task AgentToolApprovalDemo()
    {
        TornadoAgent agent = new TornadoAgent(
            Program.Connect(), 
            ChatModel.OpenAi.Gpt41.V41Mini, 
            instructions: "You are a useful assistant.",
            tools: [GetCurrentWeather],
            streaming: true,
            toolPermissionRequired:new Dictionary<string, bool>()
                {
                    { "GetCurrentWeather", true }
                }
            );

        ValueTask runEventHandler(AgentRunnerEvents runEvent)
        {
            switch (runEvent.EventType)
            {
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

        ValueTask<bool> toolApprovalHandler(string toolRequest)
        {
            Console.WriteLine(toolRequest);
            Console.WriteLine("Do you approve? (y/n)");
            string? input = Console.ReadLine();
            return ValueTask.FromResult(input?.ToLower().StartsWith('y') ?? false);
        }
        Console.WriteLine("[User]: What is the weather in boston?");
        Console.Write("[Agent]: ");
        Conversation result = await agent.RunAsync("What is the weather in boston?", onAgentRunnerEvent:runEventHandler,toolPermissionHandle: toolApprovalHandler);

        Console.WriteLine(result.Messages.Last().Content);
    }


    [TornadoTest]
    public static async Task TestResponseAPIToolCall()
    {
        TornadoAgent agent = new TornadoAgent(
            Program.Connect(),
            ChatModel.OpenAi.Codex.MiniLatest,
            instructions: "You are a useful assistant.");

        agent.ResponseOptions = new ResponseRequest()
        {
            Tools = [new ResponseLocalShellTool()]
        };

        var convo = await agent.RunAsync("what files are in current directory?",streaming:false, onAgentRunnerEvent: (evt) => {
            if (evt.EventType == AgentRunnerEventTypes.Streaming && evt is AgentRunnerStreamingEvent streamingEvent)
            {
                if (streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                {
                    Console.Write(deltaTextEvent.DeltaText); // Write the text delta directly
                }
            }
            else if(evt.EventType == AgentRunnerEventTypes.ResponseApiEvent)
            {
                if(evt is AgentRunnerResponseApiEvent responseApiEvent)
                {
                    Console.WriteLine($"\n[Response API Event]: {responseApiEvent.ResponseApiEvent.EventType}");
                }
            }
                return ValueTask.CompletedTask;
        });

        ChatMessage lastMsg = convo.Messages.Last();
        List<ToolCall>? calls = lastMsg.ToolCalls?.Where(x => x.BuiltInToolCall?.ResponseExpected ?? false).ToList();

        agent.ResponseOptions.PreviousResponseId = lastMsg.NativeObject is ResponseResult rr ? rr.Id : null;

        agent.ResponseOptions.InputItems = [new LocalShellCallOutput()
        {
            Id = calls?.First().BuiltInToolCall.Name ?? "",
            Output = "AgentsDemo.cs\nDemoBase.cs\nProgram.cs\nTornadoTestAttribute.cs",
            Status = ResponseMessageStatuses.Completed
        }];

        //ChatMessage response = new ChatMessage(ChatMessageRoles.Tool, "Hello.txt")
        //{
        //    FunctionCall = new FunctionCall()
        //    {
        //        Name = calls?.First().BuiltInToolCall.Name ?? "",
        //        Arguments = "{\"file_path\":\"Hello.txt\"}",
        //        ToolCall = calls?.First()
        //    }
        //};



        convo = await agent.RunAsync(streaming: false, responseId: agent.ResponseOptions.PreviousResponseId??"", onAgentRunnerEvent: (evt) => {
            if (evt.EventType == AgentRunnerEventTypes.Streaming && evt is AgentRunnerStreamingEvent streamingEvent)
            {
                if (streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                {
                    Console.Write(deltaTextEvent.DeltaText); // Write the text delta directly
                }
            }
            else if (evt.EventType == AgentRunnerEventTypes.ResponseApiEvent)
            {
                if (evt is AgentRunnerResponseApiEvent responseApiEvent)
                {
                    Console.WriteLine($"\n[Response API Event]: {responseApiEvent.ResponseApiEvent.EventType}");
                }
            }
            return ValueTask.CompletedTask;
        });

        Console.WriteLine(convo.Messages.Last().Content);
    }
}