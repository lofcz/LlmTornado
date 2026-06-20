using LlmTornado.Agents;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;

namespace LlmTornado.Cli.Core.Authoring;

/// <summary>
/// The "predefined LLM skill" that assists with authoring new skills and agent personas.
/// Holds the canned authoring prompts and runs a single, tool-less LLM turn to draft the
/// markdown body from the structured answers collected by the create/edit wizards.
/// </summary>
public static class AuthoringAssistant
{
    /// <summary>
    /// System prompt that turns the model into a SKILL.md authoring assistant.
    /// It must emit only the markdown body — the caller owns the YAML frontmatter.
    /// </summary>
    public const string SkillAuthorPrompt =
        """
        You are an expert author of Agent Skills (per the agentskills.io standard). Given a short brief
        about a skill, you write the SKILL.md *body* — the markdown instructions the agent reads once the
        skill is activated.

        Follow these rules:
        - Write clear, imperative, step-by-step instructions an LLM can follow to perform the skill.
        - Use markdown headings (##) to structure: purpose/overview, when to use it, the workflow/steps,
          and any important rules or gotchas.
        - Apply progressive disclosure: keep the body focused; if the skill would need bundled helper
          scripts, reference files, or assets, mention them as `scripts/<file>`, `references/<file>`, or
          `assets/<file>` so the user knows what to drop into those folders.
        - Be concrete and specific to the brief. Do not invent unrelated capabilities.

        Output ONLY the markdown body. Do NOT include YAML frontmatter, do NOT wrap the output in code
        fences, and do NOT restate the name/description as frontmatter — those are handled separately.
        """;

    /// <summary>
    /// System prompt that turns the model into an agent-persona authoring assistant.
    /// It must emit only the markdown body — the caller owns the YAML frontmatter.
    /// </summary>
    public const string AgentAuthorPrompt =
        """
        You are an expert at writing system prompts for specialized AI agent personas in a CLI coding
        assistant. Given a short brief about a persona, you write the persona's instruction body — the
        markdown the agent uses as its system prompt.

        Follow these rules:
        - Write in the second person ("You are ...") and define the persona's role, priorities, and tone.
        - Describe how it should approach tasks, what it should emphasize, and any boundaries or things it
          should avoid.
        - If the brief mentions enabled skills or blocked tools, reflect that focus in the guidance, but do
          not list them as frontmatter.
        - Keep it tight and actionable — a few focused paragraphs or short sections, not a manual.

        Output ONLY the markdown body. Do NOT include YAML frontmatter, do NOT wrap the output in code
        fences.
        """;

    /// <summary>
    /// Run a single tool-less LLM turn with the given authoring prompt and structured brief, returning
    /// the drafted markdown body (frontmatter/code-fence stripped defensively).
    /// </summary>
    public static async Task<string> DraftAsync(
        TornadoApi api,
        ChatModel model,
        string systemPrompt,
        string brief,
        CancellationToken cancellationToken = default)
    {
        TornadoAgent agent = new(api, model, name: "AuthoringAssistant", instructions: systemPrompt);

        Conversation conversation = await TornadoRunner.RunAsync(
            agent, brief, singleTurn: true, cancellationToken: cancellationToken);

        ChatMessage? last = conversation.Messages
            .LastOrDefault(m => m.Role == ChatMessageRoles.Assistant)
            ?? conversation.Messages.LastOrDefault();

        return CleanBody(last?.Content ?? string.Empty);
    }

    /// <summary>
    /// Defensively strip a leading YAML frontmatter block and surrounding ```/```markdown fences that a
    /// model may emit despite instructions, so the body is clean before we attach our own frontmatter.
    /// </summary>
    public static string CleanBody(string raw)
    {
        string text = raw.Trim();

        // Strip a single wrapping code fence (``` or ```markdown ... ```).
        if (text.StartsWith("```"))
        {
            int firstNewline = text.IndexOf('\n');
            int lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline > 0 && lastFence > firstNewline)
                text = text[(firstNewline + 1)..lastFence].Trim();
        }

        // Strip a leading frontmatter block if the model added one anyway.
        if (text.StartsWith("---"))
        {
            int secondDelimiter = text.IndexOf("\n---", 3, StringComparison.Ordinal);
            if (secondDelimiter > 0)
            {
                int afterDelimiter = text.IndexOf('\n', secondDelimiter + 1);
                if (afterDelimiter > 0)
                    text = text[(afterDelimiter + 1)..].Trim();
            }
        }

        return text;
    }
}
