using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Chat.Vendors.Anthropic;
using LlmTornado.ChatFunctions;
using LlmTornado.Code;
using LlmTornado.Infra;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace LlmTornado.Common;

/// <summary>
/// Modes available for serialization of the content
/// </summary>
public enum FunctionResultSetContentModes
{
    /// <summary>
    /// Does nothing, expects the data is already serialized into a string, if not, calls <see cref="Object.ToString"/>.
    /// </summary>
    Passthrough,
    
    /// <summary>
    /// Serializes the input using JSON serializer, unless the input is already a string.
    /// </summary>
    Serialize
}

/// <summary>
///     Represents a function call result
/// </summary>
public class FunctionResult
{
    /// <summary>
    /// Creates an empty result.
    /// </summary>
    public FunctionResult()
    {
    }

    /// <summary>
    /// </summary>
    /// <param name="name">Name of the function that was called. Can differ from the originally intended function.</param>
    /// <param name="content">A serializable object (e.g. class / dict / anonymous object) that will be serialized into JSON</param>
    public FunctionResult(string name, object? content)
    {
        Name = name;
        Content = SetContent(content);
        InvocationSucceeded = true;
    }
    
    /// <summary>
    /// </summary>
    /// <param name="name">Name of the function that was called. Can differ from the originally intended function.</param>
    /// <param name="content">Either a serializable object (e.g. class / dict / anonymous object) that will be serialized into JSON, or already serialized data.</param>
    /// <param name="mode">Whether the content should be serialized or is already serialized.</param>
    public FunctionResult(string name, object? content, FunctionResultSetContentModes mode)
    {
        Name = name;
        Content = SetContent(content, mode);
        InvocationSucceeded = true;
    }
    
    /// <summary>
    /// </summary>
    /// <param name="name">Name of the function that was called. Can differ from the originally intended function.</param>
    /// <param name="content">Either a serializable object (e.g. class / dict / anonymous object) that will be serialized into JSON, or already serialized data.</param>
    /// <param name="mode">Whether the content should be serialized or is already serialized.</param>
    /// <param name="invocationSucceeded">An indicator whether the tool invocation succeeded or not.</param>
    public FunctionResult(string name, object? content, FunctionResultSetContentModes mode, bool invocationSucceeded)
    {
        Name = name;
        Content = SetContent(content, mode);
        InvocationSucceeded = invocationSucceeded;
    }

    /// <summary>
    /// </summary>
    /// <param name="name">Name of the function that was called. Can differ from the originally intended function.</param>
    /// <param name="content">A serializable object (e.g. class / dict / anonymous object) that will be serialized into JSON</param>
    /// <param name="passthroughData">Any data you might want to work with later but not include in the generated chat message</param>
    public FunctionResult(string name, object? content, object? passthroughData)
    {
        Name = name;
        Content = SetContent(content);
        PassthroughData = passthroughData;
        InvocationSucceeded = true;
    }
    
    /// <summary>
    /// </summary>
    /// <param name="name">Name of the function that was called. Can differ from the originally intended function.</param>
    /// <param name="content">A serializable object (e.g. class / dict / anonymous object) that will be serialized into JSON</param>
    /// <param name="passthroughData">Any data you might want to work with later but not include in the generated chat message</param>
    /// <param name="invocationSucceeded">An indicator whether the tool invocation succeeded or not.</param>
    public FunctionResult(string name, object? content, object? passthroughData, bool invocationSucceeded)
    {
        Name = name;
        Content = SetContent(content);
        PassthroughData = passthroughData;
        InvocationSucceeded = invocationSucceeded;
    }
    
    /// <summary>
    /// </summary>
    /// <param name="call">The function call this result maps to.</param>
    /// <param name="content">A serializable object (e.g. class / dict / anonymous object) that will be serialized into JSON</param>
    /// <param name="passthroughData">Any data you might want to work with later but not include in the generated chat message</param>
    public FunctionResult(FunctionCall call, object? content, object? passthroughData)
    {
        Name = call.Name;
        Content = SetContent(content);
        PassthroughData = passthroughData;
        InvocationSucceeded = true;
    }
    
    /// <summary>
    /// </summary>
    /// <param name="call">The function call this result maps to.</param>
    /// <param name="content">A serializable object (e.g. class / dict / anonymous object) that will be serialized into JSON</param>
    public FunctionResult(FunctionCall call, object? content)
    {
        Name = call.Name;
        Content = SetContent(content);
        InvocationSucceeded = true;
    }
    
    /// <summary>
    /// </summary>
    /// <param name="call">The function call this result maps to.</param>
    /// <param name="content">A list of rich blocks.</param>
    public FunctionResult(FunctionCall call, List<IFunctionResultBlock> content)
    {
        Name = call.Name;
        Content = SetContentBlocks(content);
        InvocationSucceeded = true;
        RawContentBlocks = content;
    }
    
    /// <summary>
    /// </summary>
    /// <param name="call">The function call this result maps to.</param>
    /// <param name="content">A list of rich blocks.</param>
    /// <param name="invocationSucceeded">An indicator whether the tool invocation succeeded or not.</param>
    public FunctionResult(FunctionCall call, List<IFunctionResultBlock> content, bool invocationSucceeded)
    {
        Name = call.Name;
        Content = SetContentBlocks(content);
        InvocationSucceeded = invocationSucceeded;
        RawContentBlocks = content;
    }
    
    /// <summary>
    /// </summary>
    /// <param name="call">The function call this result maps to.</param>
    /// <param name="content">A serializable object (e.g. class / dict / anonymous object) that will be serialized into JSON</param>
    /// <param name="invocationSucceeded">An indicator whether the tool invocation succeeded or not.</param>
    public FunctionResult(FunctionCall call, object? content, bool invocationSucceeded)
    {
        Name = call.Name;
        Content = SetContent(content);
        InvocationSucceeded = invocationSucceeded;
    }
    
    /// <summary>
    /// </summary>
    /// <param name="call">The function call this result maps to.</param>
    /// <param name="content">A serializable object (e.g. class / dict / anonymous object) that will be serialized into JSON</param>
    /// <param name="passthroughData">Any data you might want to work with later but not include in the generated chat message</param>
    /// <param name="invocationSucceeded">An indicator whether the tool invocation succeeded or not.</param>
    public FunctionResult(FunctionCall call, object? content, object? passthroughData, bool invocationSucceeded)
    {
        Name = call.Name;
        Content = SetContent(content);
        PassthroughData = passthroughData;
        InvocationSucceeded = invocationSucceeded;
    }

    /// <summary>
    ///     Name of the function used; passthrough.
    /// </summary>
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; }

    /// <summary>
    /// Function output. This string contains JSON-encoded data unless received from an MCP tool.
    /// </summary>
    [JsonProperty("content", Required = Required.Always)]
    public string Content { get; set; }
    
    /// <summary>
    /// Function output when received from remote protocols, such as MCP.
    /// </summary>
    [JsonIgnore]
    public IRemoteContent? RemoteContent { get; set; }

    /// <summary>
    ///     A passthrough arbitrary data.
    /// </summary>
    [JsonIgnore]
    public object? PassthroughData { get; set; }
    
    /// <summary>
    ///     A flag which, if implemented by the vendor, provides the model with information whether the tool invocation succeded
    ///     or not.
    /// </summary>
    [JsonIgnore]
    public bool? InvocationSucceeded { get; set; }
    
    [JsonIgnore]
    public Type? ContentJsonType { get; set; }
    
    [JsonIgnore]
    internal object? RawContent { get; set; }
    
    [JsonIgnore]
    internal IEnumerable<IFunctionResultBlock>? RawContentBlocks { get; set; }

    private string SetContent(object? content, FunctionResultSetContentModes mode = FunctionResultSetContentModes.Serialize)
    {
        ContentJsonType = content?.GetType();
        RawContent = content;
        return mode is FunctionResultSetContentModes.Passthrough ? 
            content as string ?? content?.ToString() ?? "{}" : 
            content is null ? "{}" : JsonConvert.SerializeObject(content);
    }
    
    private string SetContentBlocks(List<IFunctionResultBlock>? content)
    {
        ContentJsonType = content?.GetType();
        RawContent = content;

        if (content is null)
        {
            return "{}";
        }

        List<string> blocks = [];

        foreach (IFunctionResultBlock block in content)
        {
            if (block is FunctionResultBlockText textBlock)
            {
                blocks.Add(textBlock.Text);
            }
        }

        return blocks.Count switch
        {
            1 => blocks[0],
            > 1 => JsonConvert.SerializeObject(blocks),
            _ => JsonConvert.SerializeObject(content)
        };
    }
}

/// <summary>
/// Content from remote protocols.
/// </summary>
public interface IRemoteContent
{
    
}

/// <summary>
/// MCP content.
/// </summary>
public class McpContent : IRemoteContent
{
    /// <summary>
    /// The response content from the tool call.
    /// </summary>
    public List<IMcpContentBlock> McpContentBlocks { get; set; }
    
    /// <summary>
    /// Gets or sets an indication of whether the tool call was unsuccessful.
    /// </summary>
    /// <remarks>
    /// When set to <see langword="true" />, it signifies that the tool execution failed.
    /// Tool errors are reported with this property set to <see langword="true" /> and details in the <see cref="P:ModelContextProtocol.Protocol.CallToolResult.Content" />
    /// property, rather than as protocol-level errors. This allows LLMs to see that an error occurred
    /// and potentially self-correct in subsequent requests.
    /// </remarks>
    public bool IsError { get; set; }
    
    /// <summary>
    /// Optional JSON object representing the structured result of the tool call.
    /// </summary>
    public JsonNode? StructuredContent { get; set; }
}

/// <summary>
/// Shared interface for MCP content blocks.
/// </summary>
public interface IMcpContentBlock
{
    /// <summary>
    /// This determines the structure of the content object. Valid values include "image", "audio", "text", "resource", and "resource_link".
    /// </summary>
    public string Type { get; set; }
    
    /// <summary>Optional annotations for the content.</summary>
    /// <remarks>
    /// These annotations can be used to specify the intended audience (<see cref="F:ModelContextProtocol.Protocol.Role.User" />, <see cref="F:ModelContextProtocol.Protocol.Role.Assistant" />, or both)
    /// and the priority level of the content. Clients can use this information to filter or prioritize content for different roles.
    /// </remarks>
    public McpAnnotations? Annotations { get; init; }
}

/// <summary>
/// Unknown MCP content block.
/// </summary>
public class McpContentBlockUnknown : McpContentBlock
{
    
}

/// <summary>
/// MCP annotations.
/// </summary>
public class McpAnnotations
{
    public IList<ChatMessageRoles>? Audience { get; init; }
    
    public float? Priority { get; init; }
    
    public DateTimeOffset? LastModified { get; set; }
}

/// <summary>
/// Base MCP content block.
/// </summary>
public abstract class McpContentBlock : IMcpContentBlock
{
    public string Type { get; set; } = string.Empty;
    public McpAnnotations? Annotations { get; init; }
    public NativeMcpContentBlock NativeBlock { get; set; }
}

public class NativeMcpContentBlock
{
    public object NativeBlock { get; set; }
}

/// <summary>
/// Text content block.
/// </summary>
public class McpContentBlockText : McpContentBlock
{
    /// <summary>
    /// The text content of the message.
    /// </summary>
    public string Text { get; set; }
}

/// <summary>
/// Text content block.
/// </summary>
public class McpContentBlockImage : McpContentBlock
{
    /// <summary>
    /// The base64-encoded image data.
    /// </summary>
    public string Data { get; set; }
    
    /// <summary>
    /// The MIME type (or "media type") of the content, specifying the format of the data.
    /// </summary>
    public string MimeType { get; set; }
}

/// <summary>
/// Represents audio provided to or from an LLM.
/// </summary>
public class McpContentBlockAudio : McpContentBlock
{
    /// <summary>
    /// The base64-encoded audio data.
    /// </summary>
    public string Data { get; set; }
    
    /// <summary>
    /// Gets or sets the MIME type (or "media type") of the content, specifying the format of the data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Common values include "audio/wav" and "audio/mp3".
    /// </para>
    /// </remarks>
    public string MimeType { get; set; }
}

/// <summary>Represents the contents of a resource, embedded into a prompt or tool call result.</summary>
/// <remarks>
/// It is up to the client how best to render embedded resources for the benefit of the LLM and/or the user.
/// </remarks>
public class McpContentBlockEmbeddedResource : McpContentBlock
{
    /// <summary>
    /// Gets or sets the resource content of the message when <see cref="T:System.Type" /> is "resource".
    /// </summary>
    /// <remarks>
    /// <para>
    /// Resources can be either text-based (<see cref="T:ModelContextProtocol.Protocol.TextResourceContents" />) or
    /// binary (<see cref="T:ModelContextProtocol.Protocol.BlobResourceContents" />), allowing for flexible data representation.
    /// Each resource has a URI that can be used for identification and retrieval.
    /// </para>
    /// </remarks>
    public McpContentBlockEmbeddedResourceContents Resource { get; set; }
    
    /// <summary>
    /// Gets or sets the MIME type (or "media type") of the content, specifying the format of the data.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Common values include "audio/wav" and "audio/mp3".
    /// </para>
    /// </remarks>
    public string MimeType { get; set; }
}

/// <summary>
/// Represents a resource that the server is capable of reading, included in a prompt or tool call result.
/// </summary>
/// <remarks>
/// Resource links returned by tools are not guaranteed to appear in the results of `resources/list` requests.
/// </remarks>
public class McpContentBlockLinkResource : McpContentBlock
{
    /// <summary>
    /// The URI of this resource.
    /// </summary>
    public string Uri { get; init; }

    /// <summary>
    /// Human-readable name for this resource.
    /// </summary>
    public string Name { get; init; }
    
    /// <summary>
    /// Gets or sets a description of what this resource represents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This can be used by clients to improve the LLM's understanding of available resources. It can be thought of like a \"hint\" to the model.
    /// </para>
    /// <para>
    /// The description should provide clear context about the resource's content, format, and purpose.
    /// This helps AI models make better decisions about when to access or reference the resource.
    /// </para>
    /// <para>
    /// Client applications can also use this description for display purposes in user interfaces
    /// or to help users understand the available resources.
    /// </para>
    /// </remarks>
    public string? Description { get; init; }

    /// <summary>Gets or sets the MIME type of this resource.</summary>
    /// <remarks>
    /// <para>
    /// <see cref="P:ModelContextProtocol.Protocol.ResourceLinkBlock.MimeType" /> specifies the format of the resource content, helping clients to properly interpret and display the data.
    /// Common MIME types include "text/plain" for plain text, "application/pdf" for PDF documents,
    /// "image/png" for PNG images, and "application/json" for JSON data.
    /// </para>
    /// <para>
    /// This property may be <see langword="null" /> if the MIME type is unknown or not applicable for the resource.
    /// </para>
    /// </remarks>
    public string? MimeType { get; init; }

    /// <summary>
    /// Gets or sets the size of the raw resource content (before base64 encoding), in bytes, if known.
    /// </summary>
    /// <remarks>
    /// This can be used by applications to display file sizes and estimate context window usage.
    /// </remarks>
    public long? Size { get; init; }
}

/// <summary>Represents the contents of a resource, embedded into a prompt or tool call result.</summary>
/// <remarks>
/// It is up to the client how best to render embedded resources for the benefit of the LLM and/or the user.
/// </remarks>
public abstract class McpContentBlockEmbeddedResourceContents
{
    /// <summary>
    /// The URI of the resource.
    /// </summary>
    public string Uri { get; set; } = string.Empty;

    /// <summary>
    /// The MIME type of the resource content.
    /// </summary>
    public string? MimeType { get; set; }
}

/// <summary>
/// Represents text-based contents of a resource in the Model Context Protocol.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="T:ModelContextProtocol.Protocol.TextResourceContents" /> is used when textual data needs to be exchanged through
/// the Model Context Protocol. The text is stored directly in the <see cref="P:ModelContextProtocol.Protocol.TextResourceContents.Text" /> property.
/// </para>
/// <para>
/// See the <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see> for more details.
/// </para>
/// </remarks>
public class McpContentBlockEmbeddedResourceContentsText : McpContentBlockEmbeddedResourceContents
{
    /// <summary>
    /// The text of the item.
    /// </summary>
    public string Text { get; set; }
}

/// <summary>
/// Represents the binary contents of a resource in the Model Context Protocol.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="T:ModelContextProtocol.Protocol.BlobResourceContents" /> is used when binary data needs to be exchanged through
/// the Model Context Protocol. The binary data is represented as a base64-encoded string
/// in the <see cref="P:ModelContextProtocol.Protocol.BlobResourceContents.Blob" /> property.
/// </para>
/// <para>
/// See the <see href="https://github.com/modelcontextprotocol/specification/blob/main/schema/">schema</see> for more details.
/// </para>
/// </remarks>
public class McpContentBlockEmbeddedResourceContentsBlob : McpContentBlockEmbeddedResourceContents
{
    /// <summary>
    /// The base64-encoded string representing the binary data of the item.
    /// </summary>
    public string Blob { get; set; }
}

/// <summary>
/// Unknown embedded resource content.
/// </summary>
public class McpContentBlockEmbeddedResourceContentsUnknown : McpContentBlockEmbeddedResourceContents
{
    /// <summary>
    /// The native object.
    /// </summary>
    public object Native { get; set; }
}

/// <summary>
///     Represents a tool object
/// </summary>
public class Tool
{
    /// <summary>
    /// Attached delegate, if any.
    /// </summary>
    [JsonIgnore]
    public Delegate? Delegate { get; }
    
    [JsonIgnore]
    internal DelegateMetadata? DelegateMetadata { get; set; }
    
    [JsonIgnore]
    internal ToolMetadata? Metadata { get; set; }
    
    [JsonIgnore]
    internal string? ToolName { get; set; }
    
    [JsonIgnore]
    internal string? ToolDescription { get; set; }
    
    [JsonIgnore]
    internal List<ToolParam>? SchemaParams { get; set; }
    
    /// <summary>
    /// Creates the tool with a delegate attached. This delegate is serialized into JSON schema and potentially invoked by a LLM.
    /// </summary>
    /// <param name="function">The function, can be an anonymous function.</param>
    /// <param name="metadata">Optional metadata: additional parameters, excluded parameters, and other options.</param>
    /// <param name="strict">Whether strict JSON schema validation is enabled.</param>
    public Tool(Delegate function, ToolMetadata? metadata = null, bool? strict = null)
    {
        Delegate = function;
        Strict = strict;
        Metadata = metadata;
    }
    
    /// <summary>
    /// Creates the tool with a delegate attached. This delegate is serialized into JSON schema and potentially invoked by a LLM.
    /// </summary>
    /// <param name="function">The function, can be an anonymous function.</param>
    /// <param name="name">Name of the function.</param>
    /// <param name="metadata">Optional metadata: additional parameters, excluded parameters, and other options.</param>
    /// <param name="strict">Whether strict JSON schema validation is enabled.</param>
    public Tool(Delegate function, string name, ToolMetadata? metadata = null, bool? strict = null)
    {
        Delegate = function;
        Strict = strict;
        Metadata = metadata;
        ToolName = NormalizeName(name);
    }
    
    /// <summary>
    /// Creates the tool with a delegate attached. This delegate is serialized into JSON schema and potentially invoked by a LLM.
    /// </summary>
    /// <param name="function">The function, can be an anonymous function.</param>
    /// <param name="name">Name of the function.</param>
    /// <param name="description">Description of the function.</param>
    /// <param name="metadata">Optional metadata: additional parameters, excluded parameters, and other options.</param>
    /// <param name="strict">Whether strict JSON schema validation is enabled.</param>
    public Tool(Delegate function, string name, string description, ToolMetadata? metadata = null, bool? strict = null)
    {
        Delegate = function;
        Strict = strict;
        Metadata = metadata;
        ToolName = NormalizeName(name);
        ToolDescription = description;
    }
    
    /// <summary>
    /// Creates a new tool from given high-level JSON schema parameters. 
    /// </summary>
    /// <param name="pars">Input parameters, if any.</param>
    /// <param name="name">Name of the function.</param>
    /// <param name="description">Description forwarded to the model.</param>
    /// <param name="strict">Whether strict JSON schema validation is enabled.</param>
    public Tool(List<ToolParam> pars, string name, string description, bool? strict = null)
    {
        ToolName = NormalizeName(name);
        ToolDescription = description;
        Strict = strict;
        SchemaParams = pars;
    }
    
    /// <summary>
    /// Creates a new tool from given high-level JSON schema parameters. 
    /// </summary>
    /// <param name="pars">Input parameters, if any.</param>
    /// <param name="name">Name of the function.</param>
    /// <param name="strict">Whether strict JSON schema validation is enabled.</param>
    public Tool(List<ToolParam> pars, string name, bool? strict = null)
    {
        ToolName = NormalizeName(name);
        Strict = strict;
        SchemaParams = pars;
    }
    
    /// <summary>
    ///     Creates a new function type tool.
    /// </summary>
    /// <param name="function"></param>
    public Tool(ToolFunction function)
    {
        Function = function;
    }
    
    /// <summary>
    ///     Creates a new function type tool with strict mode enabled/disabled.
    /// </summary>
    /// <param name="function"></param>
    /// <param name="strict">Whether to use structured output or not</param>
    public Tool(ToolFunction function, bool strict)
    {
        Function = function;
        Strict = strict;
    }

    /// <summary>
    ///     Creates a new tool of a given type.
    /// </summary>
    /// <param name="type"></param>
    public Tool(string type)
    {
        Type = type;
    }

    /// <summary>
    /// Empty tool.
    /// </summary>
    public Tool()
    {
    }

    private ConcurrentDictionary<int, ToolFunction>? serializedDict = [];

    /// <summary>
    /// Serializes tool for a given provider.
    /// </summary>
    public void Serialize(IEndpointProvider provider)
    {
        Serialize(provider, 0);
    }

    internal void Serialize(IEndpointProvider provider, int functionIndex)
    {
        if (Delegate is null && SchemaParams is null)
        {
            return;
        }
        
        int hash = provider.GetHashCode();
        serializedDict ??= [];

        if (serializedDict.TryGetValue(hash, out ToolFunction? fn))
        {
            Function = fn;
        }

        if (Delegate is not null)
        {
            DelegateMetadata = ToolFactory.CreateFromMethod(Delegate, Metadata, provider);
            DelegateMetadata.ToolFunction.Name = !ToolName.IsNullOrWhiteSpace() ? ToolName : $"tool_{functionIndex + 1}";

            if (!ToolDescription.IsNullOrWhiteSpace())
            {
                DelegateMetadata.ToolFunction.Description = ToolDescription;
            }

            Function = DelegateMetadata.ToolFunction;
            serializedDict.TryAdd(hash, Function);      
        }
        else if (SchemaParams is not null)
        {
            Function = ToolFactory.Compile(new ToolDefinition(ToolName ?? $"tool_{functionIndex + 1}", ToolDescription, SchemaParams), new ToolMeta
            {
                Provider = provider
            });
            
            serializedDict.TryAdd(hash, Function);      
        }
    }

    /// <summary>
    ///     Type of the tool, should be always "function" for chat
    /// </summary>
    [JsonProperty("type", Required = Required.Default)]
    public string Type { get; set; } = "function";

    /// <summary>
    ///     Function description
    /// </summary>
    [JsonProperty("function", Required = Required.Default)]
    public ToolFunction? Function { get; set; }

    /// <summary>
    ///     Whether the function should run in structured response mode or not.
    /// </summary>
    [JsonProperty("strict")]
    public bool? Strict { get; set; }
    
    /// <summary>
    ///     Functionality supported only by certain providers.
    /// </summary>
    [JsonIgnore]
    public ToolVendorExtensions? VendorExtensions { get; set; }
    
    /// <summary>
    /// Remote tool, for example MCP.
    /// </summary>
    [JsonIgnore]
    public IRemoteTool? RemoteTool { get; set; }
    
    /// <summary>
    ///     Creates a tool from <see cref="ToolFunction" />
    /// </summary>
    /// <param name="function"></param>
    /// <returns></returns>
    public static implicit operator Tool(ToolFunction function)
    {
        return new Tool(function);
    }

    internal static string NormalizeName(string name)
    {
        return name.Replace(" ", "_").Trim();
    }
}

/// <summary>
/// Remote tools, such as those provided by MCP.
/// </summary>
public interface IRemoteTool
{
    /// <summary>
    /// Method to execute the tool.
    /// </summary>
    public Func<Dictionary<string, object?>?, IProgress<ToolCallProgress>?, JsonSerializerOptions?, bool, CancellationToken?, ValueTask<FunctionResult>> CallAsync { get; set; }
}

/// <summary>
/// Tool provided by MCP.
/// </summary>
public class McpTool : IRemoteTool
{ 
    public Func<Dictionary<string, object?>?, IProgress<ToolCallProgress>?, JsonSerializerOptions?, bool, CancellationToken?, ValueTask<FunctionResult>> CallAsync { get; set; }

    public McpTool()
    {
        
    }
}

/// <summary>
/// Progress of the tool call.
/// </summary>
public sealed class ToolCallProgress
{
    /// <summary>Gets or sets the progress thus far.</summary>
    /// <remarks>
    /// <para>
    /// This value typically represents either a percentage (0-100) or the number of items processed so far (when used with the <see cref="P:ModelContextProtocol.ProgressNotificationValue.Total" /> property).
    /// </para>
    /// <para>
    /// When reporting progress, this value should increase monotonically as the operation proceeds.
    /// Values are typically between 0 and 100 when representing percentages, or can be any positive number
    /// when representing completed items in combination with the <see cref="P:ModelContextProtocol.ProgressNotificationValue.Total" /> property.
    /// </para>
    /// </remarks>
    public required float Progress { get; init; }

    /// <summary>Gets or sets the total number of items to process (or total progress required), if known.</summary>
    public float? Total { get; init; }

    /// <summary>Gets or sets an optional message describing the current progress.</summary>
    public string? Message { get; init; }
}

/// <summary>
///     Represents a Tool function object for the OpenAI API.
///     A tool contains information about the function to be called, its description and parameters.
/// </summary>
/// <remarks>
///     The 'Name' property represents the name of the function and must consist of alphanumeric characters, underscores,
///     or dashes, with a maximum length of 64.
///     The 'Description' property is an optional field that provides a brief explanation about what the function does.
///     The 'Parameters' property describes the parameters that the function accepts, which are represented as a JSON
///     Schema object.
///     Various types of input are acceptable for the 'Parameters' property, such as a JObject, a Dictionary of string and
///     object, an anonymous object, or any other serializable object.
///     If the object is not a JObject, it will be converted into a JObject.
///     Refer to the 'Parameters' property setter for more details.
///     Refer to the OpenAI API <see href="https://platform.openai.com/docs/guides/gpt/function-calling">guide</see> and
///     the
///     JSON Schema <see href="https://json-schema.org/understanding-json-schema/">reference</see> for more details on the
///     format of the parameters.
/// </remarks>
public class ToolFunction
{
    private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

    /// <summary>
    ///     The name of the function to be called. Must be a-z, A-Z, 0-9, or contain underscores and dashes, with a maximum
    ///     length of 64.
    /// </summary>
    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; set; }

    /// <summary>
    ///     The description of what the function does.
    /// </summary>
    [JsonProperty("description")]
    public string Description { get; set; }

    /// <summary>
    ///     The input parameters of the tool, if any.
    /// </summary>
    [JsonProperty("parameters")]
    public object? Parameters { get; set; }
    
    [JsonIgnore]
    internal object? RawParameters { get; set; }
    
    /// <summary>
    ///     Create a parameterless function.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    public ToolFunction(string name, string description)
    {
        Name = Tool.NormalizeName(name);
        Description = description;
        Parameters = null;
    }
    
    /// <summary>
    ///     Create a function with parameters.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="parameters">JSON serialized object, will be deserialized into <see cref="JObject" /> </param>
    public ToolFunction(string name, string description, string parameters)
    {
        Name = Tool.NormalizeName(name);
        Description = description;
        Parameters = JObject.Parse(parameters);
    }

    /// <summary>
    ///     Create a function with parameters.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="parameters"></param>
    public ToolFunction(string name, string description, JObject parameters)
    {
        Name = Tool.NormalizeName(name);
        Description = description;
        Parameters = parameters;
        RawParameters = parameters;
    }
    
    /// <summary>
    ///     Create a function with parameters.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="parameters"></param>
    public ToolFunction(string name, string description, JsonElement parameters)
    {
        Name = Tool.NormalizeName(name);
        Description = description;
        Parameters = JObject.Parse(parameters.ToString());
        RawParameters = parameters;
    }

    /// <summary>
    ///     Create a function with parameters.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="description"></param>
    /// <param name="parameters">A JSON-serializable object</param>
    public ToolFunction(string name, string description, object parameters)
    {
        Name = Tool.NormalizeName(name);
        Description = description;
        Parameters = JObject.FromObject(parameters, JsonSerializer.Create(serializerSettings));
    }

    /// <summary>
    ///     Creates an empty Function object.
    /// </summary>
    private ToolFunction()
    {
    }

    /// <summary>
    /// Text representation of the tool.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        StringBuilder sb = new StringBuilder();

        if (!Name.IsNullOrWhiteSpace())
        {
            sb.AppendLine(Name);
        }

        if (!Description.IsNullOrWhiteSpace())
        {
            sb.AppendLine(Description);
        }

        if (Parameters is not null)
        {
            sb.AppendLine(Parameters.ToJson(true));
        }

        return sb.ToString().Trim();
    }
}