[![LlmTornado](https://badgen.net/nuget/v/LlmTornado?v=302&icon=nuget&label=LlmTornado)](https://www.nuget.org/packages/LlmTornado)
[![LlmTornado.Contrib](https://badgen.net/nuget/v/LlmTornado.Contrib?v=302&icon=nuget&label=LlmTornado.Contrib)](https://www.nuget.org/packages/LlmTornado.Contrib)


# 🌪️ LLM Tornado - one .NET library to consume OpenAI, Anthropic, Google, DeepSeek, Cohere, Azure, Groq, and self-hosted APIs.

At least one new large language model is released each month. Wouldn't it be awesome if using the new, shiny model was as easy as switching one argument?
LLM Tornado acts as an aggregator allowing you to do just that. Think [SearX](https://github.com/searxng/searxng) but for LLMs!

OpenAI, Anthropic, Google, DeepSeek, Cohere, Azure, and Groq are currently supported, along with any OpenAI-compatible inference servers, such as [Ollama](https://github.com/ollama/ollama). Check the full Feature Matrix [here](https://github.com/lofcz/LlmTornado/blob/master/FeatureMatrix.md). 👈

⭐ Awesome things you can do with Tornado:
- [Chat with your documents](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/ChatDemo.cs#L722-L757)
- [Voice call with AI using your microphone](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/ChatDemo.cs#L905-L968)
- [Orchestrate Assistants](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/ThreadsDemo.cs#L331-L429)
- [Turn various modalities into embeddings](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/EmbeddingDemo.cs#L50-L75)
- [Generate images](https://github.com/lofcz/LlmTornado/blob/61d2a4732c88c45d4a8c053204ecdef807c34652/LlmTornado.Demo/ImagesDemo.cs#L10-L13)
- Switch providers mid-conversation (OpenAI, Cohere, and Anthropic, with parallel tools calling & streaming): 

https://github.com/lofcz/LlmTornado/assets/10260230/05c27b37-397d-4b4c-96a4-4138ade48dbe

... and a lot more! Now, instead of relying on one LLM provider, you can combine the unique strengths of many. Unlike OpenRouter and similar libraries, Tornado exposes these capabilities via seamlessly integrated `vendor extensions` which can be usually invoked in a few lines of code.

## ⚡Getting Started

Install LLM Tornado via NuGet:

```bash
dotnet add package LlmTornado
```

Optional: extra features and quality of life extension methods are distributed in `Contrib` addon:

```bash
dotnet add package LlmTornado LlmTornado.Contrib
```

## 🪄 Quick Inference

Inferencing across multiple providers is as easy as changing the `ChatModel` argument. Tornado instance can be constructed with multiple API keys, the correct key is then used based on the model automatically:

```csharp
TornadoApi api = new TornadoApi(new List<ProviderAuthentication>
{
    new ProviderAuthentication(LLmProviders.OpenAi, "OPEN_AI_KEY"),
    new ProviderAuthentication(LLmProviders.Anthropic, "ANTHROPIC_KEY"),
    new ProviderAuthentication(LLmProviders.Cohere, "COHERE_KEY"),
    new ProviderAuthentication(LLmProviders.Google, "GOOGLE_KEY"),
    new ProviderAuthentication(LLmProviders.Groq, "GROQ_KEY"),
    new ProviderAuthentication(LLmProviders.DeepSeek, "DEEP_SEEK_KEY")
});

List<ChatModel> models = [
    ChatModel.OpenAi.O3.Mini, ChatModel.Anthropic.Claude37.Sonnet,
    ChatModel.Cohere.Command.RPlus, ChatModel.Google.Gemini.Gemini2Flash,
    ChatModel.Groq.Meta.Llama370B, ChatModel.DeepSeep.Models.Chat
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

Tornado offers several levels of abstraction, trading more details for more complexity. The simple use cases where only plaintext is needed can be represented in a terse format.

```cs
await api.Chat.CreateConversation(ChatModel.Anthropic.Claude3.Sonnet)
    .AppendSystemMessage("You are a fortune teller.")
    .AppendUserInput("What will my future bring?")
    .StreamResponse(Console.Write);
```

### Tools with deferred resolve

When plaintext is insufficient, switch to `GetResponseRich()` or `StreamResponseRich()` APIs. Tools requested by the model can be resolved later and never returned to the model. This is useful in scenarios where we use the tools without intending to continue the conversation.

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

_`GetResponseRichSafe()` API is also available, which is guaranteed not to throw on the network level. The response is wrapped in a network-level wrapper, containing additional information. For production use cases, either use `try {} catch {}` on all the HTTP request producing Tornado APIs, or use the safe APIs._

### Tools with immediate resolve

Tools requested by the model can also be resolved and the results returned immediately. This has the benefit of automatically continuing the conversation.

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

### REPL

This interactive demo can be expanded into an end-user-facing interface in the style of ChatGPT. Shows how to use strongly typed tools together with streaming and resolve parallel tool calls.
`ChatStreamEventHandler` is a convenient class allowing subscription to only the events your use case needs.

```cs
public static async Task OpenAiFunctionsStreamingInteractive()
{
    // 1. set up a sample tool using a strongly typed model
    ChatPluginCompiler compiler = new ChatPluginCompiler();
    compiler.SetFunctions([
        new ChatPluginFunction("get_weather", "gets the current weather in a given city", [
            new ChatFunctionParam("city_name", "name of the city", ChatPluginFunctionAtomicParamTypes.String)
        ])
    ]);
    
    // 2. in this scenario, the conversation starts with the user asking for the current weather in two of the supported cities.
    // we can try asking for the weather in the third supported city (Paris) later.
    Conversation chat = api.Chat.CreateConversation(new ChatRequest
    {
        Model = ChatModel.OpenAi.Gpt4.Turbo,
        Tools = compiler.GetFunctions()
    }).AppendUserInput("Please call functions get_weather for Prague and Bratislava (two function calls).");

    // 3. repl
    while (true)
    {
        // 3.1 stream the response from llm
        await StreamResponse();

        // 3.2 read input
        while (true)
        {
            Console.WriteLine();
            Console.Write("> ");
            string? input = Console.ReadLine();

            if (input?.ToLowerInvariant() is "q" or "quit")
            {
                return;
            }
            
            if (!string.IsNullOrWhiteSpace(input))
            {
                chat.AppendUserInput(input);
                break;
            }
        }
    }

    async Task StreamResponse()
    {
        await chat.StreamResponseRich(new ChatStreamEventHandler
        {
            MessageTokenHandler = async (token) =>
            {
                Console.Write(token);
            },
            FunctionCallHandler = async (fnCalls) =>
            {
                foreach (FunctionCall x in fnCalls)
                {
                    if (!x.TryGetArgument("city_name", out string? cityName))
                    {
                        x.Result = new FunctionResult(x, new
                        {
                            result = "error",
                            message = "expected city_name argument"
                        }, null, true);
                        continue;
                    }

                    x.Result = new FunctionResult(x, new
                    {
                        result = "ok",
                        weather = cityName.ToLowerInvariant() is "prague" ? "A mild rain" : cityName.ToLowerInvariant() is "paris" ? "Foggy, cloudy" : "A sunny day"
                    }, null, true);
                }
            },
            AfterFunctionCallsResolvedHandler = async (fnResults, handler) =>
            {
                await chat.StreamResponseRich(handler);
            }
        });
    }
}
```

Other endpoints such as [Images](https://github.com/lofcz/LlmTornado/blob/master/LlmTornado.Demo/ImagesDemo.cs), [Embedding](https://github.com/lofcz/LlmTornado/blob/master/LlmTornado.Demo/EmbeddingDemo.cs), [Speech](https://github.com/lofcz/LlmTornado/blob/master/LlmTornado.Demo/SpeechDemo.cs), [Assistants](https://github.com/lofcz/LlmTornado/blob/master/LlmTornado.Demo/AssistantsDemo.cs), [Threads](https://github.com/lofcz/LlmTornado/blob/master/LlmTornado.Demo/ThreadsDemo.cs) and [Vision](https://github.com/lofcz/LlmTornado/blob/master/LlmTornado.Demo/VisionDemo.cs) are also supported!  
Check the links for simple to-understand examples!

## Why Tornado?

- 25,000+ installs on NuGet under previous names [Lofcz.Forks.OpenAI](https://www.nuget.org/packages/Lofcz.Forks.OpenAI), [OpenAiNg](https://www.nuget.org/packages/OpenAiNg).
- Used in commercial projects incurring charges of thousands of dollars monthly.
- The license will never change. Looking at you HashiCorp and Tiny.
- Supports streaming, functions/tools, modalities (images, audio), and strongly typed LLM plugins/connectors.
- Great performance, nullability annotations.
- Extensive tests suite.
- Maintained actively for two years, often with day 1 support for new features.

## Documentation

Most public classes, methods, and properties (90%+) are extensively XML documented. Feel free to open an issue here if you have any questions.

PRs are welcome!

## License

💜 This library is licensed under the [MIT license](https://github.com/lofcz/LlmTornado/blob/master/LICENSE).
