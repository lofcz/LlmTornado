using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat.Vendors.Zai;
using LlmTornado.Code;
using LlmTornado.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace LlmTornado.Chat.Vendors.Zai;

/// <summary>
/// https://docs.z.ai/api-reference/introduction
/// </summary>
internal class VendorZaiChatRequestData : ChatRequest
{
    /// <summary>
    /// Whether to enable sampling strategy.
    /// </summary>
    [JsonProperty("do_sample")]
    public bool? DoSample { get; set; }
    
    /// <summary>
    /// Unique request ID for tracking.
    /// </summary>
    [JsonProperty("request_id")]
    public string? RequestId { get; set; }
    
    /// <summary>
    /// Whether to enable the chain of thought reasoning.
    /// Maps from ChatRequest.ReasoningEffort and ReasoningBudget.
    /// </summary>
    [JsonProperty("thinking")]
    public ChatRequestVendorZaiThinking? Thinking { get; set; }
    
    /// <summary>
    /// Unique ID for the end user, 6–128 characters.
    /// Maps to ChatRequest.User property.
    /// </summary>
    [JsonProperty("user_id")]
    public string? UserId { get; set; }
    
    /// <summary>
    /// Whether to enable streaming response for Function Calls (GLM-4.6 only).
    /// </summary>
    [JsonProperty("tool_stream")]
    public bool? ToolStream { get; set; }
    
    /// <summary>
    /// A list of tools the model may call.
    /// </summary>
    [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
    public new List<VendorZaiTool>? Tools { get; set; }
    
    public VendorZaiChatRequestData(ChatRequest request) : base(request)
    {
        // Map existing ChatRequest properties to ZAI-specific JSON properties
        UserId = request.User;
        
        // Map reasoning from ReasoningEffort and ReasoningBudget
        if (ShouldEnableReasoning(request))
        {
            Thinking = new ChatRequestVendorZaiThinking
            {
                Type = ChatRequestVendorZaiThinkingType.Enabled
            };
        }
        
        // Convert tools to ZAI-specific format
        if (request.Tools is { Count: > 0 })
        {
            Tools = ConvertTools(request.Tools);
        }
        
        // Add built-in tools from vendor extensions
        if (request.VendorExtensions?.Zai?.BuiltInTools is { Count: > 0 })
        {
            Tools ??= new List<VendorZaiTool>();
            Tools.AddRange(request.VendorExtensions.Zai.BuiltInTools.Select(ConvertBuiltInTool));
        }
    }
    
    private static List<VendorZaiTool> ConvertTools(List<Tool> tools)
    {
        List<VendorZaiTool> zaiTools = new();
        
        foreach (Tool tool in tools)
        {
            if (tool.Function is not null)
            {
                zaiTools.Add(new VendorZaiFunctionTool
                {
                    Function = new VendorZaiFunctionObject
                    {
                        Name = tool.Function.Name,
                        Description = tool.Function.Description,
                        Parameters = tool.Function.Parameters ?? new object()
                    }
                });
            }
            else if (tool.Type == "web_search")
            {
                // Handle built-in web search tool
                zaiTools.Add(new VendorZaiWebSearchToolWrapper
                {
                    Name = tool.ToolName ?? "web_search",
                    WebSearch = new VendorZaiWebSearchObject
                    {
                        Enable = true
                    }
                });
            }
            else if (tool.Type == "retrieval")
            {
                // Handle retrieval tool if needed
                zaiTools.Add(new VendorZaiRetrievalTool
                {
                    Retrieval = new VendorZaiRetrievalObject
                    {
                        KnowledgeId = tool.ToolName ?? string.Empty
                    }
                });
            }
        }
        
        return zaiTools;
    }
    
    private static VendorZaiTool ConvertBuiltInTool(IVendorZaiChatRequestBuiltInTool builtInTool)
    {
        return builtInTool.Type switch
        {
            VendorZaiToolType.WebSearch => new VendorZaiWebSearchToolWrapper
            {
                Name = builtInTool.Name,
                WebSearch = builtInTool is VendorZaiWebSearchTool webSearch ? webSearch.WebSearch : new VendorZaiWebSearchObject { Enable = true }
            },
            _ => throw new ArgumentException($"Unsupported built-in tool type: {builtInTool.Type}")
        };
    }
    
    private static bool ShouldEnableReasoning(ChatRequest request)
    {
        // Enable reasoning if ReasoningEffort is not null and not "none"
        if (request.ReasoningEffort is not null && request.ReasoningEffort != Code.ChatReasoningEfforts.None)
        {
            return true;
        }
        
        // Enable reasoning if ReasoningBudget > 0 or == -1
        if (request.ReasoningBudget > 0 || request.ReasoningBudget == -1)
        {
            return true;
        }
        
        return false;
    }
}

/// <summary>
/// Thinking configuration for ZAI models.
/// </summary>
public class ChatRequestVendorZaiThinking
{
    /// <summary>
    /// Whether to enable the chain of thought.
    /// </summary>
    public ChatRequestVendorZaiThinkingType Type { get; set; } = ChatRequestVendorZaiThinkingType.Enabled;
}

/// <summary>
/// Thinking types for ZAI models.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ChatRequestVendorZaiThinkingType
{
    /// <summary>
    /// Enable chain of thought reasoning.
    /// </summary>
    [EnumMember(Value = "enabled")]
    Enabled,
    
    /// <summary>
    /// Disable chain of thought reasoning.
    /// </summary>
    [EnumMember(Value = "disabled")]
    Disabled
}
