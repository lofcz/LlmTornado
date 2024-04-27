[![OpenAI](https://badgen.net/nuget/v1/LlmTornado)](https://www.nuget.org/packages/LlmTornado/)

# LLM Tornado - One .NET library to consume OpenAI, Anthropic, Cohere, Azure, and self-hosed APIs.

OpenAiNextGeneration is a simple .NET library to use with various OpenAI compatible providers, such
as [OpenAI](https://platform.openai.com/docs/api-reference), [Azure OpenAI](https://azure.microsoft.com/en-us/products/ai-services/openai-service),
and [KoboldCpp](https://github.com/LostRuins/koboldcpp/releases) (
v1.45.2+). Supports features such as function calling in conjunction with streaming, caches its own `HttpClient`s.

Supported features compared to [OpenAI-API-dotnet](https://github.com/OkGoDoIt/OpenAI-API-dotnet):

- Assistants, threads, and messages support.
- Includes new models.
- Parallel function calling.
- Improved memory usage, and function calling in conjunction with streaming.
- Manages its pool of HttpClients.
- Improved credentials passing.
- Supports OpenAI-compatible API providers, such as KoboldCpp.
- Improved Azure OpenAI integration.
- Nullability annotations.
- Calls are guaranteed not to throw, full response is included in the call result. (Fully supported in 3.0+)
- Actively maintained, [backed by a company I work for](https://www.scio.cz/).

Features scheduled for open-sourcing:

- High-level plugin API.
- Approximate token counting for streaming & function calling.

## ⚡Getting Started

Install the library via NuGet:

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

## 📖 Readme

* [Requirements](#requirements)
* [Installation](#install-from-nuget)
* [Authentication](#authentication)
* [Additonal Documentation](#documentation)
* [License](#license)

## Requirements

Unlike the original library, LlmTornado supports only .NET Core >= 6.0, if you need .NET Standard 2.0 /.NET Framework
support, please use [OpenAI-API-DotNet](https://github.com/OkGoDoIt/OpenAI-API-dotnet).

## Getting started

### Install from NuGet

Install package [`LlmTornado` from Nuget](https://www.nuget.org/packages/LlmTornado/). Here's how via the command line:

```powershell
Install-Package LlmTornado
```

### Authentication

Pass keys directly to `ApiAuthentication(string key)` constructor.

You use the `APIAuthentication` when you initialize the API as shown:

```csharp
// for example
var api = new OpenAiApi("YOUR_API_KEY"); // shorthand
// or
var api = new OpenAiApi(new APIAuthentication("YOUR_API_KEY")); // create object manually
```

You may optionally include an OpenAi Organization if multiple Organizations are under one account.

```csharp
// for example
var api = new OpenAiApi(new ApiAuthentication("YOUR_API_KEY", "org-yourOrgHere"));
```

## Documentation

Every single class, method, and property has extensive XML documentation, so it should show up automatically in
IntelliSense. That combined with the official OpenAI documentation should be enough to get started. Feel free to open an
issue here if you have any questions.

## License

This library is licensed under [MIT license](https://github.com/lofcz/LlmTornado/blob/master/LICENSE).
