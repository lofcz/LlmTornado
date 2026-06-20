using LlmTornado.Cli.Core.Interactions;

namespace LlmTornado.Cli.Commands;

/// <summary>
/// Small helpers shared by the /skill and /agent authoring wizards: answer extraction and
/// the common "save location" question.
/// </summary>
internal static class WizardSupport
{
    public const string LocationProject = "project";
    public const string LocationGlobal = "global";

    /// <summary>
    /// Build the standard project-vs-global save-location question.
    /// </summary>
    public static InteractiveQuestionDefinition SaveLocationQuestion(string projectHint, string globalHint) => new()
    {
        Key = "location",
        Prompt = "Where should this be saved?",
        Type = InteractiveQuestionInputType.SingleChoice,
        Required = true,
        Options =
        [
            new InteractiveQuestionOption { Value = LocationProject, Label = "Project", Description = projectHint },
            new InteractiveQuestionOption { Value = LocationGlobal, Label = "Global", Description = globalHint },
        ],
    };

    /// <summary>
    /// Build a multi-select question from pre-built options (sorted by label) with a custom-answer escape
    /// hatch and a graceful note when the list is empty. Shared by the tool and skill pickers so they
    /// behave identically.
    /// </summary>
    public static InteractiveQuestionDefinition MultiSelectQuestion(
        string key, string prompt, string description, string emptyNote,
        IEnumerable<InteractiveQuestionOption> options, bool allowCustom = true, bool required = false)
    {
        List<InteractiveQuestionOption> sorted =
            [.. options.OrderBy(o => o.Label, StringComparer.OrdinalIgnoreCase)];
        return new InteractiveQuestionDefinition
        {
            Key = key,
            Prompt = prompt,
            Type = InteractiveQuestionInputType.MultiSelect,
            Required = required,
            AllowCustomAnswer = allowCustom,
            Description = sorted.Count > 0 ? description : $"{description} ({emptyNote})",
            Options = sorted,
        };
    }

    /// <summary>
    /// Build a multi-select question listing the currently-registered tool names so the user can pick
    /// tools from a list instead of typing exact ids. A custom answer is always allowed for tools that
    /// aren't registered yet (e.g. a skill's own not-yet-loaded scripts).
    /// </summary>
    public static InteractiveQuestionDefinition ToolSelectQuestion(
        string key, string prompt, string description, IEnumerable<string> toolNames, bool required = false)
    {
        IEnumerable<InteractiveQuestionOption> options = toolNames
            .Distinct()
            .Select(n => new InteractiveQuestionOption { Value = n, Label = n });
        return MultiSelectQuestion(key, prompt, description,
            "no tools registered yet — use a custom answer or leave blank", options, required: required);
    }

    /// <summary>Ask a single yes/no confirmation question. Returns true only on an explicit "yes".</summary>
    public static async Task<bool> ConfirmAsync(this IUserInteractionHandler interaction, string title, string prompt)
    {
        AskQuestionsInteractionResponse response = await interaction.AskQuestionsAsync(new AskQuestionsInteractionRequest
        {
            Title = title,
            Questions =
            [
                new InteractiveQuestionDefinition
                {
                    Key = "confirm", Prompt = prompt, Type = InteractiveQuestionInputType.YesNo, Required = true,
                },
            ],
        });
        return response.Find("confirm")?.BooleanValue == true;
    }

    public static InteractiveQuestionAnswer? Find(this AskQuestionsInteractionResponse response, string key) =>
        response.Answers.FirstOrDefault(a => a.Key == key);

    /// <summary>Trimmed text answer for a key, or the supplied fallback when blank/missing.</summary>
    public static string Text(this AskQuestionsInteractionResponse response, string key, string fallback = "")
    {
        string? value = response.Find(key)?.TextValue?.Trim();
        return string.IsNullOrEmpty(value) ? fallback : value;
    }

    /// <summary>Single selected value for a choice question, or fallback.</summary>
    public static string Choice(this AskQuestionsInteractionResponse response, string key, string fallback)
    {
        InteractiveQuestionAnswer? answer = response.Find(key);
        return answer?.SelectedValues.FirstOrDefault() ?? answer?.TextValue ?? fallback;
    }

    /// <summary>All selected values for a multi-select question.</summary>
    public static List<string> Selected(this AskQuestionsInteractionResponse response, string key) =>
        response.Find(key)?.SelectedValues ?? [];
}
