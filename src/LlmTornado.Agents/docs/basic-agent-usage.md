# Basic Agent Usage

Learn the fundamental patterns for creating and using agents in LlmTornado.Agents.

## Creating Agents

### Simple Agent

The most basic agent requires just a client, model, and instructions:

```csharp
using LlmTornado.Agents;
using LlmTornado.Chat.Models;

TornadoAgent agent = new TornadoAgent(
    client: client,
    model: ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a helpful assistant."
);
```

### Agent with Custom Instructions

Tailor your agent's behavior with detailed instructions:

```csharp
string instructions = """
    You are a technical writing assistant. Your role is to:
    1. Help users write clear, concise technical documentation
    2. Suggest improvements for clarity and readability
    3. Follow standard technical writing conventions
    4. Always ask clarifying questions when requirements are unclear
    """;

TornadoAgent agent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions
);
```

## Running Agents

### Basic Execution

```csharp
// Simple question-answer
Conversation result = await agent.RunAsync("What is machine learning?");
string response = result.Messages.Last().Content;
Console.WriteLine(response);
```

### Conversation Context

Agents maintain conversation history automatically:

```csharp
// First message
var result1 = await agent.RunAsync("My name is Alice");
Console.WriteLine(result1.Messages.Last().Content); // "Nice to meet you, Alice!"

// Follow-up message (agent remembers context)
var result2 = await agent.RunAsync("What's my name?");
Console.WriteLine(result2.Messages.Last().Content); // "Your name is Alice."
```

### Streaming Responses

Get real-time responses as the agent generates them:

```csharp
ValueTask HandleStreamingEvent(AgentRunnerEvents runEvent)
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
            Console.WriteLine("\\n[Tool being called...]");
            break;
        case AgentRunnerEventTypes.ToolCallResult:
            Console.WriteLine("[Tool completed]");
            break;
    }
    return ValueTask.CompletedTask;
}

// Run with streaming
Conversation result = await agent.RunAsync(
    "Write a short story about a robot",
    streaming: true,
    onAgentRunnerEvent: HandleStreamingEvent
);
```

## Agent Configuration

### Model Selection

Choose the right model for your use case:

```csharp
// Fast and cost-effective for simple tasks
var quickAgent = new TornadoAgent(client, ChatModel.OpenAi.Gpt41.V41Mini, instructions);

// More capable for complex reasoning
var smartAgent = new TornadoAgent(client, ChatModel.OpenAi.Gpt41.V41, instructions);

// Anthropic models for different use cases
var claudeAgent = new TornadoAgent(client, ChatModel.Anthropic.Claude3.Sonnet, instructions);
```

### Response Options

Configure how the agent responds:

```csharp
// Configure response behavior
agent.Options.MaxTokens = 500;
agent.Options.Temperature = 0.7;
agent.Options.TopP = 0.9;
```

## Agent Behavior Patterns

### Task-Specific Agents

Create agents specialized for specific tasks:

```csharp
// Code review agent
TornadoAgent codeReviewer = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    """
    You are a senior software engineer performing code reviews.
    Focus on:
    - Code quality and maintainability
    - Security vulnerabilities
    - Performance considerations
    - Best practice adherence
    Provide constructive feedback with specific suggestions.
    """
);

// Creative writing agent
TornadoAgent writer = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41,
    """
    You are a creative writing assistant. Help users:
    - Develop compelling characters and plots
    - Improve narrative structure
    - Enhance dialogue and descriptions
    - Overcome writer's block
    Write in an encouraging, supportive tone.
    """
);
```

### Multi-Turn Conversations

Handle complex interactions across multiple exchanges:

```csharp
TornadoAgent tutor = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    """
    You are a patient math tutor. Guide students through problems step-by-step.
    Ask questions to check understanding and provide hints rather than direct answers.
    """
);

// Interactive tutoring session
Console.WriteLine("Math Tutor: Hello! What math topic would you like help with?");

while (true)
{
    Console.Write("Student: ");
    string input = Console.ReadLine();
    
    if (input?.ToLower() == "quit") break;
    
    var response = await tutor.RunAsync(input);
    Console.WriteLine($"Math Tutor: {response.Messages.Last().Content}");
}
```

## Error Handling

### Basic Error Handling

```csharp
try
{
    var result = await agent.RunAsync("Your question here");
    Console.WriteLine(result.Messages.Last().Content);
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    // Handle specific error types as needed
}
```

### Retry Logic

Implement retry for transient failures:

```csharp
async Task<Conversation> RunWithRetry(TornadoAgent agent, string input, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            return await agent.RunAsync(input);
        }
        catch (Exception ex) when (i < maxRetries - 1)
        {
            Console.WriteLine($"Attempt {i + 1} failed: {ex.Message}");
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i))); // Exponential backoff
        }
    }
    throw new Exception($"Failed after {maxRetries} attempts");
}
```

## Agent Lifecycle

### Creating Dummy Agents

For testing or development purposes:

```csharp
// Create a dummy agent (useful for testing)
TornadoAgent dummyAgent = TornadoAgent.DummyAgent();
```

### Agent Cleanup

Agents handle resource cleanup automatically, but you can explicitly manage resources:

```csharp
// Agents implement IDisposable pattern if needed
using var agent = new TornadoAgent(client, model, instructions);
var result = await agent.RunAsync("Hello");
// Automatically disposed at end of using block
```

## Performance Tips

### Efficient Agent Usage

```csharp
// Reuse agents for multiple conversations
TornadoAgent agent = new TornadoAgent(client, model, instructions);

// Multiple conversations with the same agent
var result1 = await agent.RunAsync("First question");
var result2 = await agent.RunAsync("Follow-up question");
var result3 = await agent.RunAsync("Another question");

// Rather than creating new agents each time
```

### Memory Management

```csharp
// Clear conversation history if it gets too long
if (agent.Conversation.Messages.Count > 50)
{
    // Keep only recent messages
    var recentMessages = agent.Conversation.Messages.TakeLast(10).ToList();
    agent.Conversation = agent.Client.Chat.CreateConversation();
    foreach (var message in recentMessages)
    {
        agent.Conversation.AppendMessage(message);
    }
}
```

## Next Steps

- Learn about [Tool Integration](tool-integration.md) to add capabilities
- Explore [Structured Output](structured-output.md) for predictable responses
- Discover [Chat Runtime](chat-runtime.md) for advanced workflows
- Check out [Guardrails](guardrails.md) for input validation