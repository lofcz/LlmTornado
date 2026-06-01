using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Vendor.Anthropic;
using Newtonsoft.Json;

namespace LlmTornado.Chat.Vendors.Anthropic;

/// <summary>
/// Beta headers used by Anthropic MCP features.
/// </summary>
public static class AnthropicMcpBetaHeaders
{
    /// <summary>
    /// MCP connector beta header for Messages API MCP server integration.
    /// </summary>
    public const string McpClient = "mcp-client-2025-11-20";

    /// <summary>
    /// MCP tunnels admin API beta header (tunnel provisioning and certificate management).
    /// </summary>
    public const string McpTunnels = "mcp-tunnels-2026-05-19";
}

/// <summary>
/// Configuration for routing to an MCP server exposed through an Anthropic MCP tunnel.
/// The resolved URL is passed in <c>mcp_servers</c> using the standard <c>url</c> type.
/// </summary>
public class AnthropicMcpTunnelConfig
{
    /// <summary>
    /// Subdomain routed by the tunnel proxy (for example, <c>echo</c> in <c>echo.example.tunnel.anthropic.com</c>).
    /// </summary>
    public string Subdomain { get; set; } = string.Empty;

    /// <summary>
    /// Base tunnel domain assigned in the Claude Console (for example, <c>example.tunnel.anthropic.com</c>).
    /// </summary>
    public string TunnelDomain { get; set; } = string.Empty;

    /// <summary>
    /// Path on the upstream MCP server. Defaults to <c>/mcp</c> (FastMCP streamable-http default).
    /// </summary>
    public string Path { get; set; } = "/mcp";

    /// <summary>
    /// Builds the routed tunnel URL for the Messages API <c>mcp_servers</c> array.
    /// </summary>
    public string BuildUrl()
    {
        if (string.IsNullOrWhiteSpace(Subdomain))
        {
            throw new ArgumentException("Subdomain is required.", nameof(Subdomain));
        }

        if (string.IsNullOrWhiteSpace(TunnelDomain))
        {
            throw new ArgumentException("TunnelDomain is required.", nameof(TunnelDomain));
        }

        string subdomain = Subdomain.Trim().TrimEnd('.');
        string domain = TunnelDomain.Trim().TrimStart('.').TrimEnd('.');
        string path = string.IsNullOrWhiteSpace(Path)
            ? "/"
            : Path.StartsWith('/') ? Path : $"/{Path}";

        return $"https://{subdomain}.{domain}{path}";
    }

    /// <summary>
    /// Creates tunnel configuration for a routed MCP server.
    /// </summary>
    public static AnthropicMcpTunnelConfig Create(string subdomain, string tunnelDomain, string path = "/mcp") =>
        new()
        {
            Subdomain = subdomain,
            TunnelDomain = tunnelDomain,
            Path = path
        };
}

/// <summary>
/// Per-tool configuration for an Anthropic MCP toolset.
/// </summary>
public class AnthropicMcpToolConfig
{
    /// <summary>
    /// Whether this tool is enabled.
    /// </summary>
    [JsonProperty("enabled")]
    public bool? Enabled { get; set; }

    /// <summary>
    /// When true, the tool description is deferred and loaded via tool search.
    /// </summary>
    [JsonProperty("defer_loading")]
    public bool? DeferLoading { get; set; }
}

/// <summary>
/// MCP toolset entry for the Messages API <c>tools</c> array (<c>mcp_toolset</c>).
/// </summary>
public class AnthropicMcpToolset : IVendorAnthropicChatRequestTool
{
    /// <summary>
    /// Tool type. Always <c>mcp_toolset</c>.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; } = "mcp_toolset";

    /// <summary>
    /// Name of the MCP server defined in <c>mcp_servers</c>.
    /// </summary>
    [JsonProperty("mcp_server_name")]
    public string McpServerName { get; set; } = string.Empty;

    /// <summary>
    /// Default configuration applied to all tools in this set.
    /// </summary>
    [JsonProperty("default_config")]
    public AnthropicMcpToolConfig? DefaultConfig { get; set; }

    /// <summary>
    /// Per-tool configuration overrides keyed by tool name.
    /// </summary>
    [JsonProperty("configs")]
    public Dictionary<string, AnthropicMcpToolConfig>? Configs { get; set; }

    /// <summary>
    /// Creates a toolset that enables all tools from the given MCP server.
    /// </summary>
    public static AnthropicMcpToolset ForServer(string mcpServerName) =>
        new() { McpServerName = mcpServerName };

    /// <summary>
    /// Creates a toolset allowlist for specific tools.
    /// </summary>
    public static AnthropicMcpToolset AllowTools(string mcpServerName, params string[] toolNames)
    {
        AnthropicMcpToolset toolset = new()
        {
            McpServerName = mcpServerName,
            DefaultConfig = new AnthropicMcpToolConfig { Enabled = false },
            Configs = toolNames.ToDictionary(name => name, _ => new AnthropicMcpToolConfig { Enabled = true })
        };

        return toolset;
    }

    /// <summary>
    /// Converts deprecated <see cref="AnthropicMcpConfiguration"/> into the current toolset format.
    /// </summary>
    internal static AnthropicMcpToolset FromLegacyConfiguration(string mcpServerName, AnthropicMcpConfiguration? configuration)
    {
        if (configuration?.AllowedTools is { Length: > 0 })
        {
            return AllowTools(mcpServerName, configuration.AllowedTools);
        }

        if (configuration?.Enabled == false)
        {
            return new AnthropicMcpToolset
            {
                McpServerName = mcpServerName,
                DefaultConfig = new AnthropicMcpToolConfig { Enabled = false }
            };
        }

        return ForServer(mcpServerName);
    }
}

/// <summary>
/// Serializes Anthropic outbound tools, including MCP toolsets.
/// </summary>
internal sealed class VendorAnthropicToolsJsonConverter : JsonConverter<List<IVendorAnthropicChatRequestTool>?>
{
    public override void WriteJson(JsonWriter writer, List<IVendorAnthropicChatRequestTool>? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartArray();
        foreach (IVendorAnthropicChatRequestTool tool in value)
        {
            switch (tool)
            {
                case VendorAnthropicToolFunction function:
                    serializer.Serialize(writer, function);
                    break;
                case AnthropicMcpToolset toolset:
                    serializer.Serialize(writer, toolset);
                    break;
                default:
                    serializer.Serialize(writer, tool);
                    break;
            }
        }

        writer.WriteEndArray();
    }

    public override List<IVendorAnthropicChatRequestTool>? ReadJson(JsonReader reader, Type objectType, List<IVendorAnthropicChatRequestTool>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotSupportedException("Anthropic tool deserialization is not supported.");
    }
}
