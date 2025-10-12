using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Chat.Vendors.Zai;

/// <summary>
/// Chat features supported only by ZAI.
/// </summary>
public class ChatRequestVendorZaiExtensions
{
    /// <summary>
    /// Whether to enable sampling strategy.
    /// </summary>
    public bool? DoSample { get; set; }
    
    /// <summary>
    /// Unique request ID for tracking.
    /// </summary>
    public string? RequestId { get; set; }
    
    /// <summary>
    /// Whether to enable streaming response for Function Calls (GLM-4.6 only).
    /// </summary>
    public bool? ToolStream { get; set; }
    
    /// <summary>
    /// Built-in tools supported by ZAI.
    /// </summary>
    public List<IVendorZaiChatRequestBuiltInTool>? BuiltInTools { get; set; }
}

