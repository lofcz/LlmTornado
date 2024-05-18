using LlmTornado.Models;
using Argon;

namespace LlmTornado.FineTuning;

public sealed class CreateFineTuneJobRequest
{
    public CreateFineTuneJobRequest(Model? model, string trainingFileId, HyperParameters? hyperParameters = null, string? suffix = null, string? validationFileId = null)
    {
        Model = model ?? Models.Model.GPT35_Turbo;
        TrainingFileId = trainingFileId;
        HyperParameters = hyperParameters;
        Suffix = suffix;
        ValidationFileId = validationFileId;
    }

    [JsonProperty("model")] public string Model { get; set; }

    [JsonProperty("training_file")] public string TrainingFileId { get; set; }

    [JsonProperty("hyperparameters")] public HyperParameters HyperParameters { get; set; }

    [JsonProperty("suffix")] public string Suffix { get; set; }

    [JsonProperty("validation_file")] public string ValidationFileId { get; set; }
}