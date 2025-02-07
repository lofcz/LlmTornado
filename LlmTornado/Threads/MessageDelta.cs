using System.Collections.Generic;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

public class MessageDelta
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("object")]
    public string Object { get; set; } = null!;

    [JsonProperty("delta")]
    public MessageDeltaData Delta { get; set; } = null!;
}

public class MessageDeltaData
{
    [JsonProperty("role")]
    public ChatMessageRoles Role { get; set; }

    [JsonProperty("content")]
    [JsonConverter(typeof(MessageContentJsonConverter))]
    public IReadOnlyList<MessageContent> Content { get; set; } = null!;
}

public class RunStepDelta
{
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    [JsonProperty("object")]
    public string Object { get; set; } = null!;

    [JsonProperty("delta")]
    public RunStepDeltaData Delta { get; set; } = null!;
}

public class RunStepDeltaData
{
    [JsonProperty("step_details")]
    [JsonConverter(typeof(StepDetailsConverter))]
    public StepDetails StepDetails { get; set; } = null!;
}