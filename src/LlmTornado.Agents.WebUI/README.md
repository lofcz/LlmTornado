# LlmTornado Chat Web Interface

A Blazor web application that provides a GitHub Copilot-style chat interface for the LlmTornado Agents API, featuring real-time streaming conversations.

## Features

- 🎨 **GitHub Copilot-style UI** - Modern glass-effect design with gradient backgrounds
- ⚡ **Real-time Streaming** - Server-Sent Events (SSE) for live conversation updates
- 🔧 **Event Visualization** - Shows reasoning, tool calls, and progress indicators
- 📱 **Responsive Design** - Works on desktop and mobile devices
- 🌪️ **LlmTornado Integration** - Direct integration with LlmTornado.Agents.API

## Prerequisites

- .NET 8.0 SDK
- OpenAI API Key (set as environment variable `OPENAI_API_KEY`)

## Quick Start

1. **Set your OpenAI API Key:**
   ```bash
   export OPENAI_API_KEY="your-api-key-here"
   ```

2. **Start the Agents API (in one terminal):**
   ```bash
   cd src/LlmTornado.Agents.API
   dotnet run
   ```

3. **Start the Chat Web Interface (in another terminal):**
   ```bash
   cd src/LlmTornado.Chat.Web
   dotnet run
   ```

4. **Open your browser:**
   Navigate to `https://localhost:5001` or `http://localhost:5000`

## Configuration

The chat interface can be configured via `appsettings.json`:

```json
{
  "ChatApi": {
    "BaseUrl": "https://localhost:7242"
  }
}
```

## Architecture

```
┌─────────────────────┐    SSE/HTTP    ┌─────────────────────┐
│ LlmTornado.Chat.Web │ ──────────────► │ LlmTornado.Agents.API│
│                     │                │                     │
│ • Blazor Server     │                │ • Streaming Chat    │
│ • SSE Client        │                │ • Agent Runtime     │
│ • GitHub Copilot UI │                │ • LLM Integration   │
└─────────────────────┘                └─────────────────────┘
```

## Supported Streaming Events

The interface displays real-time events including:

- **Text Deltas** - Streaming text as it's generated
- **Tool Invocations** - When agents call external tools
- **Reasoning** - AI thinking process visualization
- **Progress Indicators** - Status updates during processing
- **Error Handling** - Error states and recovery

## Development

### Building
```bash
dotnet build
```

### Running in Development
```bash
dotnet watch run
```

### Troubleshooting

1. **API Connection Issues**: Ensure the Agents API is running on the configured port
2. **Missing API Key**: Set the `OPENAI_API_KEY` environment variable
3. **CORS Issues**: The API includes CORS configuration for local development

## License

This project is part of the LlmTornado ecosystem and follows the same licensing terms.