﻿using LlmTornado.A2A;
using LlmTornado.Agents;
using LlmTornado.Agents.DataModels;
using LlmTornado.Agents.Samples.claude_skills;
using LlmTornado.Agents.Utility;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Mcp;
using LlmTornado.Responses;
using LlmTornado.Skills;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace LlmTornado.Demo;

public class AgentsDemo : DemoBase
{
    [TornadoTest]
    public static async Task BasicTornadoAgentRun()
    {
        TornadoAgent agent = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt41.V41Mini, instructions:"You are a useful assistant.");

        Conversation result = await agent.Run("What is 2+2?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    [TornadoTest]
    [Flaky("manual interaction")]
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
            conv = await agent.Run(topic, appendMessages: conv.Messages.ToList(), streaming: true, onAgentRunnerEvent: runEventHandler);
            Console.WriteLine();
        }
    }

    [TornadoTest]
    [Flaky("manual interaction")]
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
            conv = await agent.Run(topic, appendMessages: conv.Messages.ToList());
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
        Conversation result = await agent.Run("My Name is john", onAgentRunnerEvent: runEventHandler);
        Console.Write("\n");
        Console.WriteLine("[User]: Can you help me with my homework?");
        Console.Write("[Agent]: ");
        result = await agent.Run("Can you help me with my homework?", appendMessages: result.Messages.ToList(), onAgentRunnerEvent: runEventHandler);
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
        result = await agent.Run("What is my name?", appendMessages: result.Messages.ToList(), onAgentRunnerEvent: runEventHandler);
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

        Conversation result = await agent.Run("Hello Streaming World!", streaming: true, onAgentRunnerEvent: runEventHandler);
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

        Conversation result = await agent.Run("What is 2+2? and can you provide the result to me in spanish?");

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

        Conversation result = await agent.Run("What is the weather?", inputGuardRailFunction: MathGuardRail);

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

        Conversation result = await agent.Run("How can I solve 8x + 7 = -23?");

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

        Conversation result = await agent.Run("What is the weather in boston?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    [TornadoTest]
    public static async Task RunMCPToolExample()
    {
        string serverPath = Path.GetFullPath(Path.Join("..", "..", "..", "..", "LlmTornado.Mcp.Sample.Server"));

        MCPServer mcpServer = new MCPServer("weather-tool", command: "dotnet", arguments: new[] { "run", "--project", serverPath });

        await mcpServer.InitializeAsync();

        TornadoAgent agent = new TornadoAgent(
            Program.Connect(),
            model: ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant."
                );

        agent.AddMcpTools(mcpServer.AllowedTornadoTools.ToArray());

        Conversation result = await agent.Run("What is the weather in boston?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    [TornadoTest]
    [Flaky]
    public static async Task RunMCPPuppeteerToolExample()
    {
        MCPServer mcpServer = new MCPServer("puppeteer",  command: "docker", arguments: new[] {
            "run",
            "-i",
            "--rm",
            "--init",
            "-e",
            "DOCKER_CONTAINER=true",
            "mcp/puppeteer" });

        await mcpServer.InitializeAsync();

        TornadoAgent agent = new TornadoAgent(
            Program.Connect(),
            model: ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant."
                );

        agent.AddMcpTools(mcpServer.AllowedTornadoTools.ToArray());

        Conversation result = await agent.Run("What is the weather in boston?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    [TornadoTest]
    [Flaky("REQUIRES GITHUB_API_KEY SETUP IN SYSTEM ENV. VAR")]
    public static async Task MCPRemoteToolExample()
    {
        string serverPath = "https://api.githubcopilot.com/mcp";

        MCPServer mcpServer = new MCPServer("github", serverPath, additionalConnectionHeaders: new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {Environment.GetEnvironmentVariable("GITHUB_API_KEY")}" }
        });

        await mcpServer.InitializeAsync();

        TornadoAgent agent = new TornadoAgent(
            Program.Connect(),
            model: ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant."
                );

        agent.AddMcpTools(mcpServer.AllowedTornadoTools.ToArray());

        Conversation result = await agent.Run("What repos do i have?");

        Console.WriteLine(result.Messages.Last().Content);
    }

    /// <summary>
    /// https://github.com/gongrzhe/gmail-mcp-server?tab=readme-ov-file  see this to setup Auth
    /// </summary>
    /// <returns></returns>
    [TornadoTest]
    [Flaky("Requires Gmail OAuth setup")]
    public static async Task MCPGmailToolkitExample()
    {
        MCPServer gmailServer = new MCPServer(serverLabel:"gmail", command: "npx", arguments: new[] {
            "@gongrzhe/server-gmail-autoauth-mcp"
        },
            allowedTools: ["read_email", "draft_email", "search_emails"]);

        await gmailServer.InitializeAsync();

        TornadoAgent agent = new TornadoAgent(
            Program.Connect(),
            model: ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant for managing Gmail."
                );


        agent.AddMcpTools(gmailServer.AllowedTornadoTools.ToArray());

        Conversation result = await agent.Run("Did mom respond?");

        Console.WriteLine(result.Messages.Last().Content);
    }
    

    [TornadoTest]
    [Flaky("Requires A2A Tornado setup")]
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

        Conversation result = await agent.Run("What repos do i have?");

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
    [Flaky("manual interaction")]
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
        Conversation result = await agent.Run("What is the weather in boston?", onAgentRunnerEvent:runEventHandler,toolPermissionHandle: toolApprovalHandler);

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

        Conversation convo = await agent.Run("what files are in current directory?",streaming:false, onAgentRunnerEvent: (evt) => {
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



        convo = await agent.Run(streaming: false, responseId: agent.ResponseOptions.PreviousResponseId??"", onAgentRunnerEvent: (evt) => {
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

    //[TornadoTest("Test Skills")]
    public static async Task RunSkillsAgent()
    {
        TornadoApi api = Program.Connect();
        ClaudeSkillAgent agent = new ClaudeSkillAgent();
        // run once
        //Skill skill = await agent.UploadSkillFolder(api, "skill-creator", "Static/Files/Skills/skill-creator");

        List<AnthropicSkill> skills = new List<AnthropicSkill>
                        {
                            //new AnthropicSkill("skill_016mAwJ3Z9CjdnNHXsftbypW", "latest"), //llmtornado-tutorial-generator
                            //new AnthropicSkill("skill_01FBEnqs5m8r4pYEugE9kaht", "latest"), //codebase-context-extractor
                            new AnthropicSkill("skill_01XRrc3ciQHW3ZbCxmMzcQPo","latest") //ability-generator
                        };

       Conversation conv = await agent.Invoke(api, 
           new ChatMessage(ChatMessageRoles.User,
           "Can you please make me an anthropic SKILL that can compile a Company Product Context based off Company PDF file extraction, web search, and related industry knowledge?"),
           skills);

       Console.WriteLine(conv.Messages.Last().Content ?? "n/a");
    }

    [Description("Roll a 20 sided dice")]
    public static string RollDice()
    {
        Random rand = new Random();
        string diceRoll = rand.Next(1, 20).ToString();
        Console.WriteLine($"[Dice Rolled]: {diceRoll}");
        return diceRoll;
    }

    [TornadoTest("DnD Roleplay")]
    [Flaky]
    public static async Task RunDnDRoleplayDemo()
    {
        Console.WriteLine("Welcome Too Dungeon Masters");
        List<ChatMessage> AllContent = new List<ChatMessage>([new ChatMessage(ChatMessageRoles.User, "Start a new Dungeons and Dragons campaign with me.")]);

        string dungeonMasterInstructions = @"
You are a Dungeons and Dragons Dungeon Master. You will guide the solo player through an epic adventure. You will role the dice for all interactions. Start by giving the player a character and explaining the scene and backstory. Task the Player with an action requirement at the end of each prompt.";

        string dungeonPlayerInstructions = @"
You will be given a character and must listen to the dungeon master to embark on a solo adventure. 
You do not dictate the Story. 
You are a player.
DO NOT GIVE NEXT STEPS TO THE DUNGEON MASTER.
IT IS ONLY YOU PLAYING.
THE USER WILL DISCRIBE THE OUTCOME OF YOUR ACTIONS.
THE USER WILL CREATE THE NEXT STEPS.
";

        TornadoAgent agentDungeonPlayer = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt5.V5Mini, instructions: dungeonPlayerInstructions, streaming: true);

        TornadoAgent agentDungeonMaster = new TornadoAgent(Program.Connect(), ChatModel.OpenAi.Gpt5.V5Nano, tools: [RollDice], instructions: dungeonMasterInstructions, streaming: true);
        
        Console.WriteLine("\n\n[DUNGEON MASTER]:");
        Conversation dungeonMasterConv = await agentDungeonMaster.Run(appendMessages: [new ChatMessage(ChatMessageRoles.User, "Start")], streaming: agentDungeonMaster.Streaming, onAgentRunnerEvent: (evt) => {
            if (evt.EventType == AgentRunnerEventTypes.Streaming && evt is AgentRunnerStreamingEvent streamingEvent)
            {
                if (streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                {
                    Console.Write(deltaTextEvent.DeltaText); // Write the text delta directly
                }
            }
            return ValueTask.CompletedTask;
        });

        AllContent.Add(new ChatMessage(ChatMessageRoles.Assistant, dungeonMasterConv.Messages.Last().GetMessageContent()));



        Console.WriteLine("\n\n[PLAYER]:");
        Conversation playerDungeonConversation = await agentDungeonPlayer.Run(dungeonMasterConv.Messages.Last().Content, streaming: agentDungeonPlayer.Streaming, onAgentRunnerEvent: (evt) => {
            if (evt.EventType == AgentRunnerEventTypes.Streaming && evt is AgentRunnerStreamingEvent streamingEvent)
            {
                if (streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                {
                    Console.Write(deltaTextEvent.DeltaText); // Write the text delta directly
                }
            }
            return ValueTask.CompletedTask;
        });

        dungeonMasterConv.AddUserMessage("[PLAYER]:" + playerDungeonConversation.Messages.Last().Content);

        AllContent.Add(new ChatMessage(ChatMessageRoles.User, playerDungeonConversation.Messages.Last().GetMessageContent()));

        ConversationCompressor compressor = new ConversationCompressor(Program.Connect(),20000, new ConversationCompressionOptions() 
        { 
            CompressToolCallMessages = true,
            SummaryModel = ChatModel.OpenAi.Gpt5.V5Nano,

        });

        while (true)
        {
            Console.WriteLine($"\n\nTokens: {AllContent.Sum(m=>m.GetMessageTokens())}\n\n");

            Console.WriteLine("\n\n[DUNGEON MASTER]:");
            dungeonMasterConv = await agentDungeonMaster.Run(appendMessages: dungeonMasterConv.Messages.ToList(), streaming: true, onAgentRunnerEvent: (evt) => {
                if (evt.EventType == AgentRunnerEventTypes.Streaming && evt is AgentRunnerStreamingEvent streamingEvent)
                {
                    if (streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                    {
                        Console.Write(deltaTextEvent.DeltaText); // Write the text delta directly
                    }
                }
                return ValueTask.CompletedTask;
            });

            AllContent.Add(new ChatMessage(ChatMessageRoles.Assistant, "[DUNGEON MASTER]: " + dungeonMasterConv.Messages.Last().GetMessageContent()));

            Console.WriteLine("\n\n[PLAYER]:");

            playerDungeonConversation = await agentDungeonPlayer.Run("[DUNGEON MASTER]: " + dungeonMasterConv.Messages.Last().Content, appendMessages: playerDungeonConversation.Messages.ToList(), streaming: true, onAgentRunnerEvent: (evt) => {
                if (evt.EventType == AgentRunnerEventTypes.Streaming && evt is AgentRunnerStreamingEvent streamingEvent)
                {
                    if (streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                    {
                        Console.Write(deltaTextEvent.DeltaText); // Write the text delta directly
                    }
                }
                return ValueTask.CompletedTask;
            });

            dungeonMasterConv.AddUserMessage("[PLAYER]:" + playerDungeonConversation.Messages.Last().Content);

            AllContent.Add(new ChatMessage(ChatMessageRoles.User, "[PLAYER]:" + playerDungeonConversation.Messages.Last().GetMessageContent()));

            if (compressor.ShouldCompress(AllContent))
            {
                Console.WriteLine("\n--- Compressing Conversation ---\n");
                AllContent = await compressor.Compress(AllContent);
                Console.WriteLine("\n--- Compressed Messages ---\n");
                Console.WriteLine(string.Join("\n\n", AllContent.Select(m => $"{m.Role}: {m.Content}")));
                break;
            }

        }
    }


    [TornadoTest("Compress Messages")]
    [Flaky]
    public static async Task RunCompressionDemo()
    {
        List<ChatMessage> AllContent = new List<ChatMessage>();
        AllContent.LoadMessages("Static/Files/DndConvo.json");

        ConversationCompressor compressor = new ConversationCompressor(Program.Connect(), 20000, new ConversationCompressionOptions()
        {
            CompressToolCallMessages = true,
            SummaryModel = ChatModel.OpenAi.Gpt5.V5Nano,

        });

        if (compressor.ShouldCompress(AllContent))
        {
            Console.WriteLine("\n--- Compressing Conversation ---\n");
            AllContent = await compressor.Compress(AllContent);
            Console.WriteLine("\n--- Compressed Messages ---\n");
            Console.WriteLine(string.Join("\n\n", AllContent.Select(m => $"{m.Role}: {m.Content}")));
        }
    }
}