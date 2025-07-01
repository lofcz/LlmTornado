using System.Collections.Generic;
using LlmTornado.ChatFunctions;
using LlmTornado.Common;
using Newtonsoft.Json;
using LlmTornado.Chat;

namespace LlmTornado.Chat;

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
    ///     The message with role <see cref="Tool"/>
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