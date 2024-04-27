using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.FineTuning;

[Obsolete("use FineTuneJobResponse")]
public sealed class FineTuneJob
{
    [JsonInclude] [JsonProperty("object")] public string Object { get; private set; }

    [JsonInclude] [JsonProperty("id")] public string Id { get; private set; }

    [JsonInclude] [JsonProperty("model")] public string Model { get; private set; }

    [JsonInclude]
    [JsonProperty("created_at")]
    public int? CreateAtUnixTimeSeconds { get; private set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Obsolete("Use CreateAtUnixTimeSeconds")]
    public int? CreatedAtUnixTime => CreateAtUnixTimeSeconds;

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? CreatedAt
        => CreateAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(CreateAtUnixTimeSeconds.Value).DateTime
            : null;

    [JsonInclude]
    [JsonProperty("finished_at")]
    public int? FinishedAtUnixTimeSeconds { get; private set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Obsolete("Use FinishedAtUnixTimeSeconds")]
    public int? FinishedAtUnixTime => CreateAtUnixTimeSeconds;

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? FinishedAt
        => FinishedAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(FinishedAtUnixTimeSeconds.Value).DateTime
            : null;

    [JsonInclude]
    [JsonProperty("fine_tuned_model")]
    public string FineTunedModel { get; private set; }

    [JsonInclude]
    [JsonProperty("organization_id")]
    public string OrganizationId { get; private set; }

    [JsonInclude]
    [JsonProperty("result_files")]
    public IReadOnlyList<string> ResultFiles { get; private set; }

    [JsonInclude] [JsonProperty("status")] public JobStatus Status { get; private set; }

    [JsonInclude]
    [JsonProperty("validation_file")]
    public string ValidationFile { get; private set; }

    [JsonInclude]
    [JsonProperty("training_file")]
    public string TrainingFile { get; private set; }

    [JsonInclude]
    [JsonProperty("hyperparameters")]
    public HyperParams HyperParameters { get; private set; }

    [JsonInclude]
    [JsonProperty("trained_tokens")]
    public int? TrainedTokens { get; private set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyList<Event> Events { get; internal set; } = new List<Event>();

    public static implicit operator FineTuneJobResponse(FineTuneJob job)
    {
        return new FineTuneJobResponse(job);
    }

    public static implicit operator string(FineTuneJob job)
    {
        return job?.ToString();
    }

    public override string ToString()
    {
        return Id;
    }
}