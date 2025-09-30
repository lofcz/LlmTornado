namespace LlmTornado.Agents.Samples.DataModels;

public class CodeBuildInfo
{

    public string BuildPath { get; set; }

    public string ProjectName { get; set; }
    public ExecutableOutputResult? ExecutableResult { get; set; }

    public BuildOutputResult? BuildResult { get; set; }

    public CodeBuildInfo() { }

    public CodeBuildInfo(string buildPath, string projectName)
    {
        BuildPath = buildPath;
        ProjectName = projectName;
    }
}

