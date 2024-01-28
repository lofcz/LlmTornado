using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using OpenAiNg.Common;

namespace OpenAiNg.FineTuning;

public sealed class FineTuneJobResponse
{
    private List<EventResponse> events = [];

    public FineTuneJobResponse()
    {
    }

#pragma warning disable CS0618 // Type or member is obsolete
    internal FineTuneJobResponse(FineTuneJob job)
    {
        Object = job.Object;
        Id = job.Id;
        Model = job.Model;
        CreateAtUnixTimeSeconds = job.CreateAtUnixTimeSeconds;
        FinishedAtUnixTimeSeconds = job.FinishedAtUnixTimeSeconds;
        FineTunedModel = job.FineTunedModel;
        OrganizationId = job.OrganizationId;
        ResultFiles = job.ResultFiles;
        Status = job.Status;
        ValidationFile = job.ValidationFile;
        TrainingFile = job.TrainingFile;
        HyperParameters = job.HyperParameters;
        TrainedTokens = job.TrainedTokens;
        events = new List<EventResponse>(job.Events.Count);

        foreach (Event jobEvent in job.Events) events.Add(jobEvent);
    }
#pragma warning restore CS0618 // Type or member is obsolete

    [JsonInclude] [JsonProperty("object")] public string Object { get; private set; }

    [JsonInclude] [JsonProperty("id")] public string Id { get; private set; }

    [JsonInclude] [JsonProperty("model")] public string Model { get; private set; }

    [JsonInclude]
    [JsonProperty("created_at")]
    public int? CreateAtUnixTimeSeconds { get; private set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime? CreatedAt
        => CreateAtUnixTimeSeconds.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(CreateAtUnixTimeSeconds.Value).DateTime
            : null;

    [JsonInclude]
    [JsonProperty("finished_at")]
    public int? FinishedAtUnixTimeSeconds { get; private set; }

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
    public IReadOnlyList<EventResponse> Events
    {
        get => events;
        internal set
        {
            events = value?.ToList() ?? [];

            foreach (EventResponse @event in events)
            {
            }
        }
    }

    public static implicit operator string(FineTuneJobResponse job)
    {
        return job?.ToString();
    }

    public override string ToString()
    {
        return Id;
    }
}