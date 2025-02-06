using LlmTornado.Assistants;
using Newtonsoft.Json;

namespace LlmTornado.Common;

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
    public RankerType Ranker { get; set; }


    /// <summary>
    ///     The score threshold for the file search. All values must be a floating point number between 0 and 1.
    /// </summary>
    [JsonProperty("score_threshold")]
    public float ScoreThreshold { get; set; }
}