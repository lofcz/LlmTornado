[![LlmTornado](https://badgen.net/nuget/v/LlmTornado?v=302&icon=nuget&label=LlmTornado)](https://www.nuget.org/packages/LlmTornado)
[![LlmTornado.Toolkit](https://badgen.net/nuget/v/LlmTornado.Toolkit?v=302&icon=nuget&label=LlmTornado.Toolkit)](https://www.nuget.org/packages/LlmTornado.Toolkit)
[![LlmTornado.Contrib](https://badgen.net/nuget/v/LlmTornado.Contrib?v=302&icon=nuget&label=LlmTornado.Contrib)](https://www.nuget.org/packages/LlmTornado.Contrib)


# 🌪️ LLM Tornado - The .NET library to consume 100+ APIs.

At least one new large language model is released each month. Wouldn't it be awesome if using the latest, shiniest model was as easy as switching one argument?
LLM Tornado is a framework for building AI, RAG/Agentic-enabled applications, allowing you to do just that.

Features:
-  100+ supported providers: **OpenAI, Anthropic, Google, DeepSeek, Cohere, Mistral, Azure, xAI, Perplexity, Groq, Voyage**, and any (self-hosted) OpenAI-compatible inference servers, such as [Ollama](https://github.com/lofcz/LlmTornado/blob/4c70e7d8586cb79fd9d9fe9614c85c5dda654deb/LlmTornado.Demo/CustomProviderDemo.cs#L11). Check the full Feature Matrix [here](https://github.com/lofcz/LlmTornado/blob/master/FeatureMatrix.md).
- _API harmonization_. The shape of APIs changes often. Certain parameters can't be used for reasoning models, certain parameters have different names based on the model (for example, `developer_message` vs `system_prompt`), certain providers implement standard endpoints in a non-standard way (for example, Google has two endpoints for embeddings). We take care of these annoyances as much as possible, reducing maintenance on your side.
- Powerful, strongly-typed `Vendor Extensions` for each provider offering something unique. Minimize vendor lock-in, maximize the benefits.
- Easy-to-grasp primitives for building Agentic systems, Chatbots, and RAG-based applications (`Memory/Conversation`, etc.). Less complex than Semantic Kernel, and more powerful than the raw APIs.
- Observability as a first-class citizen. Observe requests before/after firing them, with automatic secrets anonymization. Unified `usage` information with optional, vendor-specific details.
- As few breaking changes as possible. We take these _seriously_ and think ahead. Updating Tornado typically requires no action on your side, even when a new major version is released.
- Actively maintained for over two years, often with day 1 support for new features. 50+ releases.

⭐ Awesome things you can do with Tornado:
- [Chat with your documents](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/ChatDemo.cs#L722-L757)
- [Voice call with AI using your microphone](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/ChatDemo.cs#L905-L968)
- [Orchestrate Assistants](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/ThreadsDemo.cs#L331-L429)
- [Generate images](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/ImagesDemo.cs#L10-L13)
- [Summarize a video (local file / YouTube)](https://github.com/lofcz/LlmTornado/blob/cfd47f915584728d9a2365fc9d38d158673da68a/LlmTornado.Demo/ChatDemo2.cs#L119)
- [Turn text & images into high quality embeddings](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/EmbeddingDemo.cs#L50-L75)
- [Transcribe audio in real time](https://github.com/lofcz/LlmTornado/blob/e592a2fc0a37dbd0e754dac7b1655703367369df/LlmTornado.Demo/AudioDemo.cs#L29)
- Create Chatbots utilizing multiple Agents: 

https://github.com/lofcz/LlmTornado/assets/10260230/05c27b37-397d-4b4c-96a4-4138ade48dbe

... and a lot more! Now, instead of relying on one LLM provider, you can combine the unique strengths of many.

## ⚡Getting Started

Install LLM Tornado via NuGet:

```bash
dotnet add package LlmTornado LlmTornado.Toolkit # core + toolkit, recommended
# or
dotnet add package LlmTornado # slim, minimal dependencies
```

Optional addons:

```bash
dotnet add package LlmTornado LlmTornado.Contrib # productivity, quality of life enhancements
```

## 🪄 Quick Inference

Inferencing across multiple providers is as easy as changing the `ChatModel` argument. Tornado instance can be constructed with multiple API keys, the correct key is then used based on the model automatically:

```csharp
TornadoApi api = new TornadoApi([
    new (LLmProviders.OpenAi, "OPEN_AI_KEY"),
    new (LLmProviders.Anthropic, "ANTHROPIC_KEY"),
    new (LLmProviders.Cohere, "COHERE_KEY"),
    new (LLmProviders.Google, "GOOGLE_KEY"),
    new (LLmProviders.Groq, "GROQ_KEY"),
    new (LLmProviders.DeepSeek, "DEEP_SEEK_KEY"),
    new (LLmProviders.Mistral, "MISTRAL_KEY"),
    new (LLmProviders.XAi, "XAI_KEY"),
    new (LLmProviders.Perplexity, "PERPLEXITY_KEY")
]);

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

## 🔮 Custom Providers

Instead of consuming commercial APIs, one can roll their own inference servers easily with [a myriad](https://github.com/janhq/awesome-local-ai) of tools available. Here is a simple demo for streaming response with Ollama, but the same approach can be used for any custom provider:

```cs
public static async Task OllamaStreaming()
{
    TornadoApi api = new TornadoApi(new Uri("http://localhost:11434")); // default Ollama port
    
    await api.Chat.CreateConversation(new ChatModel("falcon3:1b")) // <-- replace with your model
        .AppendUserInput("Why is the sky blue?")
        .StreamResponse(Console.Write);
}
```

https://github.com/user-attachments/assets/de62f0fe-93e0-448c-81d0-8ab7447ad780

## 🔎 Advanced Inference

### Streaming

Tornado offers several levels of abstraction, trading more details for more complexity. The simple use cases where only plaintext is needed can be represented in a terse format:

```cs
await api.Chat.CreateConversation(ChatModel.Anthropic.Claude3.Sonnet)
    .AppendSystemMessage("You are a fortune teller.")
    .AppendUserInput("What will my future bring?")
    .StreamResponse(Console.Write);
```

### Streaming with Rich content

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
- Supports streaming, functions/tools, modalities (text, images, audio, video, files), and strongly typed LLM plugins/connectors.
- Covered by 200+ tests.
- Great performance, nullability annotations.
- Maintained actively for two years, often with day 1 support for new features.
- The license will never change.

[![Star History Chart](https://api.star-history.com/svg?repos=lofcz/llmtornado&type=Date)](https://www.star-history.com/#lofcz/llmtornado&Date)

## 📚 Documentation

Most public classes, methods, and properties (90%+) are extensively XML documented. Feel free to open an issue here if you have any questions.

PRs are welcome!

## 💜 License

This library is licensed under the [MIT license](https://github.com/lofcz/LlmTornado/blob/master/LICENSE).
