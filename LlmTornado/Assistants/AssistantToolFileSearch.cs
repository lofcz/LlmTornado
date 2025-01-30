using Newtonsoft.Json;

namespace LlmTornado.Assistants;

/// <summary>
///     Represents a tool object of type file_search
/// </summary>
public class AssistantToolFileSearch : AssistantTool
{
    /// <summary>
    ///     The file_search tool with default settings
    /// </summary>
    public static AssistantToolFileSearch Default => new AssistantToolFileSearch();
    /// <summary>
    ///     Creates a new file_search type tool
    /// </summary>
    public AssistantToolFileSearch()
    {
        Type = "file_search";
    }

    /// <summary>
    ///     Creates a new file_search type tool
    /// </summary>
    /// <param name="fileSearchConfig"></param>
    public AssistantToolFileSearch(ToolFileSearchConfig fileSearchConfig) : this()
    {
        FileSearchConfig = fileSearchConfig;
    }

    /// <summary>
    ///     Configuration for the file search tool
    /// </summary>
    [JsonProperty("file_search", Required = Required.Default)]
    public ToolFileSearchConfig? FileSearchConfig { get; set; }
}

/// <summary>
///     Configuration for the file search tool
/// </summary>
public class ToolFileSearchConfig
{
    /// <summary>
    ///     The maximum number of results the file search tool should output.
    ///     The default is 20 for gpt-4* models and 5 for gpt-3.5-turbo.
    ///     This number should be between 1 and 50 inclusive.
    /// </summary>
    [JsonProperty("max_num_result")]
    public int? MaxNumResult { get; set; } = 20;

    /// <summary>
    ///     The ranking options for the file search.
    ///     If not specified, the file search tool will use the auto ranker and a score_threshold of 0.
    /// </summary>
    [JsonProperty("ranking_options")]
    public RankingOptions? RankingOptions { get; set; }
}

/// <summary>
///     The ranking options for the file search.
///     If not specified, the file search tool will use the auto ranker and a score_threshold of 0.
/// </summary>
public class RankingOptions
{
    /// <summary>
    ///     The ranker to use for the file search. If not specified will use the auto ranker.
    /// </summary>
    [JsonProperty("ranker")]
    public string Ranker { get; set; } = "auto";


    /// <summary>
    ///     The score threshold for the file search. All values must be a floating point number between 0 and 1.
    /// </summary>
    [JsonProperty("score_threshold")]
    public float ScoreThreshold { get; set; }
}