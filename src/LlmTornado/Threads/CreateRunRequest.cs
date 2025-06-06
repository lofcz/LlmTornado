using System.Collections.Generic;
using LlmTornado.Assistants;
using LlmTornado.Chat.Models;
using Newtonsoft.Json;

namespace LlmTornado.Threads;

/// <summary>
/// Represents a request to create a new run in the system. This class encapsulates all
/// necessary and optional parameters for configuring a run, including assistant, model,
/// instructions, tools, and other execution-related parameters.
/// </summary>
public sealed class CreateRunRequest
{
    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="assistantId"></param>
    public CreateRunRequest(string assistantId)
    {
        AssistantId = assistantId;
    }
    
    /// <summary>
    ///     The ID of the assistant used for execution of this run.
    /// </summary>
    [JsonProperty("assistant_id")]
    public string AssistantId { get; set; }

    /// <summary>
    ///     Which model was used to generate this result.
    /// </summary>
    [JsonProperty("model")]
    [JsonConverter(typeof(ChatModelJsonConverter))]
    public ChatModel? Model { get; set; }

    /// <summary>
    ///     Overrides the instructions of the assistant. This is useful for modifying the behavior on a per-run basis.
    /// </summary>
    [JsonProperty("instructions")]
    public string? Instructions { get; set; }
    
    /// <summary>
    ///     Appends additional instructions at the end of the instructions for the run.
    ///     This is useful for modifying the behavior on a per-run basis without overriding other instructions.
    /// </summary>
    [JsonProperty("additional_instructions")]
    public string? AdditionalInstruction { get; set; }
    
    /// <summary>
    ///     Adds additional messages to the thread before creating the run.
    /// </summary>
    [JsonProperty("additional_messages")]
    public IReadOnlyList<CreateMessageRequest>? Messages { get; set; }

    /// <summary>
    ///     The list of tools that the assistant used for this run.
    /// </summary>
    [JsonProperty("tools")]
    [JsonConverter(typeof(AssistantToolConverter))]
    public IReadOnlyList<AssistantTool>? Tools { get; set; }

    /// <summary>
    ///     Set of 16 key-value pairs that can be attached to an object.
    ///     This can be useful for storing additional information about the object in a structured format.
    ///     Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
    /// </summary>
    [JsonProperty("metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; set; }
    
    /// <summary>
    ///     What sampling temperature to use, between 0 and 2.
    ///     Higher values like 0.8 will make the output more random,
    ///     while lower values like 0.2 will make it more focused and deterministic.
    /// </summary>
    [JsonProperty("temperature")]
    public double? Temperature { get; set; }

    /// <summary>
    ///     An alternative to sampling with temperature, called nucleus sampling,
    ///     where the model considers the results of the tokens with top_p probability mass.
    ///     So 0.1 means only the tokens comprising the top 10% probability mass are considered.
    ///     We generally recommend altering this or temperature but not both.
    /// </summary>
    [JsonProperty("top_p")]
    public double? TopP { get; set; }
    
    /// <summary>
    ///     If true, returns a stream of events that happen during the Run as server-sent events,
    ///     terminating when the Run enters a terminal state with a data: [DONE] message.
    /// </summary>
    [JsonProperty("stream")]
    internal bool Stream { get; set; }

    /// <summary>
    ///     The maximum number of prompt tokens specified to have been used over the course of the run.
    /// </summary>
    [JsonProperty("max_tokens")]
    public int? MaxPromptTokens { get; set; }
    
    /// <summary>
    ///     The maximum number of completion tokens specified to have been used over the course of the run.
    /// </summary>
    [JsonProperty("max_completion_tokens")]
    public int? MaxCompletionTokens { get; set; }
    
    /// <summary>
    ///     Controls for how a thread will be truncated prior to the run. Use this to control the initial context window of the run.
    /// </summary>
    [JsonProperty("truncation_strategy")]
    public TruncationStrategy? TruncationStrategy { get; set; }
    
    /// <summary>
    ///     Controls which (if any) tool is called by the model.
    ///     none means the model will not call any tools and instead generates a message.
    ///     auto is the default value and means the model can pick between generating a message or calling one or more tools.
    ///     required means the model must call one or more tools before responding to the user.
    ///     Specifying a particular tool like {"type": "file_search"} or {"type": "function", "function": {"name": "my_function"}}
    ///     forces the model to call that tool.
    /// </summary>
    [JsonProperty("tool_choice")]
    [JsonConverter(typeof(ToolChoiceConverter))]
    public ToolChoice ToolChoice { get; set; } = null!;

    /// <summary>
    ///     Whether to enable parallel function calling during tool use.
    /// </summary>
    [JsonProperty("parallel_tool_calls")] 
    public bool? ParallelToolCalls { get; set; }

    /// <summary>
    ///     Specifies the format that the model must output. 
    /// </summary>
    [JsonProperty("response_format")]
    [JsonConverter(typeof(ResponseFormatConverter))]
    public ResponseFormat ResponseFormat { get; set; } = null!;
}