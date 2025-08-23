using LlmTornado.Chat;

namespace LlmTornado.Agents.DataModels;

/// <summary>
/// Represents a callback function that handles model streaming events asynchronously.
/// </summary>
/// <param name="streamingResult">The streaming event data containing information about the current state of the streaming operation.</param>
/// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
public delegate ValueTask StreamingCallbacks(ModelStreamingEvents streamingResult);

/// <summary>
/// Specifies the current status of a model streaming operation.
/// </summary>
public enum ModelStreamingStatus
{
    /// <summary>
    /// The streaming operation is currently in progress.
    /// </summary>
    InProgress,
    
    /// <summary>
    /// The streaming operation has completed successfully.
    /// </summary>
    Completed,
    
    /// <summary>
    /// The streaming operation has failed due to an error.
    /// </summary>
    Failed,
    
    /// <summary>
    /// The streaming operation was canceled by the user or system.
    /// </summary>
    Canceled,
    
    /// <summary>
    /// The streaming operation is queued and waiting to start.
    /// </summary>
    Queued,
    
    /// <summary>
    /// The streaming operation completed but with incomplete results.
    /// </summary>
    Incomplete
}

/// <summary>
/// Specifies the type of event that occurs during model streaming operations.
/// </summary>
public enum ModelStreamingEventType
{
    /// <summary>
    /// A new streaming session was created.
    /// </summary>
    Created,
    
    /// <summary>
    /// The streaming operation is currently in progress.
    /// </summary>
    InProgress,
    
    /// <summary>
    /// The streaming operation failed.
    /// </summary>
    Failed,
    
    /// <summary>
    /// The streaming operation completed with incomplete results.
    /// </summary>
    Incomplete,
    
    /// <summary>
    /// The streaming operation completed successfully.
    /// </summary>
    Completed,
    
    /// <summary>
    /// A new output item was added to the stream.
    /// </summary>
    OutputItemAdded,
    
    /// <summary>
    /// An output item in the stream has finished processing.
    /// </summary>
    OutputItemDone,
    
    /// <summary>
    /// A new content part was added to the stream.
    /// </summary>
    ContentPartAdded,
    
    /// <summary>
    /// A content part in the stream has finished processing.
    /// </summary>
    ContentPartDone,
    
    /// <summary>
    /// A delta (incremental text update) was added to the output text.
    /// </summary>
    OutputTextDelta,
    
    /// <summary>
    /// An annotation was added to the output text.
    /// </summary>
    OutputTextAnnotationAdded,
    
    /// <summary>
    /// The text output has finished.
    /// </summary>
    TextDone,
    
    /// <summary>
    /// A delta update for content refusal was received.
    /// </summary>
    RefusalDelta,
    
    /// <summary>
    /// The content refusal processing is complete.
    /// </summary>
    RefusalDone,
    
    /// <summary>
    /// A delta update for a function call was received.
    /// </summary>
    FunctionCallDelta,
    
    /// <summary>
    /// The function call processing is complete.
    /// </summary>
    FunctionCallDone,
    
    /// <summary>
    /// File search operation is in progress.
    /// </summary>
    FileSearchInProgress,
    
    /// <summary>
    /// File search operation is actively searching.
    /// </summary>
    FileSearchSearching,
    
    /// <summary>
    /// File search operation has completed.
    /// </summary>
    FileSearchDone,
    
    /// <summary>
    /// A delta update for code interpreter code was received.
    /// </summary>
    CodeInterpreterCodeDelta,
    
    /// <summary>
    /// The code interpreter code processing is complete.
    /// </summary>
    CodeInterpreterCodeDone,
    
    /// <summary>
    /// The code interpreter is currently executing code.
    /// </summary>
    CodeInterpreterIntepreting,
    
    /// <summary>
    /// The code interpreter has completed execution.
    /// </summary>
    CodeInterpreterCompleted,
    
    /// <summary>
    /// A reasoning part was added to the stream.
    /// </summary>
    ReasoningPartAdded,
    
    /// <summary>
    /// A reasoning part has finished processing.
    /// </summary>
    ReasoningPartDone,
    
    /// <summary>
    /// An error occurred during the streaming operation.
    /// </summary>
    Error
}

/// <summary>
/// Base class for model streaming events.
/// </summary>
public class ModelStreamingEvents : EventArgs
{
    /// <summary>
    /// Response ID associated with the streaming event.
    /// </summary>
    public string? ResponseId { get; set; } = string.Empty;
    /// <summary>
    /// Sequence ID associated with the streaming event.
    /// </summary>
    public int SequenceId { get; set; } = 1;
    /// <summary>
    /// Event type associated with the streaming event.
    /// </summary>
    public ModelStreamingEventType EventType { get; set; }

    /// <summary>
    /// Status associated with the streaming event.
    /// </summary>
    public ModelStreamingStatus Status { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingEvents"/> class with the specified sequence number,
    /// response ID, event type, and status.
    /// </summary>
    /// <param name="seqNum">The sequence number associated with the streaming event. Must be a non-negative integer.</param>
    /// <param name="responseId">The unique identifier for the response associated with the event. Defaults to an empty string if not specified.</param>
    /// <param name="type">The type of the streaming event. Defaults to <see cref="ModelStreamingEventType.Created"/> if not specified.</param>
    /// <param name="status">The status of the streaming event. Defaults to <see cref="ModelStreamingStatus.InProgress"/> if not specified.</param>
    public ModelStreamingEvents(int seqNum, string responseId = "", ModelStreamingEventType type = ModelStreamingEventType.Created, ModelStreamingStatus status = ModelStreamingStatus.InProgress)
    {
        SequenceId = seqNum;
        EventType = type;
        Status = status;
        ResponseId = responseId;
    }
}

/// <summary>
/// Provides data for streaming callback events when a streaming session is created.
/// </summary>
public class ModelStreamingCreatedEvent : ModelStreamingEvents
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingCreatedEvent"/> class with the specified sequence
    /// number, response ID, event type, and status.
    /// </summary>
    /// <param name="seqNum">The sequence number associated with the event. Must be a non-negative integer.</param>
    /// <param name="responseId">The unique identifier for the response. Defaults to an empty string if not provided.</param>
    /// <param name="type">The type of the streaming event. Defaults to <see cref="ModelStreamingEventType.Created"/>.</param>
    /// <param name="status">The status of the streaming event. Defaults to <see cref="ModelStreamingStatus.InProgress"/>.</param>
    public ModelStreamingCreatedEvent(int seqNum, string responseId = "", ModelStreamingEventType type = ModelStreamingEventType.Created, ModelStreamingStatus status = ModelStreamingStatus.InProgress):base(seqNum, responseId, type, status)
    {

    }
}

/// <summary>
/// Provides data for streaming callback events when a streaming operation is in progress.
/// </summary>
public class ModelStreamingInProgressEvent : ModelStreamingEvents
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingInProgressEvent"/> class with the specified sequence
    /// number, response ID, event type, and status.
    /// </summary>
    /// <param name="seqNum">The sequence number associated with the event. Must be a non-negative integer.</param>
    /// <param name="responseId">The unique identifier for the response. Defaults to an empty string if not provided.</param>
    /// <param name="type">The type of the streaming event. Defaults to <see cref="ModelStreamingEventType.Created"/>.</param>
    /// <param name="status">The status of the streaming event. Defaults to <see cref="ModelStreamingStatus.InProgress"/>.</param>
    public ModelStreamingInProgressEvent(int seqNum, string responseId = "", ModelStreamingEventType type = ModelStreamingEventType.Created, ModelStreamingStatus status = ModelStreamingStatus.InProgress) : base(seqNum, responseId, type, status)
    {

    }
}

/// <summary>
/// Provides data for streaming callback events when a streaming operation has completed.
/// </summary>
public class ModelStreamingCompletedEvent : ModelStreamingEvents
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingCompletedEvent"/> class with the specified sequence
    /// number, response ID, event type, and status.
    /// </summary>
    /// <param name="seqNum">The sequence number associated with the event. Must be a non-negative integer.</param>
    /// <param name="responseId">The unique identifier for the response. Defaults to an empty string if not provided.</param>
    /// <param name="type">The type of the streaming event. Defaults to <see cref="ModelStreamingEventType.Created"/>.</param>
    /// <param name="status">The status of the streaming event. Defaults to <see cref="ModelStreamingStatus.InProgress"/>.</param>
    public ModelStreamingCompletedEvent(int seqNum, string responseId = "", ModelStreamingEventType type = ModelStreamingEventType.Created, ModelStreamingStatus status = ModelStreamingStatus.InProgress) : base(seqNum, responseId, type, status)
    {

    }
}

/// <summary>
/// Provides data for streaming callback events when a streaming operation has failed.
/// </summary>
public class ModelStreamingFailedEvent : ModelStreamingEvents
{
    /// <summary>
    /// Gets or sets the error message describing the failure.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Gets or sets the error code associated with the failure.
    /// </summary>
    public string? ErrorCode { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingFailedEvent"/> class with the specified sequence
    /// number, response ID, error message, and error code.
    /// </summary>
    /// <param name="seqNum">The sequence number associated with the event. Must be a non-negative integer.</param>
    /// <param name="responseId">The unique identifier for the response. Defaults to an empty string if not provided.</param>
    /// <param name="errorMessage">A message describing the error that occurred. Defaults to an empty string if not provided.</param>
    /// <param name="errorCode">A code representing the specific error that occurred. Defaults to an empty string if not provided.</param>
    public ModelStreamingFailedEvent(int seqNum, string responseId = "", string errorMessage = "", string errorCode = "") : base(seqNum, responseId, ModelStreamingEventType.Failed, ModelStreamingStatus.Failed)
    {
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Provides data for streaming callback events when a streaming operation completes with incomplete results.
/// </summary>
public class ModelStreamingIncompleteEvent : ModelStreamingEvents
{
    /// <summary>
    /// Gets or sets the reason why the streaming operation was incomplete.
    /// </summary>
    public string? Reason { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingIncompleteEvent"/> class with the specified sequence
    /// number, response ID, and reason for incompleteness.
    /// </summary>
    /// <param name="seqNum">The sequence number associated with the event. Must be a non-negative integer.</param>
    /// <param name="responseId">The unique identifier for the response. Defaults to an empty string if not provided.</param>
    /// <param name="reason">The reason why the operation was incomplete. Defaults to an empty string if not provided.</param>
    public ModelStreamingIncompleteEvent(int seqNum, string responseId = "", string reason=""): base(seqNum, responseId, ModelStreamingEventType.Incomplete, ModelStreamingStatus.Incomplete)
    {
        Reason = reason;
    }
}

/// <summary>
/// Provides data for streaming callback events when an error occurs during model streaming.
/// </summary>
public class ModelStreamingErrorEvent : ModelStreamingEvents
{
    /// <summary>
    /// Gets or sets the error message describing what went wrong.
    /// </summary>
    public string? ErrorMessage { get; set; }
    
    /// <summary>
    /// Gets or sets the error code associated with the error.
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingErrorEvent"/> class with the specified sequence
    /// number, response ID, error message, and error code.
    /// </summary>
    /// <param name="seqNum">The sequence number of the event, used to track the order of events.</param>
    /// <param name="responseID">The unique identifier for the response associated with this event. This parameter is optional and defaults to an
    /// empty string.</param>
    /// <param name="errorMessage">A message describing the error that occurred. This parameter is optional and defaults to an empty string.</param>
    /// <param name="errorCode">A code representing the specific error that occurred. This parameter is optional and defaults to an empty
    /// string.</param>
    public ModelStreamingErrorEvent(int seqNum, string responseID = "", string errorMessage = "",string errorCode = ""): base(seqNum, responseID, ModelStreamingEventType.Error, ModelStreamingStatus.Failed)
    {
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }
}

/// <summary>
/// Provides data for streaming callback events when a text delta (incremental update) is received.
/// </summary>
public class ModelStreamingOutputTextDeltaEvent : ModelStreamingEvents
{
    /// <summary>
    /// Gets or sets the index of the output stream to which this delta belongs.
    /// </summary>
    public int OutputIndex { get; set; }
    
    /// <summary>
    /// Gets or sets the index of the content part within the output stream.
    /// </summary>
    public int ContentPartIndex { get; set; }
    
    /// <summary>
    /// Gets or sets an optional identifier for the item associated with this event.
    /// </summary>
    public string? ItemId { get; set; }
    
    /// <summary>
    /// Gets or sets the incremental text content that was added in this delta update.
    /// </summary>
    public string? DeltaText { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingOutputTextDeltaEvent"/> class with the specified
    /// parameters for a text delta update.
    /// </summary>
    /// <param name="seqNum">The sequence number of the event, used to maintain the order of events.</param>
    /// <param name="outputIndex">The index of the output stream to which this delta belongs.</param>
    /// <param name="contentIndex">The index of the content part within the output stream.</param>
    /// <param name="content">The text content of the delta update.</param>
    /// <param name="itemId">An optional identifier for the item associated with this event. Defaults to an empty string if not provided.</param>
    /// <param name="responseID">An optional identifier for the response associated with this event. Defaults to an empty string if not provided.</param>
    public ModelStreamingOutputTextDeltaEvent(int seqNum, int outputIndex, int contentIndex, string content, string itemId = "", string responseID = "") : base(seqNum, responseID, ModelStreamingEventType.OutputTextDelta, ModelStreamingStatus.InProgress)
    {
        OutputIndex = outputIndex;
        ContentPartIndex = contentIndex;
        ItemId = itemId;
        DeltaText = content;
    }
}

/// <summary>
/// Provides data for streaming callback events when text output has finished.
/// </summary>
public class ModelStreamingOutputTextDoneEvent : ModelStreamingEvents
{
    /// <summary>
    /// Gets or sets the index of the output stream that has finished.
    /// </summary>
    public int OutputIndex { get; set; }
    
    /// <summary>
    /// Gets or sets the index of the content part within the output stream that has finished.
    /// </summary>
    public int ContentPartIndex { get; set; }
    
    /// <summary>
    /// Gets or sets an optional identifier for the item associated with this event.
    /// </summary>
    public string? ItemId { get; set; }
    
    /// <summary>
    /// Gets or sets the final text content when the text output is complete.
    /// </summary>
    public string? DeltaText { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingOutputTextDoneEvent"/> class with the specified
    /// parameters for a completed text output.
    /// </summary>
    /// <param name="seqNum">The sequence number of the event, used to maintain the order of events.</param>
    /// <param name="outputIndex">The index of the output stream that has finished.</param>
    /// <param name="contentIndex">The index of the content part within the output stream that has finished.</param>
    /// <param name="content">The final text content when the output is complete.</param>
    /// <param name="itemId">An optional identifier for the item associated with this event. Defaults to an empty string if not provided.</param>
    /// <param name="responseID">An optional identifier for the response associated with this event. Defaults to an empty string if not provided.</param>
    public ModelStreamingOutputTextDoneEvent(int seqNum, int outputIndex, int contentIndex, string content, string itemId = "", string responseID = "") : base(seqNum, responseID, ModelStreamingEventType.TextDone, ModelStreamingStatus.Completed)
    {
        OutputIndex = outputIndex;
        ContentPartIndex = contentIndex;
        ItemId = itemId;
        DeltaText = content;
    }
}

/// <summary>
/// Represents a streaming model item containing information about the current state and content of a streaming response.
/// </summary>
public class StreamingModelItem
{
    /// <summary>
    /// Gets or sets the unique identifier for this streaming item.
    /// </summary>
    public string? Id { get; set; }
    
    /// <summary>
    /// Gets or sets the current status of this streaming item.
    /// </summary>
    public ModelStreamingStatus Status { get; set; } = ModelStreamingStatus.InProgress;
    
    /// <summary>
    /// Gets or sets the type of chat message this item represents.
    /// </summary>
    public ChatMessageTypes type { get; set; } = ChatMessageTypes.Text;
    
    /// <summary>
    /// Gets or sets the role of the participant that generated this content (e.g., "assistant", "user").
    /// </summary>
    public string Role { get; set; } = "assistant";
    
    /// <summary>
    /// Gets or sets the content parts that make up this streaming item.
    /// </summary>
    public ChatMessagePart[]? Content { get; set; }
}

/// <summary>
/// Provides data for streaming callback events when a new output item is added to the stream.
/// </summary>
public class ModelStreamingOutputItemAddedEvent : ModelStreamingEvents
{
    /// <summary>
    /// Gets or sets the index of the output item that was added.
    /// </summary>
    public int OutputIndex { get; set; }
    
    /// <summary>
    /// Gets or sets the streaming model item that was added to the output.
    /// </summary>
    public StreamingModelItem? OutputItem { get; set; } = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingOutputItemAddedEvent"/> class with the specified
    /// sequence number and output index.
    /// </summary>
    /// <param name="seqNum">The sequence number of the event, used to maintain the order of events.</param>
    /// <param name="index">The index of the output item that was added.</param>
    /// <param name="responseID">An optional identifier for the response associated with this event. Defaults to an empty string if not provided.</param>
    public ModelStreamingOutputItemAddedEvent(int seqNum, int index, string responseID = "") : base(seqNum,responseID, ModelStreamingEventType.OutputItemAdded, ModelStreamingStatus.InProgress)
    {
        OutputIndex = index;
    }
}

/// <summary>
/// Provides data for streaming callback events when an output item has finished processing.
/// </summary>
public class ModelStreamingOutputItemDoneEvent : ModelStreamingEvents
{
    /// <summary>
    /// Gets or sets the index of the output item that has finished processing.
    /// </summary>
    public int OutputIndex { get; set; }
    
    /// <summary>
    /// Gets or sets the streaming model item that has finished processing.
    /// </summary>
    public StreamingModelItem? OutputItem { get; set; } = null;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingOutputItemDoneEvent"/> class with the specified
    /// sequence number and output index.
    /// </summary>
    /// <param name="seqNum">The sequence number of the event, used to maintain the order of events.</param>
    /// <param name="index">The index of the output item that has finished processing.</param>
    /// <param name="responseID">An optional identifier for the response associated with this event. Defaults to an empty string if not provided.</param>
    public ModelStreamingOutputItemDoneEvent(int seqNum, int index, string responseID = "") : base(seqNum, responseID,ModelStreamingEventType.OutputItemDone, ModelStreamingStatus.Completed)
    {
        OutputIndex = index;
    }
}

/// <summary>
/// Provides data for streaming callback events when a new content part is added to the stream.
/// </summary>
public class ModelStreamingContentPartAddEvent : ModelStreamingEvents
{
    /// <summary>
    /// Gets or sets the index of the output stream to which this content part belongs.
    /// </summary>
    public int OutputIndex { get; set; }
    
    /// <summary>
    /// Gets or sets the index of the content part within the output stream.
    /// </summary>
    public int ContentPartIndex { get; set; }
    
    /// <summary>
    /// Gets or sets the type of the content part that was added.
    /// </summary>
    public ChatMessageTypes ContentPartType { get; set; }
    
    /// <summary>
    /// Gets or sets the text content of the content part, if applicable.
    /// </summary>
    public string? ContentPartText { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingContentPartAddEvent"/> class with the specified
    /// parameters for a new content part.
    /// </summary>
    /// <param name="seqNum">The sequence number of the event, used to maintain the order of events.</param>
    /// <param name="outputIndex">The index of the output stream to which this content part belongs.</param>
    /// <param name="contentPartIndex">The index of the content part within the output stream.</param>
    /// <param name="contentPartType">The type of the content part that was added.</param>
    /// <param name="contentPartText">The text content of the content part, if applicable. Defaults to null if not provided.</param>
    /// <param name="responseID">An optional identifier for the response associated with this event. Defaults to an empty string if not provided.</param>
    public ModelStreamingContentPartAddEvent(int seqNum, int outputIndex, int contentPartIndex, ChatMessageTypes contentPartType, string? contentPartText = null, string responseID = "")
        :base(seqNum, responseID, ModelStreamingEventType.ContentPartAdded, ModelStreamingStatus.InProgress)
    { 
        OutputIndex = outputIndex;
        ContentPartIndex = contentPartIndex;
        ContentPartType = contentPartType;
        ContentPartText = contentPartText;
    }
}

/// <summary>
/// Provides data for streaming callback events when a content part has finished processing.
/// </summary>
public class ModelStreamingContentPartDoneEvent : ModelStreamingEvents
{
    /// <summary>
    /// Gets or sets the index of the output stream to which this content part belongs.
    /// </summary>
    public int OutputIndex { get; set; }
    
    /// <summary>
    /// Gets or sets the index of the content part within the output stream that has finished.
    /// </summary>
    public int ContentPartIndex { get; set; }
    
    /// <summary>
    /// Gets or sets the type of the content part that has finished processing.
    /// </summary>
    public ChatMessageTypes ContentPartType { get; set; }
    
    /// <summary>
    /// Gets or sets the final text content of the content part, if applicable.
    /// </summary>
    public string? ContentPartText { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingContentPartDoneEvent"/> class with the specified
    /// parameters for a completed content part.
    /// </summary>
    /// <param name="seqNum">The sequence number of the event, used to maintain the order of events.</param>
    /// <param name="outputIndex">The index of the output stream to which this content part belongs.</param>
    /// <param name="contentPartIndex">The index of the content part within the output stream that has finished.</param>
    /// <param name="contentPartType">The type of the content part that has finished processing.</param>
    /// <param name="contentPartText">The final text content of the content part, if applicable. Defaults to null if not provided.</param>
    /// <param name="responseID">An optional identifier for the response associated with this event. Defaults to an empty string if not provided.</param>
    public ModelStreamingContentPartDoneEvent(int seqNum, int outputIndex, int contentPartIndex, ChatMessageTypes contentPartType, string? contentPartText = null, string responseID = "")
        : base(seqNum, responseID, ModelStreamingEventType.ContentPartDone, ModelStreamingStatus.Completed)
    {
        OutputIndex = outputIndex;
        ContentPartIndex = contentPartIndex;
        ContentPartType = contentPartType;
        ContentPartText = contentPartText;
    }
}

/// <summary>
/// Provides data for streaming callback events when a streaming operation is queued.
/// </summary>
public class ModelStreamingQueuedEvent : ModelStreamingEvents
{
    /// <summary>
    /// Gets or sets the timestamp when the streaming operation was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
    
    /// <summary>
    /// Gets or sets the timestamp when the streaming operation was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingQueuedEvent"/> class with the specified
    /// sequence number and timestamps.
    /// </summary>
    /// <param name="seqNum">The sequence number of the event, used to maintain the order of events.</param>
    /// <param name="createdAt">The timestamp when the streaming operation was created.</param>
    /// <param name="updatedAt">The timestamp when the streaming operation was last updated.</param>
    /// <param name="responseID">An optional identifier for the response associated with this event. Defaults to an empty string if not provided.</param>
    public ModelStreamingQueuedEvent(int seqNum, DateTime createdAt, DateTime updatedAt, string responseID = "")
        : base(seqNum, responseID, ModelStreamingEventType.Created, ModelStreamingStatus.Queued)
    {
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }
}

/// <summary>
/// Provides data for streaming callback events when a reasoning part has finished processing.
/// </summary>
public class ModelStreamingReasoningPartDoneEvent : ModelStreamingEvents
{
    /// <summary>
    /// Gets or sets the index of the output stream to which this reasoning part belongs.
    /// </summary>
    public int OutputIndex { get; set; }
    
    /// <summary>
    /// Gets or sets the index of the summary part within the reasoning output.
    /// </summary>
    public int SummaryPartIndex { get; set; }
    
    /// <summary>
    /// Gets or sets the reasoning text content that was processed.
    /// </summary>
    public string? DeltaText { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier for the reasoning item.
    /// </summary>
    public string ItemId { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingReasoningPartDoneEvent"/> class with the specified
    /// parameters for a completed reasoning part.
    /// </summary>
    /// <param name="seqNum">The sequence number of the event, used to maintain the order of events.</param>
    /// <param name="outputIndex">The index of the output stream to which this reasoning part belongs.</param>
    /// <param name="summaryPartIndex">The index of the summary part within the reasoning output.</param>
    /// <param name="itemId">The unique identifier for the reasoning item.</param>
    /// <param name="reasoningText">The reasoning text content that was processed. Defaults to an empty string if not provided.</param>
    /// <param name="responseID">An optional identifier for the response associated with this event. Defaults to an empty string if not provided.</param>
    public ModelStreamingReasoningPartDoneEvent(int seqNum, int outputIndex, int summaryPartIndex, string itemId, string reasoningText = "", string responseID = "")
        : base(seqNum, responseID, ModelStreamingEventType.ReasoningPartDone, ModelStreamingStatus.InProgress)
    {
        OutputIndex = outputIndex;
        SummaryPartIndex = summaryPartIndex;
        DeltaText = reasoningText;
        ItemId = itemId;
    }
}

/// <summary>
/// Provides data for streaming callback events when a reasoning part is added to the stream.
/// </summary>
public class ModelStreamingReasoningPartAddedEvent : ModelStreamingEvents
{
    /// <summary>
    /// Gets or sets the index of the output stream to which this reasoning part belongs.
    /// </summary>
    public int OutputIndex { get; set; }
    
    /// <summary>
    /// Gets or sets the index of the summary part within the reasoning output.
    /// </summary>
    public int SummaryPartIndex { get; set; }
    
    /// <summary>
    /// Gets or sets the reasoning text content that was added.
    /// </summary>
    public string? DeltaText { get; set; }
    
    /// <summary>
    /// Gets or sets the unique identifier for the reasoning item.
    /// </summary>
    public string ItemId { get; set; }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ModelStreamingReasoningPartAddedEvent"/> class with the specified
    /// parameters for a new reasoning part.
    /// </summary>
    /// <param name="seqNum">The sequence number of the event, used to maintain the order of events.</param>
    /// <param name="outputIndex">The index of the output stream to which this reasoning part belongs.</param>
    /// <param name="summaryPartIndex">The index of the summary part within the reasoning output.</param>
    /// <param name="itemId">The unique identifier for the reasoning item.</param>
    /// <param name="reasoningText">The reasoning text content that was added. Defaults to an empty string if not provided.</param>
    /// <param name="responseID">An optional identifier for the response associated with this event. Defaults to an empty string if not provided.</param>
    public ModelStreamingReasoningPartAddedEvent(int seqNum, int outputIndex, int summaryPartIndex, string itemId, string reasoningText = "", string responseID = "")
        : base(seqNum, responseID, ModelStreamingEventType.ReasoningPartAdded, ModelStreamingStatus.Completed)
    {
        OutputIndex = outputIndex;
        SummaryPartIndex = summaryPartIndex;
        DeltaText = reasoningText;
        ItemId = itemId;
    }
}