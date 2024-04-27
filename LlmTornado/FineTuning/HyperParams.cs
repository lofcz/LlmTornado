using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace LlmTornado.FineTuning;

public sealed class HyperParams
{
    [JsonInclude]
    [JsonProperty("n_epochs")]
    public object Epochs { get; private set; }

    [JsonInclude]
    [JsonProperty("batch_size")]
    public object BatchSize { get; private set; }

    [JsonInclude]
    [JsonProperty("learning_rate_multiplier")]
    public object LearningRateMultiplier { get; private set; }
}