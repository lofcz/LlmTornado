# Code Examples

This section contains practical examples demonstrating various features and patterns in LlmTornado.Agents.

## Basic Examples

### Simple Agent

```csharp
using LlmTornado.Agents;
using LlmTornado.Chat.Models;

// Create API client
var client = new TornadoApi("your-api-key");

// Create simple agent
var agent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    "You are a helpful assistant."
);

// Use the agent
var result = await agent.RunAsync("What is the capital of France?");
Console.WriteLine(result.Messages.Last().Content);
```

### Agent with Tools

```csharp
using System.ComponentModel;

[Description("Calculate the square of a number")]
public static double Square(
    [Description("The number to square")] double number)
{
    return number * number;
}

[Description("Get the current time")]
public static string GetCurrentTime()
{
    return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
}

var agent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    "You are a math and time assistant.",
    tools: [Square, GetCurrentTime]
);

var result = await agent.RunAsync("What is the square of 15, and what time is it?");
Console.WriteLine(result.Messages.Last().Content);
```

### Structured Output Example

```csharp
[Description("Information about a person")]
public struct PersonInfo
{
    [Description("First name")]
    public string FirstName { get; set; }
    
    [Description("Last name")]
    public string LastName { get; set; }
    
    [Description("Age in years")]
    public int Age { get; set; }
    
    [Description("Occupation")]
    public string Occupation { get; set; }
    
    [Description("City of residence")]
    public string City { get; set; }
}

var agent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    "Extract person information from text in the specified format.",
    outputSchema: typeof(PersonInfo)
);

string text = "John Smith is a 35-year-old software engineer living in Seattle.";
var result = await agent.RunAsync($"Extract person info from: {text}");

var personInfo = result.Messages.Last().Content.ParseJson<PersonInfo>();
Console.WriteLine($"Name: {personInfo.FirstName} {personInfo.LastName}");
Console.WriteLine($"Age: {personInfo.Age}");
Console.WriteLine($"Job: {personInfo.Occupation}");
Console.WriteLine($"Location: {personInfo.City}");
```

## Intermediate Examples

### Streaming Agent with Event Handling

```csharp
async ValueTask HandleStreamingEvents(AgentRunnerEvents runEvent)
{
    switch (runEvent.EventType)
    {
        case AgentRunnerEventTypes.Streaming:
            if (runEvent is AgentRunnerStreamingEvent streamingEvent)
            {
                if (streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaEvent)
                {
                    Console.Write(deltaEvent.DeltaText);
                }
            }
            break;
            
        case AgentRunnerEventTypes.ToolCall:
            if (runEvent is AgentRunnerToolCallEvent toolCallEvent)
            {
                Console.WriteLine($"\\n🔧 Calling tool: {toolCallEvent.ToolCall.Name}");
            }
            break;
            
        case AgentRunnerEventTypes.ToolCallResult:
            Console.WriteLine("✅ Tool completed");
            break;
    }
    return ValueTask.CompletedTask;
}

var agent = new TornadoAgent(client, model, "You are a storyteller");

Console.WriteLine("Agent: ");
var result = await agent.RunAsync(
    "Tell me a short story about a robot learning to paint",
    streaming: true,
    onAgentRunnerEvent: HandleStreamingEvents
);
Console.WriteLine("\\n\\nStory complete!");
```

### Agent with Guardrails

```csharp
[Description("Content analysis")]
public struct ContentAnalysis
{
    [Description("Is this content appropriate for children?")]
    public bool IsChildFriendly { get; set; }
    
    [Description("Content category")]
    public string Category { get; set; }
    
    [Description("Explanation of the analysis")]
    public string Reasoning { get; set; }
}

async ValueTask<GuardRailFunctionOutput> ChildSafetyGuardRail(string input = "")
{
    var safetyAgent = new TornadoAgent(
        client,
        ChatModel.OpenAi.Gpt41.V41Mini,
        "Analyze content for child safety and appropriateness.",
        outputSchema: typeof(ContentAnalysis)
    );
    
    var result = await safetyAgent.RunAsync($"Analyze this content: {input}");
    var analysis = result.Messages.Last().Content.ParseJson<ContentAnalysis>();
    
    return new GuardRailFunctionOutput(
        analysis.Reasoning,
        tripwireTriggered: !analysis.IsChildFriendly
    );
}

var childFriendlyAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    "You create content appropriate for children."
);

try
{
    var result = await childFriendlyAgent.RunAsync(
        "Tell me about dinosaurs",
        inputGuardRailFunction: ChildSafetyGuardRail
    );
    Console.WriteLine(result.Messages.Last().Content);
}
catch (GuardRailTriggerException ex)
{
    Console.WriteLine($"Content blocked: {ex.Message}");
}
```

### Agent as Tool Example

```csharp
// Specialized translator agent
var translatorAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    "You translate English text to Spanish. Only provide the translation, no additional commentary."
);

// Specialized math agent
var mathAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    "You solve math problems step by step. Show your work clearly."
);

// Main coordinator agent that can use other agents as tools
var coordinatorAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    """
    You are a helpful assistant that can solve math problems and translate to Spanish.
    Use the available tools when needed:
    - Use the math agent for mathematical calculations
    - Use the translator agent when asked to translate to Spanish
    """,
    tools: [mathAgent.AsTool, translatorAgent.AsTool]
);

var result = await coordinatorAgent.RunAsync(
    "What is 15 * 23? Please also provide the answer in Spanish."
);
Console.WriteLine(result.Messages.Last().Content);
```

## Advanced Examples

### Sequential Runtime Example

```csharp
using LlmTornado.Agents.ChatRuntime.RuntimeConfigurations;

// Research agent
var researchAgent = new SequentialRuntimeAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    name: "Research Agent",
    instructions: "You research topics and gather information.",
    sequentialInstructions: "Research the provided topic thoroughly."
);

// Analysis agent
var analysisAgent = new SequentialRuntimeAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    name: "Analysis Agent",
    instructions: "You analyze research data and identify key insights.",
    sequentialInstructions: "Analyze the research data and provide insights."
);

// Report agent
var reportAgent = new SequentialRuntimeAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    name: "Report Agent",
    instructions: "You create professional reports based on analysis.",
    sequentialInstructions: "Create a comprehensive report from the analysis."
);

// Create sequential configuration
var sequentialConfig = new SequentialRuntimeConfiguration([
    researchAgent,
    analysisAgent,
    reportAgent
]);

// Create and run runtime
var runtime = new ChatRuntime(sequentialConfig);
var result = await runtime.InvokeAsync(
    new ChatMessage(ChatMessageRoles.User, "Create a report on renewable energy trends")
);

Console.WriteLine(result.Content);
```

### Handoff Runtime Example

```csharp
// Technical support agent
var techSupportAgent = new HandoffAgent(
    client,
    description: "Use for technical support and troubleshooting",
    model: ChatModel.OpenAi.Gpt41.V41Mini,
    name: "TechSupport",
    instructions: "You provide technical support and solve technical problems."
);

// Sales agent
var salesAgent = new HandoffAgent(
    client,
    description: "Use for sales inquiries and product information",
    model: ChatModel.OpenAi.Gpt41.V41Mini,
    name: "Sales",
    instructions: "You help with sales inquiries and product information."
);

// Customer service coordinator
var customerServiceAgent = new HandoffAgent(
    client,
    description: "Primary customer service agent",
    model: ChatModel.OpenAi.Gpt41.V41Mini,
    name: "CustomerService",
    instructions: """
        You are the primary customer service agent. Determine what customers need
        and either help them directly or hand off to the appropriate specialist.
        """,
    handoffs: [techSupportAgent, salesAgent]
);

// Create handoff configuration
var handoffConfig = new HandoffRuntimeConfiguration(customerServiceAgent);

// Monitor handoff events
ValueTask HandleHandoffEvents(ChatRuntimeEvents runtimeEvent)
{
    switch (runtimeEvent.EventType)
    {
        case ChatRuntimeEventTypes.AgentHandoff:
            if (runtimeEvent is ChatRuntimeHandoffEvent handoffEvent)
            {
                Console.WriteLine($"🔄 Handoff: {handoffEvent.FromAgent} → {handoffEvent.ToAgent}");
            }
            break;
        case ChatRuntimeEventTypes.AgentStarted:
            Console.WriteLine($"🚀 Agent started: {runtimeEvent.AgentName}");
            break;
    }
    return ValueTask.CompletedTask;
}

var runtime = new ChatRuntime(handoffConfig);
runtime.OnRuntimeEvent = HandleHandoffEvents;

var result = await runtime.InvokeAsync(
    new ChatMessage(ChatMessageRoles.User, "I'm having trouble with my software installation")
);
```

### MCP Integration Example

```csharp
// Set up MCP server
var mcpServer = new MCPServer(
    serverLabel: "file-operations",
    serverUrl: "/path/to/mcp-file-server", // Path to your MCP server executable
    allowedTools: ["read_file", "write_file", "list_directory"]
);

// Create agent with MCP tools
var fileAgent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    """
    You are a file management assistant. You can help users:
    - Read file contents
    - Write files
    - List directory contents
    
    Always ask for confirmation before writing or modifying files.
    """,
    mcpServers: [mcpServer]
);

// Monitor MCP tool usage
ValueTask HandleMcpEvents(AgentRunnerEvents runEvent)
{
    if (runEvent.EventType == AgentRunnerEventTypes.ToolCall &&
        runEvent is AgentRunnerToolCallEvent toolEvent)
    {
        if (fileAgent.McpTools.ContainsKey(toolEvent.ToolCall.Name))
        {
            Console.WriteLine($"🔧 Using MCP tool: {toolEvent.ToolCall.Name}");
        }
    }
    return ValueTask.CompletedTask;
}

var result = await fileAgent.RunAsync(
    "Please read the config.json file and summarize its contents",
    onAgentRunnerEvent: HandleMcpEvents
);
```

### Error Handling and Retry Example

```csharp
public class ResilientAgentWrapper
{
    private readonly TornadoAgent _agent;
    private readonly int _maxRetries;
    
    public ResilientAgentWrapper(TornadoAgent agent, int maxRetries = 3)
    {
        _agent = agent;
        _maxRetries = maxRetries;
    }
    
    public async Task<Conversation> RunWithRetryAsync(string input)
    {
        Exception lastException = null;
        
        for (int attempt = 1; attempt <= _maxRetries; attempt++)
        {
            try
            {
                Console.WriteLine($"Attempt {attempt}/{_maxRetries}");
                return await _agent.RunAsync(input);
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                Console.WriteLine($"Network error on attempt {attempt}: {ex.Message}");
                
                if (attempt < _maxRetries)
                {
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt));
                    Console.WriteLine($"Waiting {delay.TotalSeconds} seconds before retry...");
                    await Task.Delay(delay);
                }
            }
            catch (GuardRailTriggerException)
            {
                // Don't retry guardrail violations
                throw;
            }
        }
        
        throw new Exception($"Failed after {_maxRetries} attempts", lastException);
    }
}

// Usage
var agent = new TornadoAgent(client, model, "You are helpful");
var resilientAgent = new ResilientAgentWrapper(agent, maxRetries: 3);

try
{
    var result = await resilientAgent.RunWithRetryAsync("Hello, how are you?");
    Console.WriteLine(result.Messages.Last().Content);
}
catch (Exception ex)
{
    Console.WriteLine($"Final failure: {ex.Message}");
}
```

## Testing Examples

### Unit Testing Agents

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class AgentTests
{
    private TornadoAgent _mathAgent;
    
    [TestInitialize]
    public void Setup()
    {
        var client = new TornadoApi("test-api-key");
        _mathAgent = new TornadoAgent(
            client,
            ChatModel.OpenAi.Gpt41.V41Mini,
            "You are a math assistant. Only solve math problems."
        );
    }
    
    [TestMethod]
    public async Task TestBasicMath()
    {
        var result = await _mathAgent.RunAsync("What is 2 + 2?");
        var response = result.Messages.Last().Content;
        
        Assert.IsTrue(response.Contains("4"));
    }
    
    [TestMethod]
    public async Task TestStructuredOutput()
    {
        var agent = new TornadoAgent(
            new TornadoApi("test-key"),
            ChatModel.OpenAi.Gpt41.V41Mini,
            "Provide math solutions",
            outputSchema: typeof(MathSolution)
        );
        
        var result = await agent.RunAsync("Solve: 5 * 6");
        var solution = result.Messages.Last().Content.ParseJson<MathSolution>();
        
        Assert.AreEqual(30, solution.Answer);
        Assert.AreEqual("multiplication", solution.Operation.ToLower());
    }
    
    [TestMethod]
    public async Task TestGuardRails()
    {
        async ValueTask<GuardRailFunctionOutput> MathOnlyGuardRail(string input = "")
        {
            bool isMath = input.ToLower().Contains("math") || 
                         input.Contains("+") || 
                         input.Contains("-") || 
                         input.Contains("*") || 
                         input.Contains("/");
            
            return new GuardRailFunctionOutput(
                isMath ? "Math question approved" : "Not a math question",
                tripwireTriggered: !isMath
            );
        }
        
        // Should work
        var result = await _mathAgent.RunAsync(
            "What is 10 + 5?",
            inputGuardRailFunction: MathOnlyGuardRail
        );
        Assert.IsNotNull(result);
        
        // Should throw
        await Assert.ThrowsExceptionAsync<GuardRailTriggerException>(async () =>
        {
            await _mathAgent.RunAsync(
                "What's the weather?",
                inputGuardRailFunction: MathOnlyGuardRail
            );
        });
    }
}

public struct MathSolution
{
    public int Answer { get; set; }
    public string Operation { get; set; }
    public string Explanation { get; set; }
}
```

## Performance Examples

### Parallel Agent Execution

```csharp
// Create multiple agents for parallel processing
var agents = Enumerable.Range(0, 5).Select(i => 
    new TornadoAgent(
        client, 
        ChatModel.OpenAi.Gpt41.V41Mini,
        $"You are assistant #{i}"
    )
).ToArray();

var inputs = new[]
{
    "What is machine learning?",
    "Explain quantum computing",
    "What is blockchain?",
    "Describe artificial intelligence",
    "What is cloud computing?"
};

// Process all inputs in parallel
var tasks = agents.Zip(inputs, async (agent, input) =>
{
    var result = await agent.RunAsync(input);
    return new { Input = input, Response = result.Messages.Last().Content };
});

var results = await Task.WhenAll(tasks);

foreach (var result in results)
{
    Console.WriteLine($"Q: {result.Input}");
    Console.WriteLine($"A: {result.Response}");
    Console.WriteLine();
}
```

### Caching Agent Responses

```csharp
public class CachedAgent
{
    private readonly TornadoAgent _agent;
    private readonly Dictionary<string, string> _cache = new();
    
    public CachedAgent(TornadoAgent agent)
    {
        _agent = agent;
    }
    
    public async Task<string> RunAsync(string input)
    {
        var key = input.GetHashCode().ToString();
        
        if (_cache.TryGetValue(key, out var cachedResponse))
        {
            Console.WriteLine("🚀 Cache hit!");
            return cachedResponse;
        }
        
        Console.WriteLine("⏳ Processing...");
        var result = await _agent.RunAsync(input);
        var response = result.Messages.Last().Content;
        
        _cache[key] = response;
        return response;
    }
}

// Usage
var agent = new TornadoAgent(client, model, "You are helpful");
var cachedAgent = new CachedAgent(agent);

// First call - will process
var response1 = await cachedAgent.RunAsync("What is the capital of France?");

// Second call - will use cache
var response2 = await cachedAgent.RunAsync("What is the capital of France?");
```

These examples demonstrate the key features and patterns available in LlmTornado.Agents. Start with the basic examples and gradually work your way up to the more advanced patterns as you become more familiar with the framework.