using System.Text;
using System.Text.Json;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Infra;

namespace LlmTornado.Cli.Core.Tools;

/// <summary>
/// LLM-based per-turn tool optimizer.
/// When total tool count exceeds the configured threshold, uses a cheap/fast LLM
/// to select the most relevant tools for the current user message.
/// Uses structured output with ToolParamListEnum to constrain the LLM response
/// to only valid tool names.
/// </summary>
internal sealed class ToolOptimizer
{
    private static readonly HashSet<string> BuiltInToolNames =
    [
        "load_skill",
        "list_skills",
        "read_reference",
    ];

    private readonly TornadoApi _api;
    private readonly ChatModel _model;
    private readonly int _maxTools;

    public ToolOptimizer(TornadoApi api, ChatModel model, int maxTools)
    {
        _api = api;
        _model = model;
        _maxTools = maxTools;
    }

    /// <summary>
    /// Optimize the tool list for a specific user message.
    /// Returns a filtered list containing built-in tools + the most relevant candidate tools.
    /// If optimization is not needed (count within limit) or fails, returns the full list.
    /// </summary>
    public async Task<ToolOptimizationResult> OptimizeAsync(
        List<Tool> allTools,
        string userMessage,
        CancellationToken ct = default)
    {
        // Separate built-in tools from candidates
        List<Tool> builtInTools = [];
        List<Tool> candidateTools = [];

        foreach (Tool tool in allTools)
        {
            string name = tool.ResolvedName;
            if (BuiltInToolNames.Contains(name))
                builtInTools.Add(tool);
            else
                candidateTools.Add(tool);
        }

        int budget = _maxTools - builtInTools.Count;

        // No optimization needed if within budget
        if (candidateTools.Count <= budget)
        {
            return new ToolOptimizationResult
            {
                Tools = allTools,
                WasOptimized = false,
                OriginalCount = allTools.Count,
                SelectedCount = allTools.Count,
            };
        }

        try
        {
            List<string> selectedNames = await SelectToolsAsync(candidateTools, budget, userMessage, ct);

            if (selectedNames.Count == 0)
            {
                // Fallback: LLM returned empty selection
                return ToolOptimizationResult.Fallback(allTools, "empty selection from optimizer");
            }

            // Build the name lookup set for filtering
            HashSet<string> selectedSet = new(selectedNames, StringComparer.OrdinalIgnoreCase);

            List<Tool> filteredTools = [..builtInTools];
            foreach (Tool tool in candidateTools)
            {
                if (selectedSet.Contains(tool.ResolvedName))
                    filteredTools.Add(tool);
            }

            // Ensure we got at least some tools (guard against mismatch)
            if (filteredTools.Count <= builtInTools.Count)
            {
                return ToolOptimizationResult.Fallback(allTools, "no matching tools after filtering");
            }

            return new ToolOptimizationResult
            {
                Tools = filteredTools,
                WasOptimized = true,
                OriginalCount = allTools.Count,
                SelectedCount = filteredTools.Count,
            };
        }
        catch (Exception ex)
        {
            return ToolOptimizationResult.Fallback(allTools, ex.Message);
        }
    }

    /// <summary>
    /// Calls the cheap LLM with structured output to select the most relevant tools.
    /// The response schema uses ToolParamListEnum constraining output to valid tool names.
    /// </summary>
    private async Task<List<string>> SelectToolsAsync(
        List<Tool> candidateTools,
        int budget,
        string userMessage,
        CancellationToken ct)
    {
        // Build the tool catalog for the prompt
        List<string> toolNames = candidateTools.Select(t => t.ResolvedName).Distinct().ToList();

        StringBuilder catalogBuilder = new();
        foreach (Tool tool in candidateTools)
        {
            string name = tool.ResolvedName;
            string desc = tool.ResolvedDescription;
            catalogBuilder.AppendLine(string.IsNullOrEmpty(desc) ? $"- {name}" : $"- {name}: {desc}");
        }

        // Build the structured output schema with ToolParamListEnum
        // This constrains the model's output to only valid tool names
        ChatRequestResponseFormats responseFormat = ChatRequestResponseFormats.StructuredJson(
        [
            new ToolParam("selected_tools", new ToolParamListEnum(
                $"Select up to {budget} tool names most relevant to the user's request.",
                toolNames))
        ], "tool_selection");

        Conversation conv = _api.Chat.CreateConversation(new ChatRequest
        {
            Model = _model,
            ResponseFormat = responseFormat,
        });

        conv.AppendSystemMessage(
            $"You are a tool selector. Given a user's message and a catalog of available tools, " +
            $"select the {budget} tools most relevant to fulfilling the user's request. " +
            $"Consider what operations the user might need and pick tools that would be useful. " +
            $"If fewer than {budget} tools are relevant, select only the relevant ones.");

        conv.AppendUserInput(
            $"User message: {userMessage}\n\n" +
            $"Available tools:\n{catalogBuilder}");

        RestDataOrException<ChatChoice> result = await conv.GetResponseSafe(ct);

        if (result.Data is null)
        {
            throw new InvalidOperationException(
                result.Exception?.Message ?? "Optimizer LLM returned no response.");
        }

        // Parse the structured JSON response
        string? responseText = result.Data.Message?.Content;
        if (string.IsNullOrWhiteSpace(responseText))
        {
            throw new InvalidOperationException("Optimizer LLM returned empty content.");
        }

        return ParseSelectedTools(responseText);
    }

    /// <summary>
    /// Parse the structured JSON response to extract the selected_tools array.
    /// Expected format: { "selected_tools": ["tool_a", "tool_b", ...] }
    /// </summary>
    private static List<string> ParseSelectedTools(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        if (root.TryGetProperty("selected_tools", out JsonElement toolsElement) &&
            toolsElement.ValueKind == JsonValueKind.Array)
        {
            List<string> tools = [];
            foreach (JsonElement item in toolsElement.EnumerateArray())
            {
                string? name = item.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                    tools.Add(name);
            }

            return tools;
        }

        throw new InvalidOperationException(
            $"Unexpected response format from optimizer: {json[..Math.Min(json.Length, 200)]}");
    }
}

/// <summary>
/// Result of a tool optimization pass.
/// </summary>
internal sealed class ToolOptimizationResult
{
    /// <summary>
    /// The (potentially filtered) tool list to use for this turn.
    /// </summary>
    public required List<Tool> Tools { get; init; }

    /// <summary>
    /// Whether optimization was actually performed (vs. passthrough).
    /// </summary>
    public required bool WasOptimized { get; init; }

    /// <summary>
    /// Total tool count before optimization.
    /// </summary>
    public required int OriginalCount { get; init; }

    /// <summary>
    /// Tool count after optimization.
    /// </summary>
    public required int SelectedCount { get; init; }

    /// <summary>
    /// If optimization was skipped due to a fallback, the reason.
    /// </summary>
    public string? FallbackReason { get; init; }

    public static ToolOptimizationResult Fallback(List<Tool> allTools, string reason)
    {
        return new ToolOptimizationResult
        {
            Tools = allTools,
            WasOptimized = false,
            OriginalCount = allTools.Count,
            SelectedCount = allTools.Count,
            FallbackReason = reason,
        };
    }
}
