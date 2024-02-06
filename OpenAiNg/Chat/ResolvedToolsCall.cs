using System.Collections.Generic;
using Newtonsoft.Json;
using OpenAiNg.Chat;
using OpenAiNg.ChatFunctions;
using OpenAiNg.Common;

namespace OpenAiNg.Chat;

/// <summary>
///     An optional class to be used with models that support returning function calls.
/// </summary>
public class ResolvedToolCall
{
    /// <summary>
    ///     The function called - its name and arguments.
    /// </summary>
    public FunctionCall Call { get; set; }
    
    /// <summary>
    ///     The result the function <see cref="Call"/> returned.
    /// </summary>
    public FunctionResult Result { get; set; }
    
    /// <summary>
    ///     The message with role <see cref="ChatMessageRole.Tool"/>
    /// </summary>
    public ChatMessage ToolMessage { get; set; }
}

public class ResolvedToolsCall
{
    /// <summary>
    ///     Results of the individual tools requested by to model.
    /// </summary>
    public List<ResolvedToolCall> ToolResults { get; set; } = [];
    
    /// <summary>
    ///     References all tools requested by the model.
    /// </summary>
    public ChatMessage AssistantMessage { get; set; }
}