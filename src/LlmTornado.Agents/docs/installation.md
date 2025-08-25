# Installation

This guide covers how to install and set up LlmTornado.Agents in your project.

## Package Installation

### Using .NET CLI

```bash
dotnet add package LlmTornado.Agents
```

### Using Package Manager Console

```powershell
Install-Package LlmTornado.Agents
```

### Using PackageReference

Add this to your `.csproj` file:

```xml
<PackageReference Include="LlmTornado.Agents" Version="latest" />
```

## Dependencies

LlmTornado.Agents automatically includes these dependencies:

- **LlmTornado** - Core LLM integration library
- **LlmTornado.Mcp** - Model Context Protocol support
- **Newtonsoft.Json** - JSON serialization
- **Microsoft.Extensions.Options** - Configuration support

## Target Framework Requirements

- **.NET 8.0** or later for full feature support
- **.NET Standard 2.0** for basic functionality

## Provider Setup

### OpenAI

```csharp
using LlmTornado;
using LlmTornado.Code;

// Create client with OpenAI
TornadoApi client = new([
    new ProviderAuthentication(LLmProviders.OpenAi, "your-openai-api-key")
]);
```

### Anthropic

```csharp
TornadoApi client = new([
    new ProviderAuthentication(LLmProviders.Anthropic, "your-anthropic-api-key")
]);
```

### Multiple Providers

```csharp
TornadoApi client = new([
    new ProviderAuthentication(LLmProviders.OpenAi, "your-openai-key"),
    new ProviderAuthentication(LLmProviders.Anthropic, "your-anthropic-key"),
    new ProviderAuthentication(LLmProviders.Google, "your-google-key")
]);
```

## Environment Variables

Set up your API keys using environment variables:

### Windows (Command Prompt)
```cmd
set OPENAI_API_KEY=your-openai-api-key
set ANTHROPIC_API_KEY=your-anthropic-api-key
```

### Windows (PowerShell)
```powershell
$env:OPENAI_API_KEY="your-openai-api-key"
$env:ANTHROPIC_API_KEY="your-anthropic-api-key"
```

### macOS/Linux
```bash
export OPENAI_API_KEY="your-openai-api-key"
export ANTHROPIC_API_KEY="your-anthropic-api-key"
```

### .env File Support

Create a `.env` file in your project root:

```env
OPENAI_API_KEY=your-openai-api-key
ANTHROPIC_API_KEY=your-anthropic-api-key
GOOGLE_API_KEY=your-google-api-key
```

Then load it in your application:

```csharp
// Using DotNetEnv package
DotNetEnv.Env.Load();

TornadoApi client = new([
    new ProviderAuthentication(LLmProviders.OpenAi, Environment.GetEnvironmentVariable("OPENAI_API_KEY"))
]);
```

## Configuration Options

### Basic Configuration

```csharp
using LlmTornado.Agents;
using LlmTornado.Chat.Models;

TornadoAgent agent = new TornadoAgent(
    client: client,
    model: ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a helpful assistant."
);
```

### Advanced Configuration

```csharp
TornadoAgent agent = new TornadoAgent(
    client: client,
    model: ChatModel.OpenAi.Gpt41.V41Mini,
    instructions: "You are a specialized assistant.",
    outputSchema: typeof(MyOutputType),
    tools: [MyTool1, MyTool2],
    mcpServers: [mcpServer1, mcpServer2]
);
```

## Dependency Injection Setup

For ASP.NET Core or other DI containers:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

public void ConfigureServices(IServiceCollection services)
{
    // Add configuration
    services.Configure<TornadoOptions>(Configuration.GetSection("Tornado"));
    
    // Register TornadoApi
    services.AddSingleton<TornadoApi>(provider =>
    {
        var config = provider.GetRequiredService<IConfiguration>();
        return new TornadoApi([
            new ProviderAuthentication(LLmProviders.OpenAi, config["OPENAI_API_KEY"])
        ]);
    });
    
    // Register agents as needed
    services.AddTransient<TornadoAgent>(provider =>
    {
        var client = provider.GetRequiredService<TornadoApi>();
        return new TornadoAgent(
            client,
            ChatModel.OpenAi.Gpt41.V41Mini,
            "You are a helpful assistant."
        );
    });
}
```

## Verification

Test your installation with this simple verification:

```csharp
using LlmTornado.Agents;
using LlmTornado.Chat.Models;

try
{
    var client = new TornadoApi(Environment.GetEnvironmentVariable("OPENAI_API_KEY"));
    var agent = new TornadoAgent(client, ChatModel.OpenAi.Gpt41.V41Mini, "You are a test assistant.");
    
    var result = await agent.RunAsync("Say hello!");
    Console.WriteLine("✅ Installation successful!");
    Console.WriteLine($"Response: {result.Messages.Last().Content}");
}
catch (Exception ex)
{
    Console.WriteLine("❌ Installation issue:");
    Console.WriteLine(ex.Message);
}
```

## Troubleshooting

### Common Issues

**Missing API Key**
```
Error: API key not provided
```
Solution: Set your API key in environment variables or pass directly to TornadoApi constructor.

**Invalid Model**
```
Error: Model not found or not supported
```
Solution: Check the `ChatModel` class for available models for your provider.

**Network Issues**
```
Error: Unable to connect to API
```
Solution: Check your internet connection and API endpoint availability.

### Getting Help

- Check the [GitHub Issues](https://github.com/Johnny2x2/LlmTornado/issues)
- Review the [API Reference](api/tornado-agent.md)
- See [Examples](examples/) for working code

## Next Steps

Once installation is complete:

1. Follow the [Quick Start Guide](quick-start.md)
2. Learn about [Basic Agent Usage](basic-agent-usage.md)
3. Explore [Tool Integration](tool-integration.md)