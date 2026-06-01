using System.Net.Http;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.Code;
using LlmTornado.Demo;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Tests;

/// <summary>
/// Unit and integration tests for Anthropic MCP tunnels and MCP connector serialization.
/// </summary>
[TestFixture]
public class AnthropicMcpTunnelTests
{
    private static IEndpointProvider CreateProvider() =>
        new TornadoApi(LLmProviders.Anthropic, "test-key").GetProvider(LLmProviders.Anthropic);

    [Test]
    public void TunnelConfig_BuildUrl_UsesSubdomainDomainAndPath()
    {
        AnthropicMcpTunnelConfig tunnel = AnthropicMcpTunnelConfig.Create("echo", "example.tunnel.anthropic.com", "/mcp");

        Assert.That(tunnel.BuildUrl(), Is.EqualTo("https://echo.example.tunnel.anthropic.com/mcp"));
    }

    [Test]
    public void TunnelConfig_BuildUrl_NormalizesPathWithoutLeadingSlash()
    {
        AnthropicMcpTunnelConfig tunnel = AnthropicMcpTunnelConfig.Create("docs", "acme.tunnel.anthropic.com", "mcp");

        Assert.That(tunnel.BuildUrl(), Is.EqualTo("https://docs.acme.tunnel.anthropic.com/mcp"));
    }

    [Test]
    public void McpServer_ForTunnel_BuildsResolvedUrl()
    {
        AnthropicMcpServer server = AnthropicMcpServer.ForTunnel(
            "echo",
            "echo",
            "example.tunnel.anthropic.com",
            "/mcp",
            "upstream-token");

        Assert.That(server.Name, Is.EqualTo("echo"));
        Assert.That(server.Url, Is.EqualTo("https://echo.example.tunnel.anthropic.com/mcp"));
        Assert.That(server.Tunnel, Is.Not.Null);
        Assert.That(server.AuthorizationToken, Is.EqualTo("upstream-token"));
    }

    [Test]
    public void McpTunnelRequest_SerializesServersAndToolsets()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude48.Opus,
            MaxTokens = 256,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Use the hello tool to greet tunnel.")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    McpServers =
                    [
                        AnthropicMcpServer.ForTunnel("echo", "echo", "example.tunnel.anthropic.com", "/mcp", "upstream-token")
                    ]
                }
            }
        };

        JObject body = ParseBody(request);

        JToken? server = body["mcp_servers"]?[0];
        Assert.That(server?["type"]?.ToString(), Is.EqualTo("url"));
        Assert.That(server?["name"]?.ToString(), Is.EqualTo("echo"));
        Assert.That(server?["url"]?.ToString(), Is.EqualTo("https://echo.example.tunnel.anthropic.com/mcp"));
        Assert.That(server?["authorization_token"]?.ToString(), Is.EqualTo("upstream-token"));
        Assert.That(server?["tool_configuration"], Is.Null);

        JToken? toolset = body["tools"]?.FirstOrDefault(t => t?["type"]?.ToString() == "mcp_toolset");
        Assert.That(toolset?["mcp_server_name"]?.ToString(), Is.EqualTo("echo"));
    }

    [Test]
    public void LegacyMcpConfiguration_MigratesToToolsetAllowlist()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude45.Sonnet250929,
            MaxTokens = 256,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    McpServers =
                    [
                        new AnthropicMcpServer
                        {
                            Name = "github",
                            Url = "https://api.githubcopilot.com/mcp/",
                            Configuration = new AnthropicMcpConfiguration
                            {
                                AllowedTools = ["create_branch", "list_repos"]
                            }
                        }
                    ]
                }
            }
        };

        JObject body = ParseBody(request);

        JToken? toolset = body["tools"]?.FirstOrDefault(t => t?["type"]?.ToString() == "mcp_toolset");
        Assert.That(toolset?["default_config"]?["enabled"]?.Value<bool>(), Is.False);
        Assert.That(toolset?["configs"]?["create_branch"]?["enabled"]?.Value<bool>(), Is.True);
        Assert.That(toolset?["configs"]?["list_repos"]?["enabled"]?.Value<bool>(), Is.True);
    }

    [Test]
    public void ExplicitMcpToolsets_AreSerialized()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude48.Opus,
            MaxTokens = 256,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    McpServers =
                    [
                        AnthropicMcpServer.ForTunnel("echo", "echo", "example.tunnel.anthropic.com")
                    ],
                    McpToolsets =
                    [
                        new AnthropicMcpToolset
                        {
                            McpServerName = "echo",
                            DefaultConfig = new AnthropicMcpToolConfig { DeferLoading = true }
                        }
                    ]
                }
            }
        };

        JObject body = ParseBody(request);

        JArray toolsets = new JArray(body["tools"]?.Where(t => t?["type"]?.ToString() == "mcp_toolset") ?? []);
        Assert.That(toolsets, Has.Count.EqualTo(1));
        Assert.That(toolsets[0]?["default_config"]?["defer_loading"]?.Value<bool>(), Is.True);
    }

    [Test]
    public void McpRequest_IncludesMcpClientBetaHeader()
    {
        ChatRequest request = new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude48.Opus,
            Messages = [new ChatMessage(ChatMessageRoles.User, "Hello")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    McpServers =
                    [
                        AnthropicMcpServer.ForTunnel("echo", "echo", "example.tunnel.anthropic.com")
                    ]
                }
            }
        };

        AnthropicEndpointProvider provider = new AnthropicEndpointProvider
        {
            Api = new TornadoApi(LLmProviders.Anthropic, "test-key")
        };

        HttpRequestMessage httpRequest = provider.OutboundMessage(
            "https://api.anthropic.com/v1/messages",
            HttpMethod.Post,
            "{}",
            false,
            request);

        string? betaHeader = httpRequest.Headers.TryGetValues("anthropic-beta", out IEnumerable<string>? values)
            ? string.Join(",", values)
            : null;

        Assert.That(betaHeader, Does.Contain(AnthropicMcpBetaHeaders.McpClient));
        Assert.That(betaHeader, Does.Not.Contain("mcp-client-2025-04-04"));
    }

    [Test]
    [Explicit("Requires Anthropic API key, MCP tunnel access, and ANTHROPIC_MCP_TUNNEL_* env vars")]
    public async Task CreateChatCompletion_WithTunnelMcpServer_ReturnsResponse()
    {
        string? apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        string? tunnelDomain = Environment.GetEnvironmentVariable("ANTHROPIC_MCP_TUNNEL_DOMAIN");
        string? subdomain = Environment.GetEnvironmentVariable("ANTHROPIC_MCP_TUNNEL_SUBDOMAIN") ?? "echo";
        string? path = Environment.GetEnvironmentVariable("ANTHROPIC_MCP_TUNNEL_PATH") ?? "/mcp";
        string? upstreamToken = Environment.GetEnvironmentVariable("ANTHROPIC_MCP_TUNNEL_AUTH_TOKEN");

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            if (await Program.SetupApi() && !string.IsNullOrWhiteSpace(Program.ApiKeys.Anthropic))
            {
                apiKey = Program.ApiKeys.Anthropic;
            }
        }

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(tunnelDomain))
        {
            Assert.Ignore("Set ANTHROPIC_API_KEY and ANTHROPIC_MCP_TUNNEL_DOMAIN to run MCP tunnel integration test.");
        }

        TornadoApi api = new TornadoApi(LLmProviders.Anthropic, apiKey);

        ChatResult result = await api.Chat.CreateChatCompletion(new ChatRequest
        {
            Model = ChatModel.Anthropic.Claude48.Opus,
            MaxTokens = 256,
            Messages = [new ChatMessage(ChatMessageRoles.User, "What MCP tools do you have available from the echo server?")],
            VendorExtensions = new ChatRequestVendorExtensions
            {
                Anthropic = new ChatRequestVendorAnthropicExtensions
                {
                    McpServers =
                    [
                        AnthropicMcpServer.ForTunnel("echo", subdomain, tunnelDomain, path, upstreamToken)
                    ]
                }
            }
        });

        Assert.That(result.Ok, Is.True, () => result.Exception?.Message ?? "Request failed");
        Assert.That(result.Choices, Is.Not.Empty);
        Assert.That(result.Choices![0].Message?.Content, Is.Not.Null.And.Not.Empty);
    }

    private static JObject ParseBody(ChatRequest request)
    {
        TornadoRequestContent serialized = request.Serialize(CreateProvider());
        return JObject.Parse(serialized.Body.ToString()!);
    }
}
