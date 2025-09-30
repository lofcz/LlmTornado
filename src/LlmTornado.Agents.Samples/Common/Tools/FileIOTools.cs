using LlmTornado.Agents.Samples.Common.Utility;
using System.ComponentModel;

namespace LlmTornado.Agents.Samples.Common.Tools;

public class FileIOTools
{
    public static string ProjectName { get; set; } = "FunctionApplication";

    public static string SafeWorkingDirectory
    {
        get => FileIOUtility.SafeWorkingDirectory ?? throw new InvalidOperationException("SafeWorkingDirectory is not set. Please set SafeWorkingDirectory before reading directories.");
        set => FileIOUtility.SafeWorkingDirectory = value;
    }

    [Description("Use this tool to read files already written")]
    public static string ReadFileTool([Description("file path of the file you wish to read.")] string filePath)
    {
        return FileIOUtility.ReadFile(filePath);
    }

    [Description("Use this tool to get all the file paths in the project")]
    public static string GetFilesTool()
    {
        return FileIOUtility.GetAllPaths(ProjectName);
    }
}


