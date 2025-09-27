using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Responses;

/// <summary>
/// The status of an item. Populated when items are returned via API.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseMessageStatuses
{
    /// <summary>
    /// Item is in progress.
    /// </summary>
    [EnumMember(Value = "in_progress")]
    InProgress,
    
    /// <summary>
    /// Item is searching (for file search tool calls).
    /// </summary>
    [EnumMember(Value = "searching")]
    Searching,
    
    /// <summary>
    /// Item is completed.
    /// </summary>
    [EnumMember(Value = "completed")]
    Completed,
    
    /// <summary>
    /// Item is incomplete.
    /// </summary>
    [EnumMember(Value = "incomplete")]
    Incomplete,
    
    /// <summary>
    /// Item has failed.
    /// </summary>
    [EnumMember(Value = "failed")]
    Failed,
    
    /// <summary>
    /// Item has been queued (background mode).
    /// </summary>
    [EnumMember(Value = "queued")]
    Queued,
    
    /// <summary>
    /// Item has been cancelled (background mode).
    /// </summary>
    [EnumMember(Value = "cancelled")]
    Cancelled
}

/// <summary>
/// Constrains effort on reasoning for reasoning models.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseReasoningEfforts
{
    /// <summary>
    /// Low reasoning effort.
    /// </summary>
    [EnumMember(Value = "low")]
    Low,
    
    /// <summary>
    /// Medium reasoning effort.
    /// </summary>
    [EnumMember(Value = "medium")]
    Medium,
    
    /// <summary>
    /// High reasoning effort.
    /// </summary>
    [EnumMember(Value = "high")]
    High
}

/// <summary>
/// A summary of the reasoning performed by the model.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseReasoningSummaries
{
    /// <summary>
    /// Automatically decides the summary level.
    /// </summary>
    [EnumMember(Value = "auto")]
    Auto,
    
    /// <summary>
    /// Concise summary of reasoning.
    /// </summary>
    [EnumMember(Value = "concise")]
    Concise,
    
    /// <summary>
    /// Detailed summary of reasoning.
    /// </summary>
    [EnumMember(Value = "detailed")]
    Detailed
}

/// <summary>
/// The truncation strategy to use for the model response.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseTruncationStrategies
{
    /// <summary>
    /// If the context of this response and previous ones exceeds the model's context window size, 
    /// the model will truncate the response to fit the context window by dropping input items in the middle of the conversation.
    /// </summary>
    [EnumMember(Value = "auto")]
    Auto,
    
    /// <summary>
    /// If a model response will exceed the context window size for a model, the request will fail with a 400 error.
    /// </summary>
    [EnumMember(Value = "disabled")]
    Disabled
}

/// <summary>
/// Specifies additional output data to include in the model response.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ResponseIncludeFields
{
    /// <summary>
    /// Includes the outputs of python code execution in code interpreter tool call items.
    /// </summary>
    [EnumMember(Value = "code_interpreter_call.outputs")]
    CodeInterpreterCallOutputs,

    /// <summary>
    /// Includes the outputs of python code execution in code interpreter tool call items.
    /// </summary>
    [EnumMember(Value = "local_shell_call_output.output")]
    LocalShellCallOutput,

    /// <summary>
    /// Include image urls from the computer call output.
    /// </summary>
    [EnumMember(Value = "computer_call_output.output.image_url")]
    ComputerCallOutputImageUrl,

    /// <summary>
    /// Include the search results of the file search tool call.
    /// </summary>
    [EnumMember(Value = "file_search_call.results")]
    FileSearchCallResults,

    /// <summary>
    /// Include image urls from the input message.
    /// </summary>
    [EnumMember(Value = "message.input_image.image_url")]
    MessageInputImageImageUrl,

    /// <summary>
    /// Include logprobs with assistant messages.
    /// </summary>
    [EnumMember(Value = "message.output_text.logprobs")]
    MessageOutputTextLogprobs,

    /// <summary>
    /// Includes an encrypted version of reasoning tokens in reasoning item outputs.
    /// </summary>
    [EnumMember(Value = "reasoning.encrypted_content")]
    ReasoningEncryptedContent
}

/// <summary>
/// Mouse button types for computer actions.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ComputerMouseButton
{
    /// <summary>
    /// Left mouse button.
    /// </summary>
    [EnumMember(Value = "left")]
    Left,
    
    /// <summary>
    /// Right mouse button.
    /// </summary>
    [EnumMember(Value = "right")]
    Right,
    
    /// <summary>
    /// Mouse wheel button.
    /// </summary>
    [EnumMember(Value = "wheel")]
    Wheel,
    
    /// <summary>
    /// Back mouse button.
    /// </summary>
    [EnumMember(Value = "back")]
    Back,
    
    /// <summary>
    /// Forward mouse button.
    /// </summary>
    [EnumMember(Value = "forward")]
    Forward
} 