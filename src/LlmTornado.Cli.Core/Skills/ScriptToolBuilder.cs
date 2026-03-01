using System.Diagnostics;
using System.Text;
using LlmTornado.Common;

namespace LlmTornado.Cli.Core.Skills;

/// <summary>
/// Approval decision persisted for a script tool during the current session.
/// </summary>
public enum ScriptApprovalPolicy
{
    /// <summary>
    /// Prompt the user each time (default for first use).
    /// </summary>
    Ask,

    /// <summary>
    /// Always allow execution without prompting.
    /// </summary>
    AlwaysAllow,

    /// <summary>
    /// Never allow execution.
    /// </summary>
    NeverAllow
}

/// <summary>
/// Builds LlmTornado Tool instances from skill script files with approval gating.
/// </summary>
public static class ScriptToolBuilder
{
    private const int MaxOutputChars = 30_000;

    /// <summary>
    /// Per-session approval decisions keyed by tool name (e.g. "my-skill:extract").
    /// </summary>
    private static readonly Dictionary<string, ScriptApprovalPolicy> ApprovalPolicies = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Create Tool objects for all scripts in all enabled skills (no approval gating).
    /// </summary>
    public static List<Tool> BuildScriptTools(List<Skill> enabledSkills)
    {
        return BuildScriptTools(enabledSkills, null);
    }

    /// <summary>
    /// Create Tool objects for all scripts in all enabled skills.
    /// When <paramref name="approval"/> is provided, each script execution will be gated
    /// by the approval flow: first use prompts the user who can choose Allow / Always Allow / Never Allow.
    /// </summary>
    public static List<Tool> BuildScriptTools(List<Skill> enabledSkills, IToolApproval? approval)
    {
        List<Tool> tools = [];

        foreach (Skill skill in enabledSkills)
        {
            // Pre-approve any tools declared in the skill's allowed-tools frontmatter field
            if (approval is not null && skill.AllowedTools.Count > 0)
            {
                approval.PreApproveSkillTools(skill.AllowedTools);
            }

            foreach (SkillScript script in skill.Scripts)
            {
                string toolName = $"{skill.Name}:{Path.GetFileNameWithoutExtension(script.FileName)}";
                string description = $"Run script '{script.FileName}' from skill '{skill.Name}'. " +
                                     $"Extension: .{script.Extension}. Pass arguments as a single string.";

                // Capture for closure
                IToolApproval? capturedApproval = approval;
                string capturedToolName = toolName;
                Skill capturedSkill = skill;

                Tool tool = new(
                    new Func<string, string>(args =>
                    {
                        // Gate execution through the approval system
                        if (capturedApproval is not null)
                        {
                            string? denied = EnforceApproval(capturedApproval, capturedToolName, capturedSkill.Name, script.FileName);
                            if (denied is not null)
                                return denied;
                        }

                        return RunScript(script, args, capturedSkill.DirectoryPath);
                    }),
                    toolName,
                    description);

                tools.Add(tool);
            }
        }

        return tools;
    }

    /// <summary>
    /// Check the approval policy for a script tool. Returns an error message if denied, null if allowed.
    /// </summary>
    private static string? EnforceApproval(IToolApproval approval, string toolName, string skillName, string scriptFileName)
    {
        // Check if already auto-approved (e.g. from allowed-tools frontmatter)
        if (approval.IsAutoApproved(toolName))
            return null;

        // Check session-persisted policy
        if (ApprovalPolicies.TryGetValue(toolName, out ScriptApprovalPolicy policy))
        {
            return policy switch
            {
                ScriptApprovalPolicy.AlwaysAllow => null,
                ScriptApprovalPolicy.NeverAllow => $"Execution of '{scriptFileName}' from skill '{skillName}' is blocked (policy: never allow).",
                _ => PromptAndPersist(approval, toolName, skillName, scriptFileName)
            };
        }

        // First use — prompt user
        return PromptAndPersist(approval, toolName, skillName, scriptFileName);
    }

    /// <summary>
    /// Prompt the user for approval and persist their decision for the session.
    /// </summary>
    private static string? PromptAndPersist(IToolApproval approval, string toolName, string skillName, string scriptFileName)
    {
        string requestMessage = $"Skill '{skillName}' wants to execute script '{scriptFileName}'. Allow execution?";
        bool allowed = approval.HandleToolPermissionRequest(requestMessage).GetAwaiter().GetResult();

        if (allowed)
        {
            // For now, first approval sets AlwaysAllow for the session.
            // This can be extended to expose the 3-way choice (Allow / Always Allow / Never Allow) via IToolApproval.
            ApprovalPolicies[toolName] = ScriptApprovalPolicy.AlwaysAllow;
            return null;
        }

        ApprovalPolicies[toolName] = ScriptApprovalPolicy.NeverAllow;
        return $"Execution of '{scriptFileName}' from skill '{skillName}' was denied by user.";
    }

    /// <summary>
    /// Reset all session-persisted approval decisions.
    /// </summary>
    public static void ResetApprovalPolicies() => ApprovalPolicies.Clear();

    private static string RunScript(SkillScript script, string arguments, string workingDir)
    {
        try
        {
            string[] cmdParts = script.Command.Split(' ', 2);
            string fileName = cmdParts[0];
            string baseArgs = cmdParts.Length > 1 ? cmdParts[1] + " " : "";

            ProcessStartInfo psi = new()
            {
                FileName = fileName,
                Arguments = $"{baseArgs}\"{script.AbsolutePath}\" {arguments}",
                WorkingDirectory = workingDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using Process? process = Process.Start(psi);
            if (process is null)
                return "ERROR: Failed to start process.";

            // Read output with timeout
            StringBuilder output = new();
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null && output.Length < MaxOutputChars)
                    output.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null && output.Length < MaxOutputChars)
                    output.AppendLine($"[stderr] {e.Data}");
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            bool exited = process.WaitForExit(TimeSpan.FromMinutes(5));
            if (!exited)
            {
                process.Kill(true);
                return output + "\n[TIMEOUT: Process killed after 5 minutes]";
            }

            string result = output.ToString();
            if (result.Length > MaxOutputChars)
                result = result[..MaxOutputChars] + "\n[OUTPUT TRUNCATED]";

            return string.IsNullOrWhiteSpace(result) ? $"[exit code: {process.ExitCode}]" : result;
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }
}
