using System;
using System.Collections.Generic;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.FineTuning;

[Obsolete("use FineTuneJobResponse")]
public sealed class FineTuneJob
{
    [JsonProperty("object")] 
    public string Object { get; private set; }

    [JsonProperty("id")]
    public string Id { get; private set; }

    [JsonProperty("model")] 
    public string Model { get; private set; }
    
    [JsonProperty("created_at")]
    public int? CreateAtUnixTimeSeconds { get; private set; }

    [JsonIgnore]
    [Obsolete("Use CreateAtUnixTimeSeconds")]
    public int? CreatedAtUnixTime => CreateAtUnixTimeSeconds;

    [JsonIgnore]
    public DateTime? CreatedAt
        => CreateAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(CreateAtUnixTimeSeconds.Value).DateTime
            : null;
    
    [JsonProperty("finished_at")]
    public int? FinishedAtUnixTimeSeconds { get; private set; }

    [JsonIgnore]
    [Obsolete("Use FinishedAtUnixTimeSeconds")]
    public int? FinishedAtUnixTime => CreateAtUnixTimeSeconds;

    [JsonIgnore]
    public DateTime? FinishedAt
        => FinishedAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(FinishedAtUnixTimeSeconds.Value).DateTime
            : null;
    
    [JsonProperty("fine_tuned_model")]
    public string FineTunedModel { get; private set; }
    
    [JsonProperty("organization_id")]
    public string OrganizationId { get; private set; }
    
    [JsonProperty("result_files")]
    public IReadOnlyList<string> ResultFiles { get; private set; }

    [JsonProperty("status")] public JobStatus Status { get; private set; }
    
    [JsonProperty("validation_file")]
    public string ValidationFile { get; private set; }
    
    [JsonProperty("training_file")]
    public string TrainingFile { get; private set; }
    
    [JsonProperty("hyperparameters")]
    public HyperParams HyperParameters { get; private set; }

    [JsonProperty("trained_tokens")]
    public int? TrainedTokens { get; private set; }

    [JsonIgnore]
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