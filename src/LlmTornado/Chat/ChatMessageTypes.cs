using System;
using Newtonsoft.Json;

namespace LlmTornado.Chat;

/// <summary>
///     Represents available message types.
/// </summary>
public enum ChatMessageTypes
{
    /// <summary>
    /// Message part is a text fragment.
    /// </summary>
    Text,
    
    /// <summary>
    /// Message part is either base64 encoded image or a publicly available URL pointing to an image.
    /// </summary>
    Image,
    
    /// <summary>
    /// Message part is an audio fragment.
    /// </summary>
    Audio,

    /// <summary>
    /// Message part is URI-based file.
    /// </summary>
    FileLink,
    
    /// <summary>
    /// Message part is a reasoning block.<br/>
    /// <b>Supported only by Anthropic.</b>
    /// </summary>
    Reasoning,
    
    /// <summary>
    /// Message part is either base64 encoded PDF or a publicly available URL pointing to a PDF (unencrypted, no passwords).<br/>
    /// <b>Supported only by Anthropic.</b>
    /// </summary>
    Document,
    
    /// <summary>
    /// Message part is a search result. This enables natural citations for RAG applications.<br/>
    /// <b>Supported only by Anthropic.</b>
    /// </summary>
    SearchResult,
    
    /// <summary>
    /// Message part is a video fragment.
    /// </summary>
    Video,
    
    /// <summary>
    /// Message part is an executable code fragment.
    /// </summary>
    ExecutableCode,
    
    /// <summary>
    /// Message part is result of a code execution.
    /// </summary>
    CodeExecutionResult,
    
    /// <summary>
    /// A content block that represents a file to be uploaded to the container. Files uploaded via this block will be available in the container's input directory.<br/>
    /// <b>Supported only by Anthropic.</b>
    /// </summary>
    ContainerUpload,

    /// <summary>
    /// A reference block (e.g., list of reference ids).
    /// </summary>
    Reference,
    
    /// <summary>
    /// A compaction block containing a server-generated summary of earlier conversation context.<br/>
    /// Used by the compaction API to manage long conversations. Content before this block is discarded on subsequent requests.<br/>
    /// <b>Supported only by Anthropic (Claude Opus 4.6+).</b>
    /// </summary>
    Compaction,

    /// <summary>
    /// Server-side tool call part returned by Google Gemini built-in tools during tool context circulation.<br/>
    /// <b>Supported only by Google Gemini 3+ tool combination.</b>
    /// </summary>
    ServerSideToolCall,

    /// <summary>
    /// Server-side tool response part returned by Google Gemini built-in tools during tool context circulation.<br/>
    /// <b>Supported only by Google Gemini 3+ tool combination.</b>
    /// </summary>
    ServerSideToolResponse,

    /// <summary>
    /// Native Google API part preserved for round-trip when combined with server-side tool parts.<br/>
    /// <b>Supported only by Google Gemini 3+ tool combination.</b>
    /// </summary>
    GoogleVendorPart,
}