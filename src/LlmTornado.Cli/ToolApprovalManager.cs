using System.Text.Json;
using System.Text.Json.Serialization;

namespace LlmTornado.Cli;

internal enum ToolApprovalState
{
    Unknown,
    AlwaysAllow,
    AlwaysDeny,
}

/// <summary>
/// Manages tool approval with first-use prompting and persistence.
/// </summary>
internal sealed class ToolApprovalManager
{
    private readonly Dictionary<string, ToolApprovalState> _approvals = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConsoleRenderer _renderer;

    public ToolApprovalManager(ConsoleRenderer renderer)
    {
        _renderer = renderer;
        LoadFromDisk();
    }

    /// <summary>
    /// The delegate to pass as toolPermissionHandle to the runtime.
    /// </summary>
    public async ValueTask<bool> HandleToolPermissionRequest(string requestMessage)
    {
        string toolName = ParseToolName(requestMessage);

        if (_approvals.TryGetValue(toolName, out ToolApprovalState state))
        {
            switch (state)
            {
                case ToolApprovalState.AlwaysAllow:
                    _renderer.WriteToolAutoApproved(toolName);
                    return true;
                case ToolApprovalState.AlwaysDeny:
                    _renderer.WriteToolAutoDenied(toolName);
                    return false;
            }
        }

        return await PromptForApproval(toolName, requestMessage);
    }

    private ValueTask<bool> PromptForApproval(string toolName, string requestMessage)
    {
        _renderer.WriteToolApprovalPrompt(requestMessage);

        while (true)
        {
            string? input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1":
                    return ValueTask.FromResult(true);

                case "2":
                    _approvals[toolName] = ToolApprovalState.AlwaysAllow;
                    SaveToDisk();
                    ConsoleRenderer.WriteInfo($"Tool '{toolName}' will be auto-approved in future.");
                    return ValueTask.FromResult(true);

                case "3":
                    return ValueTask.FromResult(false);

                case "4":
                    _approvals[toolName] = ToolApprovalState.AlwaysDeny;
                    SaveToDisk();
                    ConsoleRenderer.WriteInfo($"Tool '{toolName}' will be auto-denied in future.");
                    return ValueTask.FromResult(false);

                default:
                    ConsoleRenderer.WriteError("Please enter 1, 2, 3, or 4.");
                    break;
            }
        }
    }

    public Dictionary<string, ToolApprovalState> GetAllApprovals() => new(_approvals);

    public void ResetAll()
    {
        _approvals.Clear();
        SaveToDisk();
    }

    public bool ResetTool(string toolName)
    {
        bool removed = _approvals.Remove(toolName);
        if (removed) SaveToDisk();
        return removed;
    }

    /// <summary>
    /// Pre-approve tools from a skill's allowed-tools frontmatter.
    /// </summary>
    public void PreApproveSkillTools(IEnumerable<string> toolNames)
    {
        foreach (string name in toolNames)
        {
            _approvals.TryAdd(name, ToolApprovalState.AlwaysAllow);
        }
        SaveToDisk();
    }

    private static string ParseToolName(string requestMessage)
    {
        // requestMessage format: "Tool: {name}\nArguments: {args}"
        foreach (string line in requestMessage.Split('\n'))
        {
            if (line.StartsWith("Tool:", StringComparison.OrdinalIgnoreCase))
                return line[5..].Trim();
        }
        return requestMessage.Split('\n')[0].Trim();
    }

    private void LoadFromDisk()
    {
        Dictionary<string, string>? data = CliStorage.LoadJson<Dictionary<string, string>>(CliStorage.ToolApprovalsPath);
        if (data is null)
            return;

        foreach ((string tool, string state) in data)
        {
            _approvals[tool] = state switch
            {
                "allow" => ToolApprovalState.AlwaysAllow,
                "deny" => ToolApprovalState.AlwaysDeny,
                _ => ToolApprovalState.Unknown,
            };
        }
    }

    private void SaveToDisk()
    {
        Dictionary<string, string> data = new();
        foreach ((string tool, ToolApprovalState state) in _approvals)
        {
            if (state == ToolApprovalState.Unknown)
                continue;

            data[tool] = state switch
            {
                ToolApprovalState.AlwaysAllow => "allow",
                ToolApprovalState.AlwaysDeny => "deny",
                _ => "unknown",
            };
        }
        CliStorage.SaveJson(CliStorage.ToolApprovalsPath, data);
    }
}
