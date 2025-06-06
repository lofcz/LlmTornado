using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Assistants;

/// <summary>
/// Represents a collection of specific resources utilized by an assistant's tools.
/// Each tool may require its own unique set of configurations or parameters,
/// such as those needed for a tool that interprets code or searches files.
/// </summary>
public class ToolResources
{
    /// <summary>
    /// Configuration for the code interpreter tool.
    /// </summary>
    [JsonProperty("code_interpreter")]
    public CodeInterpreterConfig? CodeInterpreter { get; set; }

    /// <summary>
    /// Configuration for the file search tool, defining the necessary resources and settings.
    /// </summary>
    [JsonProperty("file_search")]
    public FileSearchConfig? FileSearch { get; set; }
}

/// <summary>
/// Configuration settings for the file search tool, including details
/// about associated resources such as vector store IDs used during the operation.
/// </summary>
public class FileSearchConfig
{
    /// <summary>
    /// Represents the configuration settings for file search functionality.
    /// </summary>
    public FileSearchConfig()
    {
    }

    /// <summary>
    /// Represents the configuration settings for a file search operation, including optional file IDs.
    /// </summary>
    /// <param name="fileIds"></param>
    public FileSearchConfig(List<string>? fileIds)
    {
        FileSearchFileIds = fileIds;
    }

    /// <summary>
    /// Represents a collection of vector store IDs associated with the file search tool configuration.
    /// </summary>
    [JsonProperty("vector_store_ids")]
    public IReadOnlyList<string>? FileSearchFileIds { get; set; }
}

/// <summary>
/// Configuration settings for the code interpreter tool.
/// This class contains properties such as lists of file IDs that are utilized as resources
/// by the code interpreter during its operations.
/// </summary>
public class CodeInterpreterConfig
{
    /// <summary>
    /// List of file IDs used by the code interpreter tool
    /// </summary>
    [JsonProperty("file_ids")]
    public IReadOnlyList<string>? CodeInterpreterFileIds { get; set; }
}