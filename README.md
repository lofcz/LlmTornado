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

Switching the vendor is as easy as changing `ChatModel`! Tornado instance can be constructed with multiple API keys, the correct key is then used based on the model.

### Streaming

```cs
await api.Chat.CreateConversation(ChatModel.Anthropic.Claude3.Sonnet)
    .AppendSystemMessage("You are a fortune teller.")
    .AppendUserInput("What will my future bring?")
    .StreamResponse(Console.Write);
```

### Functions with deferred resolve

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

### Functions with immediate resolve

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

chat.AppendMessage(ChatMessageRole.System, "You are a helpful assistant");
Guid msgId = Guid.NewGuid();
chat.AppendMessage(ChatMessageRole.User, "What is the weather like today in Prague?", msgId);

await chat.StreamResponseRich(msgId, (x) =>
{
    sb.Append(x);
    return Task.CompletedTask;
}, functions =>
{
    List<FunctionResult> results = [];

    foreach (FunctionCall fn in functions)
    {
        results.Add(new FunctionResult(fn.Name, "A mild rain is expected around noon."));
    }

    return Task.FromResult(results);
}, null, null, null);


string response = sb.ToString();
Console.WriteLine(response);
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
