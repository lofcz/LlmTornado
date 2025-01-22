using System.Text.Json.Serialization;

namespace LlmTornado.VectorStores;

/// <summary>
/// Represents the counts of files in different processing states within a vector store.
/// </summary>
public class FileCountInfo
{
    /// <summary>
    /// The number of files that are currently being processed.
    /// </summary>
    [JsonPropertyName("in_progress")]
    public int InProgress { get; set; }

    /// <summary>
    /// The number of files that have been successfully processed.
    /// </summary>
    [JsonPropertyName("completed")]
    public int Completed { get; set; }

    /// <summary>
    /// The number of files that have failed to process.
    /// </summary>
    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    /// <summary>
    /// The number of files that were cancelled.
    /// </summary>
    [JsonPropertyName("cancelled")]
    public int Cancelled { get; set; }

    /// <summary>
    /// The total number of files.
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }
}