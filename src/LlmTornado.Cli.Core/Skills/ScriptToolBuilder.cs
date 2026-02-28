using System.Diagnostics;
using System.Text;
using LlmTornado.Common;

namespace LlmTornado.Cli.Core.Skills;

/// <summary>
/// Builds LlmTornado Tool instances from skill script files.
/// </summary>
internal static class ScriptToolBuilder
{
    private const int MaxOutputChars = 30_000;

    /// <summary>
    /// Create Tool objects for all scripts in all enabled skills.
    /// </summary>
    public static List<Tool> BuildScriptTools(List<Skill> enabledSkills)
    {
        List<Tool> tools = [];

        foreach (Skill skill in enabledSkills)
        {
            foreach (SkillScript script in skill.Scripts)
            {
                string toolName = $"{skill.Name}:{Path.GetFileNameWithoutExtension(script.FileName)}";
                string description = $"Run script '{script.FileName}' from skill '{skill.Name}'. " +
                                     $"Extension: .{script.Extension}. Pass arguments as a single string.";

                Tool tool = new(
                    new Func<string, string>(args => RunScript(script, args, skill.DirectoryPath)),
                    toolName,
                    description);

                tools.Add(tool);
            }
        }

        return tools;
    }

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
