using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using LlmTornado.Cli.Core;
using LlmTornado.Cli.Core.Interactions;
using System.Globalization;

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
internal sealed class ToolApprovalManager : IToolApproval, IUserInteractionHandler
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

    public ValueTask<AskQuestionsInteractionResponse> AskQuestionsAsync(AskQuestionsInteractionRequest request, CancellationToken cancellationToken = default)
    {
        _renderer.WriteQuestionWorkflowStart(request);

        AskQuestionsInteractionResponse response = new();
        for (int index = 0; index < request.Questions.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            InteractiveQuestionDefinition question = request.Questions[index];
            _renderer.WriteQuestionPrompt(question, index + 1, request.Questions.Count);
            response.Answers.Add(PromptForQuestion(question));
            Console.WriteLine();
        }

        return ValueTask.FromResult(response);
    }

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

    /// <inheritdoc />
    public bool IsAutoApproved(string toolName)
    {
        return _approvals.TryGetValue(toolName, out ToolApprovalState state)
               && state == ToolApprovalState.AlwaysAllow;
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

    public int ApproveTools(IEnumerable<string> toolNames, bool overwriteExisting = true)
    {
        int count = 0;
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (string name in toolNames)
        {
            if (string.IsNullOrWhiteSpace(name) || !seen.Add(name))
                continue;

            if (overwriteExisting || !_approvals.ContainsKey(name))
                _approvals[name] = ToolApprovalState.AlwaysAllow;

            count++;
        }

        SaveToDisk();
        return count;
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

    private InteractiveQuestionAnswer PromptForQuestion(InteractiveQuestionDefinition question)
    {
        // On a real terminal, multi-select uses the interactive arrow/space picker.
        if (ConsoleRenderer.IsInteractiveMultiSelect(question))
        {
            ConsoleRenderer.MultiSelectResult result = _renderer.RunMultiSelect(question);
            return new InteractiveQuestionAnswer
            {
                Key = question.Key,
                Type = question.Type,
                SelectedValues = result.Values,
                UsedCustomAnswer = result.UsedCustom,
            };
        }

        while (true)
        {
            _renderer.WriteQuestionInputHint(question);
            string? input = Console.ReadLine()?.Trim();

            switch (question.Type)
            {
                case InteractiveQuestionInputType.SingleChoice:
                    if (TryHandleSingleChoice(question, input, out InteractiveQuestionAnswer? singleChoiceAnswer))
                        return singleChoiceAnswer;
                    break;

                case InteractiveQuestionInputType.MultiSelect:
                    if (TryHandleMultiSelect(question, input, out InteractiveQuestionAnswer? multiSelectAnswer))
                        return multiSelectAnswer;
                    break;

                case InteractiveQuestionInputType.YesNo:
                    if (TryHandleYesNo(question, input, out InteractiveQuestionAnswer? boolAnswer))
                        return boolAnswer;
                    break;

                case InteractiveQuestionInputType.Number:
                    if (TryHandleNumber(question, input, out InteractiveQuestionAnswer? numberAnswer))
                        return numberAnswer;
                    break;

                default:
                    if (TryHandleText(question, input, out InteractiveQuestionAnswer? textAnswer))
                        return textAnswer;
                    break;
            }
        }
    }

    private static bool TryHandleText(InteractiveQuestionDefinition question, string? input, [NotNullWhen(true)] out InteractiveQuestionAnswer? answer)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            if (question.Required)
            {
                ConsoleRenderer.WriteError("A value is required.");
                answer = null;
                return false;
            }

            answer = new InteractiveQuestionAnswer
            {
                Key = question.Key,
                Type = question.Type,
            };
            return true;
        }

        answer = new InteractiveQuestionAnswer
        {
            Key = question.Key,
            Type = question.Type,
            TextValue = input,
        };
        return true;
    }

    private static bool TryHandleYesNo(InteractiveQuestionDefinition question, string? input, [NotNullWhen(true)] out InteractiveQuestionAnswer? answer)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            if (question.Required)
            {
                ConsoleRenderer.WriteError("Please answer y or n.");
                answer = null;
                return false;
            }

            answer = new InteractiveQuestionAnswer
            {
                Key = question.Key,
                Type = question.Type,
            };
            return true;
        }

        bool? value = input.ToLowerInvariant() switch
        {
            "y" or "yes" or "true" => true,
            "n" or "no" or "false" => false,
            _ => null,
        };

        if (value is null)
        {
            ConsoleRenderer.WriteError("Please answer y or n.");
            answer = null;
            return false;
        }

        answer = new InteractiveQuestionAnswer
        {
            Key = question.Key,
            Type = question.Type,
            BooleanValue = value,
        };
        return true;
    }

    private static bool TryHandleNumber(InteractiveQuestionDefinition question, string? input, [NotNullWhen(true)] out InteractiveQuestionAnswer? answer)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            if (question.Required)
            {
                ConsoleRenderer.WriteError("A numeric value is required.");
                answer = null;
                return false;
            }

            answer = new InteractiveQuestionAnswer
            {
                Key = question.Key,
                Type = question.Type,
            };
            return true;
        }

        if (!decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value)
            && !decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value))
        {
            ConsoleRenderer.WriteError("Enter a valid number.");
            answer = null;
            return false;
        }

        if (question.MinValue is not null && value < question.MinValue.Value)
        {
            ConsoleRenderer.WriteError($"Value must be at least {question.MinValue.Value}.");
            answer = null;
            return false;
        }

        if (question.MaxValue is not null && value > question.MaxValue.Value)
        {
            ConsoleRenderer.WriteError($"Value must be at most {question.MaxValue.Value}.");
            answer = null;
            return false;
        }

        answer = new InteractiveQuestionAnswer
        {
            Key = question.Key,
            Type = question.Type,
            NumberValue = value,
        };
        return true;
    }

    private static bool TryHandleSingleChoice(InteractiveQuestionDefinition question, string? input, [NotNullWhen(true)] out InteractiveQuestionAnswer? answer)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            if (question.Required)
            {
                ConsoleRenderer.WriteError("Select an option.");
                answer = null;
                return false;
            }

            answer = new InteractiveQuestionAnswer
            {
                Key = question.Key,
                Type = question.Type,
            };
            return true;
        }

        if (!int.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
        {
            ConsoleRenderer.WriteError("Enter the number of the selected option.");
            answer = null;
            return false;
        }

        if (index == 0 && question.AllowCustomAnswer)
        {
            string? customAnswer = PromptForCustomAnswer(question.Required);
            if (customAnswer is null)
            {
                answer = null;
                return false;
            }

            answer = new InteractiveQuestionAnswer
            {
                Key = question.Key,
                Type = question.Type,
                TextValue = customAnswer,
                UsedCustomAnswer = true,
            };
            return true;
        }

        if (index < 1 || index > question.Options.Count)
        {
            ConsoleRenderer.WriteError("Enter a valid option number.");
            answer = null;
            return false;
        }

        answer = new InteractiveQuestionAnswer
        {
            Key = question.Key,
            Type = question.Type,
            TextValue = question.Options[index - 1].Value,
        };
        return true;
    }

    private static bool TryHandleMultiSelect(InteractiveQuestionDefinition question, string? input, [NotNullWhen(true)] out InteractiveQuestionAnswer? answer)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            if (question.Required)
            {
                ConsoleRenderer.WriteError("Select at least one option.");
                answer = null;
                return false;
            }

            answer = new InteractiveQuestionAnswer
            {
                Key = question.Key,
                Type = question.Type,
            };
            return true;
        }

        string[] parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            ConsoleRenderer.WriteError("Select at least one option.");
            answer = null;
            return false;
        }

        HashSet<string> values = new(StringComparer.OrdinalIgnoreCase);
        bool usedCustomAnswer = false;

        foreach (string part in parts)
        {
            if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
            {
                ConsoleRenderer.WriteError("Use comma-separated option numbers.");
                answer = null;
                return false;
            }

            if (index == 0 && question.AllowCustomAnswer)
            {
                string? customAnswer = PromptForCustomAnswer(true);
                if (customAnswer is null)
                {
                    answer = null;
                    return false;
                }

                values.Add(customAnswer);
                usedCustomAnswer = true;
                continue;
            }

            if (index < 1 || index > question.Options.Count)
            {
                ConsoleRenderer.WriteError("Enter valid option numbers.");
                answer = null;
                return false;
            }

            values.Add(question.Options[index - 1].Value);
        }

        if (question.Required && values.Count == 0)
        {
            ConsoleRenderer.WriteError("Select at least one option.");
            answer = null;
            return false;
        }

        answer = new InteractiveQuestionAnswer
        {
            Key = question.Key,
            Type = question.Type,
            SelectedValues = values.ToList(),
            UsedCustomAnswer = usedCustomAnswer,
        };
        return true;
    }

    private static string? PromptForCustomAnswer(bool required)
    {
        Console.Write(required ? "Custom answer: " : "Custom answer (press Enter to skip): ");
        string? input = Console.ReadLine()?.Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            if (required)
            {
                ConsoleRenderer.WriteError("A custom answer is required.");
                return null;
            }

            return string.Empty;
        }

        return input;
    }
}
