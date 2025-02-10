using System.Collections.Generic;
using LlmTornado.Code;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
/// Represents an incremental update to a threaded message during streaming events.
/// Provides data related to the changes or updates occurring within a message.
/// </summary>
public class MessageDelta
{
    /// <summary>
    /// Gets or sets the unique identifier associated with the current object.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Represents the object type associated with the specific entity.
    /// This is typically used to identify the kind of resource or entity being represented in a given context.
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; } = null!;

    /// <summary>
    /// Represents the incremental updates or changes to a message during a streaming operation.
    /// The Delta property encapsulates information such as the role of the message, as well as
    /// the associated content provided in a read-only list.
    /// </summary>
    [JsonProperty("delta")]
    public MessageDeltaData Delta { get; set; } = null!;
}

/// <summary>
/// Represents the changes or updates in a message within a chat thread.
/// Provides details about the role associated with the message and its content.
/// </summary>
public class MessageDeltaData
{
    /// <summary>
    /// Represents the role of a chat participant in the conversation.
    /// </summary>
    /// <remarks>
    /// The <see cref="Role"/> property identifies the type of participant in the chat,
    /// facilitating differentiated behaviors and responsibilities for each role.
    /// This role is defined by the <see cref="ChatMessageRoles"/> enumeration, which includes options such as:
    /// - Unknown: Role is not defined.
    /// - System: Backend system or orchestrator.
    /// - User: End-user initiating messages.
    /// - Assistant: Virtual assistant providing responses.
    /// - Tool: Specialized tools or plugins invoked during the interaction.
    /// </remarks>
    [JsonProperty("role")]
    public ChatMessageRoles Role { get; set; }

    /// <summary>
    /// Represents the core content of the message delta.
    /// Contains a read-only list of message content objects, where each object represents
    /// a specific type of message content. The list can include various derived implementations
    /// of the abstract `MessageContent` class, providing flexibility for different types of data
    /// like text, media, or custom-defined content.
    /// </summary>
    [JsonProperty("content")]
    [JsonConverter(typeof(MessageContentJsonConverter))]
    public IReadOnlyList<MessageContent> Content { get; set; } = null!;
}

/// <summary>
/// Represents a step delta in a run stream. This class is used to capture updates related to
/// a specific step within a run process, providing details about the step's changes or progression.
/// </summary>
public class RunStepDelta
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = null!;

    /// <summary>
    /// Represents a specific object identifier and related information. This property typically
    /// provides information about the type or kind of the object it pertains to, serving as a
    /// metadata indicator in the context of a data model or API response.
    /// </summary>
    [JsonProperty("object")]
    public string Object { get; set; } = null!;

    /// <summary>
    /// Gets or sets the delta information for the current step of the run process.
    /// </summary>
    /// <remarks>
    /// The Delta property represents incremental changes or updates to the step details
    /// during a thread execution process. It is utilized to capture dynamic updates and
    /// reflect real-time changes within a running step.
    /// </remarks>
    [JsonProperty("delta")]
    public RunStepDeltaData Delta { get; set; } = null!;
}

/// <summary>
/// Represents the data associated with a delta update for a run step.
/// </summary>
public class RunStepDeltaData
{
    /// <summary>
    /// Represents the abstract base class for the details of a specific step in a process or workflow.
    /// Contains common properties shared by all step detail types.
    /// </summary>
    [JsonProperty("step_details")]
    [JsonConverter(typeof(StepDetailsConverter))]
    public StepDetails StepDetails { get; set; } = null!;
}