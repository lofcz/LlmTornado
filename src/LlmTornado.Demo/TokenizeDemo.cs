using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Responses;
using LlmTornado.Tokenize;
using LlmTornado.Tokenize.Vendors;

namespace LlmTornado.Demo;

public class TokenizeDemo : DemoBase
{
    [TornadoTest]
    public static async Task TokenizeMoonshotAiText()
    {
        TokenizeRequest request = new TokenizeRequest(ChatModel.MoonshotAi.Models.MoonshotV18k, "Hello, world! This is a test message.");
        TokenizeResult? result = await Program.ConnectMulti().Tokenize.CountTokens(request);
        
        Assert.That(result, Is.NotNull);
        Assert.That(result!.TotalTokens, Is.GreaterThan(0));
        
        Console.WriteLine($"MoonshotAI text tokenization: {result.TotalTokens} tokens");
    }
    
    [TornadoTest]
    public static async Task TokenizeMoonshotAiMessages()
    {
        TokenizeRequest request = new TokenizeRequest(ChatModel.MoonshotAi.Models.MoonshotV18k, [
            new ChatMessage(ChatMessageRoles.System, "You are a helpful assistant."),
            new ChatMessage(ChatMessageRoles.User, "Hello, how are you?"),
            new ChatMessage(ChatMessageRoles.Assistant, "I'm doing well, thank you!")
        ]);
        
        TokenizeResult? result = await Program.ConnectMulti().Tokenize.CountTokens(request);
        
        Assert.That(result, Is.NotNull);
        Assert.That(result!.TotalTokens, Is.GreaterThan(0));
        
        Console.WriteLine($"MoonshotAI messages tokenization: {result.TotalTokens} tokens");
    }
    
    [TornadoTest]
    public static async Task TokenizeAnthropicMessages()
    {
        TokenizeRequest request = new TokenizeRequest(ChatModel.Anthropic.Claude45.Sonnet250929, [
            new ChatMessage(ChatMessageRoles.System, "You are a helpful assistant."),
            new ChatMessage(ChatMessageRoles.User, "Hello, how are you?"),
            new ChatMessage(ChatMessageRoles.Assistant, "I'm doing well, thank you!")
        ]);
        
        TokenizeResult? result = await Program.ConnectMulti().Tokenize.CountTokens(request);
        
        Assert.That(result, Is.NotNull);
        Assert.That(result!.TotalTokens, Is.GreaterThan(0));
        
        if (result.NativeResult is VendorAnthropicTokenizeResult anthropicResult)
        {
            Console.WriteLine($"Anthropic messages tokenization: {result.TotalTokens} tokens (input: {anthropicResult.InputTokens})");
        }
        else
        {
            Console.WriteLine($"Anthropic messages tokenization: {result.TotalTokens} tokens");
        }
    }
    
    [TornadoTest]
    public static async Task TokenizeAnthropicWithTools()
    {
        List<Tool> tools =
        [
            new Tool(new ToolFunction("get_weather", "Get the weather for a location", new
            {
                type = "object",
                properties = new
                {
                    location = new
                    {
                        type = "string",
                        description = "The city and state, e.g. San Francisco, CA"
                    }
                },
                required = new[] { "location" }
            }))
        ];
        
        TokenizeRequest request = new TokenizeRequest(
            ChatModel.Anthropic.Claude3.Haiku,
            [
                new ChatMessage(ChatMessageRoles.System, "You are a helpful assistant."),
                new ChatMessage(ChatMessageRoles.User, "What's the weather in San Francisco?")
            ],
            tools
        );
        
        TokenizeResult? result = await Program.ConnectMulti().Tokenize.CountTokens(request);
        
        Assert.That(result, Is.NotNull);
        Assert.That(result!.TotalTokens, Is.GreaterThan(0));
        
        Console.WriteLine($"Anthropic with tools tokenization: {result.TotalTokens} tokens");
    }
    
    [TornadoTest]
    public static async Task TokenizeGoogleMessages()
    {
        TokenizeRequest request = new TokenizeRequest(ChatModel.Google.Gemini.Gemini25Flash, [
            new ChatMessage(ChatMessageRoles.System, "You are a helpful assistant."),
            new ChatMessage(ChatMessageRoles.User, "Hello, how are you?"),
            new ChatMessage(ChatMessageRoles.Assistant, "I'm doing well, thank you!")
        ]);
        
        TokenizeResult? result = await Program.ConnectMulti().Tokenize.CountTokens(request);
        
        Assert.That(result, Is.NotNull);
        Assert.That(result!.TotalTokens, Is.GreaterThan(0));
        
        Console.WriteLine($"Google messages tokenization: {result.TotalTokens} tokens");
    }
    
    [TornadoTest]
    public static async Task TokenizeGoogleText()
    {
        TokenizeRequest request = new TokenizeRequest(ChatModel.Google.Gemini.Gemini25Flash, "Hello, world! This is a test message.");
        TokenizeResult? result = await Program.ConnectMulti().Tokenize.CountTokens(request);
        
        Assert.That(result, Is.NotNull);
        Assert.That(result!.TotalTokens, Is.GreaterThan(0));
        
        Console.WriteLine($"Google text tokenization: {result.TotalTokens} tokens");
    }
    
    [TornadoTest]
    public static async Task TokenizeCohereText()
    {
        TokenizeRequest request = new TokenizeRequest(ChatModel.Cohere.Command.AReasoning2508, "Hello, world! This is a test message.");
        TokenizeResult? result = await Program.ConnectMulti().Tokenize.CountTokens(request);
        
        Assert.That(result, Is.NotNull);
        Assert.That(result!.TotalTokens, Is.GreaterThan(0));
        
        Console.WriteLine($"Cohere text tokenization: {result.TotalTokens} tokens");
        if (result.NativeResult is VendorCohereTokenizeResult cohereResult)
        {
            if (cohereResult.Tokens is not null && cohereResult.TokenStrings is not null)
            {
                Console.WriteLine($"First 5 token IDs: {string.Join(", ", cohereResult.Tokens.Take(5))}");
                Console.WriteLine($"First 5 token strings: {string.Join(", ", cohereResult.TokenStrings.Take(5))}");
            }
        }
    }
    
    [TornadoTest]
    public static async Task TokenizeCohereLongText()
    {
        string longText = string.Join(" ", Enumerable.Range(1, 100).Select(i => $"Word{i}"));
        TokenizeRequest request = new TokenizeRequest(ChatModel.Cohere.Command.AReasoning2508, longText);
        TokenizeResult? result = await Program.ConnectMulti().Tokenize.CountTokens(request);
        
        Assert.That(result, Is.NotNull);
        Assert.That(result!.TotalTokens, Is.GreaterThan(0));
        
        Console.WriteLine($"Cohere long text tokenization: {result.TotalTokens} tokens");
    }
    
    [TornadoTest]
    public static async Task TokenizeOpenAiText()
    {
        TokenizeRequest request = new TokenizeRequest(ChatModel.OpenAi.Gpt5.V5Mini, "Hello, world! This is a test message.");
        TokenizeResult? result = await Program.ConnectMulti().Tokenize.CountTokens(request);
        
        Assert.That(result, Is.NotNull);
        Assert.That(result!.TotalTokens, Is.GreaterThan(0));
        
        Console.WriteLine($"OpenAI text tokenization: {result.TotalTokens} tokens");
    }
    
    [TornadoTest]
    public static async Task TokenizeOpenAiMessages()
    {
        TokenizeRequest request = new TokenizeRequest(ChatModel.OpenAi.Gpt5.V5Mini, [
            new ChatMessage(ChatMessageRoles.System, "You are a helpful assistant."),
            new ChatMessage(ChatMessageRoles.User, "Hello, how are you?"),
            new ChatMessage(ChatMessageRoles.Assistant, "I'm doing well, thank you!")
        ]);
        
        TokenizeResult? result = await Program.ConnectMulti().Tokenize.CountTokens(request);
        
        Assert.That(result, Is.NotNull);
        Assert.That(result!.TotalTokens, Is.GreaterThan(0));
        
        Console.WriteLine($"OpenAI messages tokenization: {result.TotalTokens} tokens");
    }
    
    [TornadoTest]
    public static async Task TokenizeOpenAiWithTools()
    {
        List<Tool> tools =
        [
            new Tool(new ToolFunction("get_weather", "Get the weather for a location", new
            {
                type = "object",
                properties = new
                {
                    location = new
                    {
                        type = "string",
                        description = "The city and state, e.g. San Francisco, CA"
                    }
                },
                required = new[] { "location" }
            }))
        ];
        
        TokenizeRequest request = new TokenizeRequest(
            ChatModel.OpenAi.Gpt5.V5Mini,
            [
                new ChatMessage(ChatMessageRoles.System, "You are a helpful assistant."),
                new ChatMessage(ChatMessageRoles.User, "What's the weather in San Francisco?")
            ],
            tools
        );
        
        TokenizeResult? result = await Program.ConnectMulti().Tokenize.CountTokens(request);
        
        Assert.That(result, Is.NotNull);
        Assert.That(result!.TotalTokens, Is.GreaterThan(0));
        
        Console.WriteLine($"OpenAI with tools tokenization: {result.TotalTokens} tokens");
    }
    
    [TornadoTest]
    [TornadoTestCase("gpt-5-mini")]
    [TornadoTestCase("claude-sonnet-4-20250514")]
    [TornadoTestCase("gemini-2.5-flash")]
    public static async Task TokenizeChatRequestWithTools(string model)
    {
        ChatRequest chatRequest = new ChatRequest
        {
            Model = model,
            Messages = [
                new ChatMessage(ChatMessageRoles.System, "You are a helpful assistant with access to tools."),
                new ChatMessage(ChatMessageRoles.User, "What's the weather in San Francisco and New York?")
            ],
            Tools = [
                new Tool(new ToolFunction("get_weather", "Get the weather for a location", new
                {
                    type = "object",
                    properties = new
                    {
                        location = new
                        {
                            type = "string",
                            description = "The city and state, e.g. San Francisco, CA"
                        }
                    },
                    required = new[] { "location" }
                }))
            ]
        };
        
        TokenizeRequest request = new TokenizeRequest(chatRequest);
        TokenizeResult? result = await Program.ConnectMulti().Tokenize.CountTokens(request);
        
        Assert.That(result, Is.NotNull);
        Assert.That(result!.TotalTokens, Is.GreaterThan(0));
        
        Console.WriteLine($"[{model}] ChatRequest with tools tokenization: {result.TotalTokens} tokens");
    }
    
    [TornadoTest]
    public static async Task TokenizeOpenAiFromResponseRequest()
    {
        ResponseRequest responseRequest = new ResponseRequest
        {
            Model = ChatModel.OpenAi.Gpt5.V5Mini,
            Instructions = "You are a helpful assistant.",
            InputString = "What is the meaning of life?"
        };
        
        TokenizeRequest request = new TokenizeRequest(responseRequest);
        TokenizeResult? result = await Program.ConnectMulti().Tokenize.CountTokens(request);
        
        Assert.That(result, Is.NotNull);
        Assert.That(result!.TotalTokens, Is.GreaterThan(0));
        
        Console.WriteLine($"OpenAI from ResponseRequest tokenization: {result.TotalTokens} tokens");
    }
}

