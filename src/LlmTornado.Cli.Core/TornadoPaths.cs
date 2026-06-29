namespace LlmTornado.Cli.Core;

/// <summary>
/// Central resolution of the CLI's skill/agent folders, so global and project locations share one
/// universal scheme:
/// <list type="bullet">
///   <item><description>Global root: the <c>TORNADO_HOME</c> environment variable when set, otherwise
///   the per-user application-data folder (<c>%APPDATA%/llmtornado</c> on Windows, the platform
///   equivalent elsewhere). The env var lets a host point everything at a custom location
///   (e.g. <c>/workspace/llmtornado</c> in a container).</description></item>
///   <item><description>Project root: <c>&lt;cwd&gt;/llmtornado</c>.</description></item>
/// </list>
/// Both roots use the same known subfolder names — <c>skills</c> and <c>agents</c>.
/// </summary>
public static class TornadoPaths
{
    /// <summary>Environment variable that overrides the global root for all CLI data.</summary>
    public const string HomeEnvVar = "TORNADO_HOME";

    /// <summary>Folder name used for the global root and the project subfolder.</summary>
    private const string FolderName = "llmtornado";

    public const string SkillsLeaf = "skills";
    public const string AgentsLeaf = "agents";
    public const string McpConfigLeaf = "mcp.json";

    /// <summary>
    /// The global root folder: <c>TORNADO_HOME</c> if set, otherwise <c>&lt;app-data&gt;/llmtornado</c>
    /// (<c>%APPDATA%</c> on Windows, <c>$XDG_CONFIG_HOME</c>/<c>~/.config</c> on Unix).
    /// </summary>
    public static string GlobalRoot()
    {
        string? env = Environment.GetEnvironmentVariable(HomeEnvVar);
        if (!string.IsNullOrWhiteSpace(env))
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(env));

        string configRoot = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // Some container/minimal Unix setups return an empty ApplicationData path.
        if (string.IsNullOrWhiteSpace(configRoot))
            configRoot = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(configRoot))
        {
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(profile))
                configRoot = Path.Combine(profile, ".config");
        }

        if (string.IsNullOrWhiteSpace(configRoot))
            configRoot = Path.GetFullPath(".config");

        return Path.GetFullPath(Path.Combine(configRoot, FolderName));
    }

    /// <summary>The project root folder for a working directory: <c>&lt;cwd&gt;/llmtornado</c>.</summary>
    public static string ProjectRoot(string? cwd = null)
        => Path.GetFullPath(Path.Combine(cwd ?? Directory.GetCurrentDirectory(), FolderName));

    public static string GlobalSkillsDirectory() => Path.Combine(GlobalRoot(), SkillsLeaf);
    public static string GlobalAgentsDirectory() => Path.Combine(GlobalRoot(), AgentsLeaf);

    public static string ProjectSkillsDirectory(string? cwd = null) => Path.Combine(ProjectRoot(cwd), SkillsLeaf);
    public static string ProjectAgentsDirectory(string? cwd = null) => Path.Combine(ProjectRoot(cwd), AgentsLeaf);

    public static string GlobalMcpConfig() => Path.Combine(GlobalRoot(), McpConfigLeaf);
    public static string ProjectMcpConfig(string? cwd = null) => Path.Combine(ProjectRoot(cwd), McpConfigLeaf);
}
