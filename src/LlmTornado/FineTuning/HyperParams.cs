using Newtonsoft.Json;

namespace LlmTornado.FineTuning;

public sealed class HyperParams
{
    [JsonProperty("n_epochs")]
    public object Epochs { get; private set; }

    [JsonProperty("batch_size")]
    public object BatchSize { get; private set; }

    [JsonProperty("learning_rate_multiplier")]
    public object LearningRateMultiplier { get; private set; }
}