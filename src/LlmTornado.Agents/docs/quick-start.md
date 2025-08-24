# Quick Start

Get up and running with LlmTornado.Agents in just a few steps.

## Installation

Add the LlmTornado.Agents package to your project:

```bash
dotnet add package LlmTornado.Agents
```

## Prerequisites

You'll need:
- .NET 8.0 or later
- An API key for your chosen LLM provider (OpenAI, Anthropic, etc.)
- Basic familiarity with C# and async programming

## Your First Agent

Here's how to create and run your first agent:

```csharp
using LlmTornado.Agents;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

// Create a client with your API key
TornadoApi client = new TornadoApi("your_api_key");

// Create a simple agent
TornadoAgent agent = new TornadoAgent(
    client,
    model: ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a helpful assistant.");

// Run the agent
Conversation result = await agent.RunAsync("What is 2+2?");

// Get the response
Console.WriteLine(result.Messages.Last().Content);
```

## Running with Streaming

For real-time responses, enable streaming:

```csharp
// Create an event handler for streaming responses
ValueTask StreamEventHandler(AgentRunnerEvents runEvent)
{
    switch (runEvent.EventType)
    {
        case AgentRunnerEventTypes.Streaming:
            if (runEvent is AgentRunnerStreamingEvent streamingEvent)
            {
                if (streamingEvent.ModelStreamingEvent is ModelStreamingOutputTextDeltaEvent deltaTextEvent)
                {
                    Console.Write(deltaTextEvent.DeltaText);
                }
            }
            break;
    }
    return ValueTask.CompletedTask;
}

// Run with streaming enabled
Conversation result = await agent.RunAsync(
    "Tell me a story about a robot.", 
    streaming: true, 
    onAgentRunnerEvent: StreamEventHandler);
```

## Adding Your First Tool

Extend your agent with custom functionality:

```csharp
using System.ComponentModel;

// Define a tool method
[Description("Get the current weather in a given location")]
public static string GetCurrentWeather(
    [Description("The city and state, e.g. Boston, MA")] string location)
{
    // Your weather API integration here
    return $"The weather in {location} is sunny, 72°F";
}

// Create an agent with tools
TornadoAgent agent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a helpful weather assistant.",
    tools: [GetCurrentWeather]);

// Ask about weather
Conversation result = await agent.RunAsync("What's the weather like in Boston?");
Console.WriteLine(result.Messages.Last().Content);
```

## Structured Output

Get consistent, structured responses:

```csharp
using System.ComponentModel;

// Define the output structure
[Description("Weather information for a location")]
public struct WeatherInfo
{
    [Description("The location name")]
    public string Location { get; set; }
    
    [Description("Temperature in Fahrenheit")]
    public int Temperature { get; set; }
    
    [Description("Weather condition")]
    public string Condition { get; set; }
}

// Create agent with structured output
TornadoAgent agent = new TornadoAgent(
    client,
    ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "Provide weather information in the specified format.",
    outputSchema: typeof(WeatherInfo));

Conversation result = await agent.RunAsync("What's the weather in Miami?");

// Parse the structured response
WeatherInfo weather = result.Messages.Last().Content.ParseJson<WeatherInfo>();
Console.WriteLine($"Location: {weather.Location}");
Console.WriteLine($"Temperature: {weather.Temperature}°F");
Console.WriteLine($"Condition: {weather.Condition}");
```

## Next Steps

Now that you have a basic agent running, explore these advanced features:

- [Basic Agent Usage](basic-agent-usage.md) - Learn more agent patterns
- [Tool Integration](tool-integration.md) - Add powerful capabilities
- [Structured Output](structured-output.md) - Master structured responses
- [Chat Runtime](chat-runtime.md) - Build complex conversational flows

## Common Issues

### API Key Setup
Make sure your API key is properly configured:

```csharp
// From environment variable
TornadoApi client = new TornadoApi(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));

// Or directly (not recommended for production)
TornadoApi client = new TornadoApi("sk-your-api-key-here");
```

### Model Selection
Different providers have different model naming:

```csharp
// OpenAI
ChatModel.OpenAi.Gpt41.V41Mini

// Anthropic
ChatModel.Anthropic.Claude3.Sonnet

// Check the ChatModel class for all available models
```