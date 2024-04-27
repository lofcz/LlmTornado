[![LlmTornado](https://badgen.net/nuget/v/LlmTornado)](https://www.nuget.org/packages/LlmTornado)

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

```csharp
var api = new LlmTornado.OpenAiApi("YOUR_API_KEY");
var result = await api.Completions.GetCompletion("One Two Three One Two");
Console.WriteLine(result);
// should print something starting with "Three"
```

## Why Tornado?

- 25,000+ installs on NuGet under previous names [Lofcz.Forks.OpenAI](https://www.nuget.org/packages/Lofcz.Forks.OpenAI), [OpenAiNg](https://www.nuget.org/packages/OpenAiNg).
- Used in commercial projects incurring charges of thousands of dollars monthly.
- The license will never change. Looking at you HashiCorp and Tiny.
- Supports streaming, functions/tools, and strongly typed LLM plugins/connectors.
- Great performance, nullability annotations.
- Maintained actively for over half a year.

## Documentation

Every public class, method, and property has extensive XML documentation, using LLM Tornado should be intuitive if you've used any other LLM library previously. Feel free to open an
issue here if you have any questions.

PRs are welcome! ❤️

## License

This library is licensed under [MIT license](https://github.com/lofcz/LlmTornado/blob/master/LICENSE).
