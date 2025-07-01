[![LlmTornado](https://badgen.net/nuget/v/LlmTornado?v=302&icon=nuget&label=LlmTornado)](https://www.nuget.org/packages/LlmTornado)
[![LlmTornado.Toolkit](https://badgen.net/nuget/v/LlmTornado.Toolkit?v=302&icon=nuget&label=LlmTornado.Toolkit)](https://www.nuget.org/packages/LlmTornado.Toolkit)
[![LlmTornado.Mcp](https://badgen.net/nuget/v/LlmTornado.Mcp?v=302&icon=nuget&label=LlmTornado.Mcp)](https://www.nuget.org/packages/LlmTornado.Mcp)
[![LlmTornado.Contrib](https://badgen.net/nuget/v/LlmTornado.Contrib?v=302&icon=nuget&label=LlmTornado.Contrib)](https://www.nuget.org/packages/LlmTornado.Contrib)

# 🌪️ LLM Tornado

**Build AI agents and multi-agent systems in minutes with one toolkit. Use any Provider, switch without refactoring anything. Deploy today.**

## Key Features:
-  **Use any model from any Provider**: All you need to know is the model's name; we handle the rest. Built-in: [Anthropic](https://docs.anthropic.com/en/docs/intro), [Azure](https://azure.microsoft.com/en-us/products/ai-services/openai-service), [Cohere](https://docs.cohere.com/changelog), [DeepInfra](https://deepinfra.com/docs/), [DeepSeek](https://api-docs.deepseek.com/), [Google](https://ai.google.dev/gemini-api/docs), [Groq](https://console.groq.com/docs/overview), [Mistral](https://docs.mistral.ai/getting-started), [Ollama](https://ollama.com/search), [OpenAI](https://platform.openai.com/docs), [OpenRouter](https://openrouter.ai/docs/quickstart), [Perplexity](https://docs.perplexity.ai/home), [Voyage](https://www.voyageai.com/), [vLLM](https://docs.vllm.ai/en/latest/), [xAI](https://docs.x.ai/docs). Check the full Feature Matrix [here](https://github.com/lofcz/LlmTornado/blob/master/FeatureMatrix.md).
- **Multi-Agent Systems**: Toolkit for the orchestration of multiple collaborating specialist agents.
- **Maximize request success rate**: We keep track of which parameters are supported by which models, how long the reasoning context can be, etc., and silently modify your requests to comply with rules enforced by a diverse set of Providers.
- **Leverage unique capabilities**: Non-standard features from all major Providers are carefully mapped, documented, and ready to use via strongly-typed code.
- **Fully multimodal**: Text, image, video, document, URL, and audio inputs are supported.
- **MCP compatible**: Seamlessly integrate Model Context Protocol using the official .NET SDK and `LlmTornado.Mcp` adapter.
- **Enterprise ready**: Observability as a first-class citizen. Preview any request before committing to it. Streamlined usage information as a monitoring backbone. Flexibility with multilingual apps in mind.

## ⭐ With a few lines of code you can:
- [Chat with your documents](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/ChatDemo.cs#L722-L757)
- [Make multiple-speaker podcasts](https://github.com/lofcz/LlmTornado/blob/d1042281082ea5ff1de9dcb438a847d4cd9c416b/LlmTornado.Demo/ChatDemo2.cs#L332-L374)
- [Voice call with AI using your microphone](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/ChatDemo.cs#L905-L968)
- [Orchestrate Assistants](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/ThreadsDemo.cs#L331-L429)
- [Generate images](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/ImagesDemo.cs#L10-L13)
- [Summarize a video (local file / YouTube)](https://github.com/lofcz/LlmTornado/blob/cfd47f915584728d9a2365fc9d38d158673da68a/LlmTornado.Demo/ChatDemo2.cs#L119)
- [Turn text & images into high quality embeddings](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/EmbeddingDemo.cs#L50-L75)
- [Transcribe audio in real time](https://github.com/lofcz/LlmTornado/blob/e592a2fc0a37dbd0e754dac7b1655703367369df/LlmTornado.Demo/AudioDemo.cs#L29)

... and a lot more! Now, instead of relying on one LLM provider, you can combine the unique strengths of many.

## ⚡Getting Started

Install LLM Tornado via NuGet:

```bash
dotnet add package LlmTornado.Toolkit
```

Optional addons:

```bash
dotnet add package LlmTornado.Mcp # Model Context Protocol (MCP) integration
dotnet add package LlmTornado.Contrib # productivity, quality of life enhancements
```

## 🪄 Quick Inference

Inferencing across multiple providers is as easy as changing the `ChatModel` argument. Tornado instance can be constructed with multiple API keys, the correct key is then used based on the model automatically:

```csharp
TornadoApi api = new TornadoApi([
    // note: delete lines with providers you won't be using
    new (LLmProviders.OpenAi, "OPEN_AI_KEY"),
    new (LLmProviders.Anthropic, "ANTHROPIC_KEY"),
    new (LLmProviders.Cohere, "COHERE_KEY"),
    new (LLmProviders.Google, "GOOGLE_KEY"),
    new (LLmProviders.Groq, "GROQ_KEY"),
    new (LLmProviders.DeepSeek, "DEEP_SEEK_KEY"),
    new (LLmProviders.Mistral, "MISTRAL_KEY"),
    new (LLmProviders.XAi, "XAI_KEY"),
    new (LLmProviders.Perplexity, "PERPLEXITY_KEY"),
    new (LLmProviders.Voyage, "VOYAGE_KEY"),
    new (LLmProviders.DeepInfra, "DEEP_INFRA_KEY"),
    new (LLmProviders.OpenRouter, "OPEN_ROUTER_KEY")
]);

// this sample iterates a bunch of models, gives each the same task, and prints results.
List<ChatModel> models = [
    ChatModel.OpenAi.O3.Mini, ChatModel.Anthropic.Claude37.Sonnet,
    ChatModel.Cohere.Command.RPlus, ChatModel.Google.Gemini.Gemini2Flash001,
    ChatModel.Groq.Meta.Llama370B, ChatModel.DeepSeek.Models.Chat,
    ChatModel.Mistral.Premier.MistralLarge, ChatModel.XAi.Grok.Grok2241212,
    ChatModel.Perplexity.Sonar.Default
];

foreach (ChatModel model in models)
{
    string? response = await api.Chat.CreateConversation(model)
        .AppendSystemMessage("You are a fortune teller.")
        .AppendUserInput("What will my future bring?")
        .GetResponse();

    Console.WriteLine(response);
}
```

💡 Instead of passing in a strongly typed model, you can pass a string instead: `await api.Chat.CreateConversation("gpt-4o")`, Tornado will automatically resolve the provider.

## ❄️ Vendor Extensions

Tornado has a powerful concept of `VendorExtensions` which can be applied to various endpoints and are strongly typed. Many Providers offer unique/niche APIs, often enabling use cases otherwise unavailable. For example, let's set a reasoning budget for Anthropic's Claude 3.7:

```cs
public static async Task AnthropicSonnet37Thinking()
{
    Conversation chat = Program.Connect(LLmProviders.Anthropic).Chat.CreateConversation(new ChatRequest
    {
        Model = ChatModel.Anthropic.Claude37.Sonnet,
        VendorExtensions = new ChatRequestVendorExtensions(new ChatRequestVendorAnthropicExtensions
        {
            Thinking = new AnthropicThinkingSettings
            {
                BudgetTokens = 2_000,
                Enabled = true
            }
        })
    });
    
    chat.AppendUserInput("Explain how to solve differential equations.");

    ChatRichResponse blocks = await chat.GetResponseRich();

    if (blocks.Blocks is not null)
    {
        foreach (ChatRichResponseBlock reasoning in blocks.Blocks.Where(x => x.Type is ChatRichResponseBlockTypes.Reasoning))
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(reasoning.Reasoning?.Content);
            Console.ResetColor();
        }

        foreach (ChatRichResponseBlock reasoning in blocks.Blocks.Where(x => x.Type is ChatRichResponseBlockTypes.Message))
        {
            Console.WriteLine(reasoning.Message);
        }
    }
}
```

## 🔮 Self-Hosted/Custom Providers

Instead of consuming commercial APIs, one can easily roll their inference servers with [a plethora](https://github.com/janhq/awesome-local-ai) of available tools. Here is a simple demo for streaming response with Ollama, but the same approach can be used for any custom provider:

```cs
public static async Task OllamaStreaming()
{
    TornadoApi api = new TornadoApi(new Uri("http://localhost:11434")); // default Ollama port, API key can be passed in the second argument if needed
    
    await api.Chat.CreateConversation(new ChatModel("falcon3:1b")) // <-- replace with your model
        .AppendUserInput("Why is the sky blue?")
        .StreamResponse(Console.Write);
}
```

If you need more control over requests, for example, custom headers, you can create an instance of a built-in Provider. This is useful for custom deployments like Amazon Bedrock, Vertex AI, etc.

```cs
TornadoApi tornadoApi = new TornadoApi(new AnthropicEndpointProvider
{
    Auth = new ProviderAuthentication("ANTHROPIC_API_KEY"),
    // {0} = endpoint, {1} = action, {2} = model's name
    UrlResolver = (endpoint, url, ctx) => "https://api.anthropic.com/v1/{0}{1}",
    RequestResolver = (request, data, streaming) =>
    {
        // by default, providing a custom request resolver omits beta headers
        // request is HttpRequestMessage, data contains the payload
    },
    RequestSerializer = (data, ctx) =>
    {
       // data is JObject, which can be modified before
       // being serialized into a string.
    }
});
```

https://github.com/user-attachments/assets/de62f0fe-93e0-448c-81d0-8ab7447ad780

## 🔎 Advanced Inference

### Streaming

Tornado offers three levels of abstraction, trading more details for more complexity. The simple use cases where only plaintext is needed can be represented in a terse format:

```cs
await api.Chat.CreateConversation(ChatModel.Anthropic.Claude3.Sonnet)
    .AppendSystemMessage("You are a fortune teller.")
    .AppendUserInput("What will my future bring?")
    .StreamResponse(Console.Write);
```  
  
The levels of abstraction are:
- `Response` (`string` for chat, `float[]` for embeddings, etc.)
- `ResponseRich` (tools, modalities, metadata such as usage)
- `ResponseRichSafe` (same as level 2, guaranteed not to throw on network level, for example, if the provider returns an internal error or doesn't respond at all)

### Streaming with Rich content (tools, images, audio..)

When plaintext is insufficient, switch to `StreamResponseRich` or `GetResponseRich()` APIs. Tools requested by the model can be resolved later and never returned to the model. This is useful in scenarios where we use the tools without intending to continue the conversation:

```cs
//Ask the model to generate two images, and stream the result:
public static async Task GoogleStreamImages()
{
    Conversation chat = api.Chat.CreateConversation(new ChatRequest
    {
        Model = ChatModel.Google.GeminiExperimental.Gemini2FlashImageGeneration,
        Modalities = [ ChatModelModalities.Text, ChatModelModalities.Image ]
    });
    
    chat.AppendUserInput([
        new ChatMessagePart("Generate two images: a lion and a squirrel")
    ]);
    
    await chat.StreamResponseRich(new ChatStreamEventHandler
    {
        MessagePartHandler = async (part) =>
        {
            if (part.Text is not null)
            {
                Console.Write(part.Text);
                return;
            }

            if (part.Image is not null)
            {
                // In our tests this executes Chafa to turn the raw base64 data into Sixels
                await DisplayImage(part.Image.Url);
            }
        },
        BlockFinishedHandler = (block) =>
        {
            Console.WriteLine();
            return ValueTask.CompletedTask;
        },
        OnUsageReceived = (usage) =>
        {
            Console.WriteLine();
            Console.WriteLine(usage);
            return ValueTask.CompletedTask;
        }
    });
}
```

### Tools with immediate resolve

Tools requested by the model can be resolved and the results returned immediately. This has the benefit of automatically continuing the conversation:

```cs
Conversation chat = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt4.O,
    Tools =
    [
        new Tool(new ToolFunction("get_weather", "gets the current weather", new
        {
            type = "object",
            properties = new
            {
                location = new
                {
                    type = "string",
                    description = "The location for which the weather information is required."
                }
            },
            required = new List<string> { "location" }
        }))
    ]
})
.AppendSystemMessage("You are a helpful assistant")
.AppendUserInput("What is the weather like today in Prague?");

ChatStreamEventHandler handler = new ChatStreamEventHandler
{
  MessageTokenHandler = (x) =>
  {
      Console.Write(x);
      return Task.CompletedTask;
  },
  FunctionCallHandler = (calls) =>
  {
      calls.ForEach(x => x.Result = new FunctionResult(x, "A mild rain is expected around noon.", null));
      return Task.CompletedTask;
  },
  AfterFunctionCallsResolvedHandler = async (results, handler) => { await chat.StreamResponseRich(handler); }
};

await chat.StreamResponseRich(handler);
```

### Tools with deferred resolve

Instead of resolving the tool call, we can postpone/quit the conversation. This is useful for extractive tasks, where we care only for the tool call:

```cs
Conversation chat = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt4.Turbo,
    Tools = new List<Tool>
    {
        new Tool
        {
            Function = new ToolFunction("get_weather", "gets the current weather")
        }
    },
    ToolChoice = new OutboundToolChoice(OutboundToolChoiceModes.Required)
});

chat.AppendUserInput("Who are you?"); // user asks something unrelated, but we force the model to use the tool
ChatRichResponse response = await chat.GetResponseRich(); // the response contains one block of type Function
```

_`GetResponseRichSafe()` API is also available, which is guaranteed not to throw on the network level. The response is wrapped in a network-level wrapper, containing additional information. For production use cases, either use `try {} catch {}` on all the HTTP request-producing Tornado APIs, or use the safe APIs._

## 🌐 MCP
To use Model Context Protocol, install the `LlmTornado.Mcp` adapter. After that, new interop methods will become available on the `ModelContextProtocol` types. The following example uses the `GetForecast` tool defined on an [example MCP server](https://modelcontextprotocol.io/quickstart/server#c%23):
```cs
[McpServerToolType]
public sealed class WeatherTools
{
    [McpServerTool, Description("Get weather forecast for a location.")]
    public static async Task<string> GetForecast(
        HttpClient client,
        [Description("Latitude of the location.")] double latitude,
        [Description("Longitude of the location.")] double longitude)
    {
        var pointUrl = string.Create(CultureInfo.InvariantCulture, $"/points/{latitude},{longitude}");
        using var jsonDocument = await client.ReadJsonDocumentAsync(pointUrl);
        var forecastUrl = jsonDocument.RootElement.GetProperty("properties").GetProperty("forecast").GetString()
            ?? throw new Exception($"No forecast URL provided by {client.BaseAddress}points/{latitude},{longitude}");

        using var forecastDocument = await client.ReadJsonDocumentAsync(forecastUrl);
        var periods = forecastDocument.RootElement.GetProperty("properties").GetProperty("periods").EnumerateArray();

        return string.Join("\n---\n", periods.Select(period => $"""
                {period.GetProperty("name").GetString()}
                Temperature: {period.GetProperty("temperature").GetInt32()}°F
                Wind: {period.GetProperty("windSpeed").GetString()} {period.GetProperty("windDirection").GetString()}
                Forecast: {period.GetProperty("detailedForecast").GetString()}
                """));
    }
}
```

The following is done by a client:
```cs
// your clientTransport, for example StdioClientTransport
await using IMcpClient mcpClient = await McpClientFactory.CreateAsync(clientTransport);

// 1. fetch tools
List<Tool> tools = await mcpClient.ListTornadoToolsAsync();

// 2. create a conversation, pass available tools
TornadoApi api = new TornadoApi(LLmProviders.OpenAi, apiKeys.OpenAi);
Conversation conversation = api.Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt41.V41,
    Tools = tools,
    // force any of the available tools to be used (use new OutboundToolChoice("toolName") to specify which if needed)
    ToolChoice = OutboundToolChoice.Required
});

// 3. let the model call the tool and infer arguments
await conversation
    .AddSystemMessage("You are a helpful assistant")
    .AddUserMessage("What is the weather like in Dallas?")
    .GetResponseRich(async calls =>
    {
        foreach (FunctionCall call in calls)
        {
            // retrieve arguments inferred by the model
            double latitude = call.GetOrDefault<double>("latitude");
            double longitude = call.GetOrDefault<double>("longitude");
            
            // call the tool on the MCP server, pass args
            await call.ResolveRemote(new
            {
                latitude = latitude,
                longitude = longitude
            });

            // extract the tool result and pass it back to the model
            if (call.Result?.RemoteContent is McpContent mcpContent)
            {
                foreach (IMcpContentBlock block in mcpContent.McpContentBlocks)
                {
                    if (block is McpContentBlockText textBlock)
                    {
                        call.Result.Content = textBlock.Text;
                    }
                }
            }
        }
    });

// stop forcing the client to call the tool
conversation.RequestParameters.ToolChoice = null;

// 4. stream final response
await conversation.StreamResponse(Console.Write);
```

A complete example is available here: [client](https://github.com/lofcz/LlmTornado/blob/master/src/LlmTornado.Mcp.Sample.Server/WeatherTools.cs), [server](https://github.com/lofcz/LlmTornado/blob/master/src/LlmTornado.Mcp.Sample/Program.cs).

## 🧰 Toolkit

Tornado includes powerful abstractions in the `LlmTornado.Toolkit` package, allowing rapid development of applications, while avoiding many design pitfalls. Scalability and tuning-friendly code design are at the core of these abstractions.

### ToolkitChat

`ToolkitChat` is a primitive for graph-based workflows, where edges move data and nodes execute functions. ToolkitChat supports streaming, rich responses, and chaining tool calls. Tool calls are provided via `ChatFunction` or `ChatPlugin` (an envelope with multiple tools). Many overloads accept a primary and a secondary model acting as a backup, this zig-zag strategy overcomes temporary downtime in APIs better than simple retrying of the same model. All tool calls are strongly typed and `strict` by default. For providers, where a strict JSON schema is not supported (Anthropic, for example), prefill with `{` is used as a fallback. Call can be marked as non-strict by simply changing a parameter.

```cs
class DemoAggregatedItem
{
    public string Name { get; set; }
    public string KnownName { get; set; }
    public int Quantity { get; set; }
}

string sysPrompt = "aggregate items by type";
string userPrompt = "three apples, one cherry, two apples, one orange, one orange";

await ToolkitChat.GetSingleResponse(api, ChatModel.Google.Gemini.Gemini2Flash001, ChatModel.OpenAi.Gpt41.V41Mini, sysPrompt, new ChatFunction([
    new ChatFunctionParam("items", new ChatFunctionTypeListTypedObject("aggregated items", true, [
        new ChatFunctionParam("name", "name of the item", true, ChatFunctionAtomicParamTypes.String),
        new ChatFunctionParam("quantity", "aggregated quantity", true, ChatFunctionAtomicParamTypes.Int),
        new ChatFunctionParam("known_name", new ChatFunctionTypeEnum("known name of the item", true, [ "apple", "cherry", "orange", "other" ]))
    ]))
], async (args, ctx) =>
{
    if (!args.ParamTryGet("items", out List<DemoAggregatedItem>? items) || items is null)
    {
        return new ChatFunctionCallResult(ChatFunctionCallResultParameterErrors.MissingRequiredParameter, "items");
    }
    
    Console.WriteLine("Aggregated items:");

    foreach (DemoAggregatedItem item in items)
    {
        Console.WriteLine($"{item.Name}: {item.Quantity}");
    }
    
    return new ChatFunctionCallResult();
}), userPrompt); // temp defaults to 0, output length to 8k

/*
Aggregated items:
apple: 5
cherry: 1
orange: 2
*/
```

## 👉 Why Tornado?

- 50,000+ installs on NuGet (previous names [Lofcz.Forks.OpenAI](https://www.nuget.org/packages/Lofcz.Forks.OpenAI), [OpenAiNg](https://www.nuget.org/packages/OpenAiNg)).
- Used in [award-winning](https://www-aiawards-cz.translate.goog/?_x_tr_sl=cs&_x_tr_tl=en&_x_tr_hl=cs) commercial projects, processing > 100B tokens monthly.
- Covered by 250+ tests.
- Great performance.
- The license will never change.

[![Star History Chart](https://api.star-history.com/svg?repos=lofcz/llmtornado&type=Date)](https://www.star-history.com/#lofcz/llmtornado&Date)

## 📚 Contributing

PRs are welcome! We are accepting new Provider implementations, contributions towards a 100 % green [Feature Matrix](https://github.com/lofcz/LlmTornado/blob/master/FeatureMatrix.md), and, after public discussion, new abstractions.

## License

This library is licensed under the [MIT](https://github.com/lofcz/LlmTornado/blob/master/LICENSE) license. 💜
