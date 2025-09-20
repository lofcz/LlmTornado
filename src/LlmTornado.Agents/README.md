# 🌪️ LLM Tornado Agents

**LlmTornado Agents is a framework designed to facilitate the creation and management of AI agents that can 
perform complex tasks by leveraging large language models (LLMs). The framework provides a structured approach 
to building agents that can interact with various tools, manage state, and execute tasks in a modular fashion.**
## ⭐ Key Features:
- **Tool Integration**: Easily integrate external tools and APIs to extend the capabilities of your agents.
- **Simple Structured Output**: Define structured output schemas for agents to ensure consistent and reliable responses.
- **MCP tools support**: Seamlessly integrate with tools from the MCP ecosystem.

## ⚡ Getting Started

### 🪄 Simple Agent Setup
```csharp
using LlmTornado.Agents;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Agents.DataModels;

TornadoApi client = new TornadoApi("your_api_key");

TornadoAgent agent = new TornadoAgent(
            client,
            model:ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant.");

Conversation result = await agent.RunAsync("What is 2+2?");

Console.WriteLine(result.Messages.Last().Content);
```

### Comprehensive event based runner feedback

#### Streaming with runner event handler
```csharp
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
```

### 🛠️ Tool Integration with comprehensive schema support to convert most delegate types
- **Also Support For MCP Servers Tools**
- **Agents As Tools**
- **Inline Lambda Tool Definitions**

```csharp

// Automatic Enum conversion support
[JsonConverter(typeof(StringEnumConverter))]
public enum Unit
{
    Celsius, 
    Fahrenheit
}

// Optionally Ignore parameters from schema
[Description("Get the current weather in a given location")]
public static string GetCurrentWeather(
    [Description("The city and state, e.g. Boston, MA")] string location,
    [Description("Unit of temp.")]  Unit unit = Unit.Celsius)
{
    // Call the weather API here.
    return $"31 C";
}

TornadoAgent agent = new TornadoAgent(client,
            ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant.",
            tools: [GetCurrentWeather]);

Conversation result = await agent.RunAsync("What is the weather in boston?");

Console.WriteLine(result.Messages.Last().Content);
```
:exclamation:warning: Avoid Nested Complex Types as they are not supported by OpenAI Function Calling

## Automatic Structured Output Schema Conversion from C# types
```csharp  

[Description("Check if the user is asking a math question")]
public struct IsMath
{
    [Description("explain why this is a math problem")]
    public string Reasoning { get; set; }
    [Description("Is the user asking a math question")]
    public bool IsMathRequest { get; set; }
}

TornadoAgent guardrailAgent = new TornadoAgent(client,
            ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant.",
            outputType: typeof(IsMath));

Conversation result = await guardrailAgent.RunAsync("Is 2+2 a math question?");

IsMath? isMath = result.Messages.Last().Content.JsonDecode<IsMath>();

Console.WriteLine($"Is Math: {isMath?.IsMathRequest}, Reasoning: {isMath?.Reasoning}");
```
## Use Agents As a Tool
```csharp
TornadoAgent agentTranslator = new TornadoAgent(
            client,
            ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You only translate english input to spanish output. Do not answer or respond, only translate.");

TornadoAgent agent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a useful assistant that when asked to translate you only can rely on the given tools to translate language.",
    tools: [agentTranslator.AsTool]);

Conversation result = await agent.RunAsync("What is 2+2? and can you provide the result to me in spanish?");

Console.WriteLine(result.Messages.Last().Content);
```
## Create MCP Tool
```csharp
string serverPath = Path.GetFullPath(Path.Join("..", "..", "..", "..", "LlmTornado.Mcp.Sample.Server"));

var mcpServer = new MCPServer("weather-tool", serverPath);

TornadoAgent agent = new TornadoAgent(
    Program.Connect(),
    model: ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a useful assistant.",
    mcpServers: [mcpServer]
        );

Conversation result = await agent.RunAsync("What is the weather in boston?");

Console.WriteLine(result.Messages.Last().Content);
```
## Create input guardrails to stop runner
```csharp
// Guardrail function to check if input is a math question
async ValueTask<GuardRailFunctionOutput> MathGuardRail(string? input = "")
{
    TornadoAgent mathGuardrail = new TornadoAgent(client, ChatModel.OpenAi.Gpt41.V41Mini, instructions: "Check if the user is asking you a Math related question.", outputSchema: typeof(IsMath));

    Conversation result = await mathGuardrail.RunAsync(input);

    IsMath? isMath = result.Messages.Last().Content.JsonDecode<IsMath>();

    // Trigger guardrail if not a math question
    return new GuardRailFunctionOutput(isMath?.Reasoning ?? "", !isMath?.IsMathRequest ?? false);
}

TornadoAgent agent= new TornadoAgent(
            client,
            ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful agent");

// This will throw an exception if the input is not a math question
Conversation result = await agent.RunAsync("What is the weather?", inputGuardRailFunction: MathGuardRail);
```

# Chat Runtime
**The Chat Runtime is a powerful framework for building conversational AI applications. 
It provides a structured way to manage the flow of conversation, handle user input, and integrate with various AI models and tools.**

# The Standard Process for talking with an agent goes like this:

![standard process](/assets/ChatCompletionFlow.jpg)

# Using the Agent loop we can handle the tool invoking automatically

![AgentFlow](/assets/AgentRunnerFlow.jpg)

Custom Runtime Configurations can be created as long as the Class inherits the interface `IRuntimeConfiguration`

# Using ChatRuntime To add more complex agentic behavior  
## Basic Concept
![RuntimeFlow](/assets/RuntimeFlow.jpg)

# Prebuilt Runtime Configurations
## Sequential ChatRuntime Configuration
![SequentialFlow](/assets/SequentialFlow.jpg)

Sequential Instructions allows additional controls over the basic Instructions to allow the next agent in line not to repond to itself (Role.Assistant message)

```csharp
    string researchInstructions = """
                    You are a research assistant. Given a search term, you search the web for that term and
                    produce a concise summary of the results. The summary must be 2-3 paragraphs and less than 300 
                    words. Capture the main points. Write succinctly, no need to have complete sentences or good
                    grammar. This will be consumed by someone synthesizing a report, so its vital you capture the 
                    essence and ignore any fluff. Do not include any additional commentary other than the summary itself.
                    """;

    SequentialRuntimeAgent ResearchAgent = new SequentialRuntimeAgent(
        client:client,
        name: "Research Agent",
        model: ChatModel.OpenAi.Gpt41.V41Mini,
        instructions: researchInstructions,
        sequentialInstructions:"Research the provided topic in my next message thoroughly and provide a summary.");

    ResearchAgent.ResponseOptions = new ResponseRequest() { Tools = [new ResponseWebSearchTool()] };

    string reportInstructions = """
                You are a senior researcher tasked with writing a cohesive report for a research query.
                you will be provided with the original query, and some initial research done by a research assistant.

                you should first come up with an outline for the report that describes the structure and flow of the report. 
                Then, generate the report and return that as your final output.

                The final output should be in markdown format, and it should be lengthy and detailed. Aim for 2-3 pages of content, at least 250 words.
                """;

    SequentialRuntimeAgent ReportAgent = new SequentialRuntimeAgent(
        client: client,
        name: "Report Agent",
        model: ChatModel.OpenAi.Gpt41.V41Mini,
        instructions: reportInstructions,
        sequentialInstructions: "With the provided research summarize the findings in this thread.");

    SequentialRuntimeConfiguration sequentialRuntimeConfiguration = new SequentialRuntimeConfiguration([ResearchAgent, ReportAgent]);

    ChatRuntime runtime = new ChatRuntime(sequentialRuntimeConfiguration);

    ChatMessage report = await runtime.InvokeAsync(new ChatMessage(Code.ChatMessageRoles.User, "Write a report about the benefits of using AI agents."));

    Console.WriteLine(report.Content);
```

## Handoff ChatRuntime Configuration
![HandoffFlow](/assets/handoffOrchestrationflow.jpg)
```csharp
HandoffAgent translatorAgent = new HandoffAgent(
            client: client,
            name: "SpanishAgent",
            model: ChatModel.OpenAi.Gpt41.V41Mini,
            instructions: "You are a useful assistant. Please only respond in spanish",
            description: "Use this Agent for spanish speaking response");

HandoffAgent usefulAgent = new HandoffAgent(
    client: client,
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
```
# Power of the Orchestration Runtime Configuration
## Orchestration powered by State Machine architecture
![OrchestrationFlow](/assets/OrchestrationFlow.jpg)

## Inside the Orchestration Invoke
![OrchestrationInvoke](/assets/OrchestrationInvokeFlow.jpg)

* Think of a `Orchestration` as a State Machine with strongly typed `TInput` and `TOutput` values for input validation
* Think of a `Runnable` as a state within a State Machine with strongly typed `TInput` and `TOutput` values for input validation
* Think of a `Advancer` as a state transition that has access to `TOutput` of the state to for conditional checks for Advancing to next 
`Runnable`
* Using the `Advancer` you can even add a Conversion Method as an input parameter to the transition to facilitate advancing to a Runnable with a invalid `TInput` (`TInput` != `TOutput`)

## Creating Custom Orchestration Workflows
![CodingOrchestration](/assets/CodingOrchestrationflow.jpg)

## Create Complex Orchestration Workflows
![ComplexOrchestration](/assets/PlannerOrchestrationFlow.jpg)

## TODO
* [ ]  Make todo list