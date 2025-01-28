using System.Collections.Generic;
using Newtonsoft.Json;

namespace LlmTornado.Assistants;

/// <summary>
///     A set of resources that are used by the assistant's tools.
///     The resources are specific to the type of tool. For example,
///     the code_interpreter tool requires a list of file IDs, while the file_search tool requires a list of vector store IDs.
/// </summary>
public class ToolResources
{
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("code_interpreter")]
    public CodeInterpreterConfig? CodeInterpreter { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    [JsonProperty("file_search")]
    public FileSearchConfig? FileSearch { get; set; }
    
}

/// <summary>
/// 
/// </summary>
public class FileSearchConfig
{
    /// <summary>
    ///     Constructs empty file search config
    /// </summary>
    public FileSearchConfig()
    {
    }
    
    /// <summary>
    ///     Constructs file search config with file IDs
    /// </summary>
    /// <param name="fileIds"></param>
    public FileSearchConfig(List<string>? fileIds)
    {
        FileSearchFileIds = fileIds;
    }
    
    /// <summary>
    ///     List of vector store IDs for file search tool
    /// </summary>
    [JsonProperty("vector_store_ids")]
    public IReadOnlyList<string>? FileSearchFileIds { get; set; }
}

/// <summary>
/// 
/// </summary>
public class CodeInterpreterConfig
{
    /// <summary>
    ///     List of file IDs for code interpreter tool
    /// </summary>
    [JsonProperty("file_ids")]
    public IReadOnlyList<string>? CodeInterpreterFileIds { get; set; }
}