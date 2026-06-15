using System.Collections.Generic;

namespace LlmTornado.ManagedAgents.Anthropic;

/// <summary>
/// Constants for Claude Managed Agents API (beta).
/// </summary>
public static class VendorAnthropicManagedAgentsConstants
{
    /// <summary>
    /// Required <c>anthropic-beta</c> header value for Managed Agents requests.
    /// </summary>
    public const string BetaHeader = "managed-agents-2026-04-01";

    internal static Dictionary<string, object?> ApiHeaders => new()
    {
        ["anthropic-beta"] = BetaHeader
    };
}
