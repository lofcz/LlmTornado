[![LlmTornado](https://badgen.net/nuget/v/LlmTornado?v=301)](https://www.nuget.org/packages/LlmTornado)

# 🌪️ LLM Tornado - one .NET library to consume OpenAI, Anthropic, Cohere, Azure, and self-hosted APIs.

Each month at least one new large language model is released. Would it be awesome if using the new model was as easy as switching one argument?
LLM Tornado acts as an aggregator allowing you to do just that. Think [SearX](https://github.com/searxng/searxng) but for LLMs!

OpenAI, Cohere, Anthropic, and Azure are currently supported along with [KoboldCpp](https://github.com/LostRuins/koboldcpp) and [Ollama](https://github.com/ollama/ollama).

## ⚡Getting Started

Install LLM Tornado via NuGet:

```
Install-Package LlmTornado
```

## 🔮 Quick Inference

### Switching vendors

Switching the vendor is as easy as changing `ChatModel` argument. Tornado instance can be constructed with multiple API keys, the correct key is then used based on the model.

```csharp
TornadoApi api = new TornadoApi(new List<ProviderAuthentication>
{
    new ProviderAuthentication(LLmProviders.OpenAi, Program.ApiKeys.OpenAi),
    new ProviderAuthentication(LLmProviders.Anthropic, Program.ApiKeys.Anthropic),
    new ProviderAuthentication(LLmProviders.Cohere, Program.ApiKeys.Cohere)
});

List<ChatModel> models =
[
    ChatModel.OpenAi.Gpt4.Turbo,
    ChatModel.Anthropic.Claude3.Sonnet,
    ChatModel.Cohere.CommandRPlus
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

### Streaming

Tornado offers several levels of abstraction, trading more details for more complexity. The simple use cases where only plaintext is needed can be represented in a terse format.

```cs
await api.Chat.CreateConversation(ChatModel.Anthropic.Claude3.Sonnet)
    .AppendSystemMessage("You are a fortune teller.")
    .AppendUserInput("What will my future bring?")
    .StreamResponse(Console.Write);
```

_Examples listed below use `Program.Connect()` to construct the Tornado instance. Please use `new TornadoApi("API_KEY", LLmProviders.XXX)` instead._

### Tools with deferred resolve

When plaintext is insufficient, switch to `GetResponseRich()` or `StreamResponseRich()` APIs. Tools requested by the model can be resolved later and never returned to the model. This is useful in scenarios where we use the tools without intending to continue the conversation.

```cs
Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
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
StringBuilder sb = new StringBuilder();

Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.OpenAi.Gpt4.Turbo,
    Tools = [
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
});

chat.OnAfterToolsCall = async (result) =>
{
    string? str = await chat.GetResponse();

    if (str is not null)
    {
        sb.Append(str);
    }
};

chat.AppendMessage(ChatMessageRoles.System, "You are a helpful assistant");
Guid msgId = Guid.NewGuid();
chat.AppendMessage(ChatMessageRoles.User, "What is the weather like today in Prague?", msgId);

await chat.StreamResponseRich(msgId, (x) =>
{
    sb.Append(x);
    return Task.CompletedTask;
}, functions =>
{
    List<FunctionResult> results = functions.Select(fn => new FunctionResult(fn.Name, "A mild rain is expected around noon.")).ToList();
    return Task.FromResult(results);
}, null);


string response = sb.ToString();
Console.WriteLine(response);
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
    Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
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
- Supports streaming, functions/tools, and strongly typed LLM plugins/connectors.
- Great performance, nullability annotations.
- Extensive tests suite.
- Maintained actively for over half a year.

## Documentation

Every public class, method, and property has extensive XML documentation, using LLM Tornado should be intuitive if you've used any other LLM library previously. Feel free to open an
issue here if you have any questions.

PRs are welcome! ❤️

## License

This library is licensed under [MIT license](https://github.com/lofcz/LlmTornado/blob/master/LICENSE).
