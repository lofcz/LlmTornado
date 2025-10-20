# MCP Servers Anthropic
see [https://docs.claude.com/en/docs/mcp](https://docs.claude.com/en/docs/mcp) For more information

# Quick Start

```csharp
 Conversation chat = Program.Connect().Chat.CreateConversation(new ChatRequest
{
    Model = ChatModel.Anthropic.Claude45.Sonnet250929,
    Messages = [
        new ChatMessage(ChatMessageRoles.User, "Make a new branch on the Agent-Skills Repo")
    ],
    VendorExtensions = new ChatRequestVendorExtensions()
    {
        Anthropic = new ChatRequestVendorAnthropicExtensions
        {
            McpServers = [
            new AnthropicMcpServer(){
                    Name = "github",
                    Url = "https://api.githubcopilot.com/mcp/",
                    AuthorizationToken = Environment.GetEnvironmentVariable("GITHUB_API_KEY") ?? "github-api-key"
                }
            ]
        }
    }
});

ChatRichResponse response = await chat.GetResponseRich();
Console.WriteLine("Anthropic MCP Server Use:");
Console.WriteLine(response);
```

## As Agent
```csharp

TornadoAgent agent = new TornadoAgent(api, ChatModel.Anthropic.Claude45.Sonnet250929);

agent.Options.VendorExtensions = new ChatRequestVendorExtensions
{
     Anthropic = new ChatRequestVendorAnthropicExtensions
        {
            McpServers = [
            new AnthropicMcpServer(){
                    Name = "github",
                    Url = "https://api.githubcopilot.com/mcp/",
                    AuthorizationToken = Environment.GetEnvironmentVariable("GITHUB_API_KEY") ?? "github-api-key"
                }
            ]
        }
};

agent.Options.MaxTokens = 10024;
agent.Options.ReasoningBudget = 8000;
        
Conversation conv = await agent.RunAsync(appendMessages: [new ChatMessage(ChatMessageRoles.User,
    "Make a new branch on the Agent-Skills Repo")] );

Console.WriteLine(conv.Messages.Last().Content ?? "n/a");
```


