using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Threads;

/// <summary>
/// Represents a file attachment associated with a message.
/// Contains metadata and a set of tools applicable to the attachment.
/// </summary>
public sealed class MessageAttachment
{
    /// <summary>
    ///     The ID of the file to attach to the message.
    /// </summary>
    [JsonProperty("file_id")]
    public string FileId { get; set; } = null!;

    /// <summary>
    ///     The tools to add this file to.
    /// </summary>
    [JsonProperty("tools")]
    public IReadOnlyList<MessageTool> Tools { get; set; } = null!;
}

/// <summary>
///     Message tool
/// </summary>
[JsonConverter(typeof(MessageToolConverter))]
public abstract class MessageTool
{
    /// <summary>
    ///     Type of message tool
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = null!;
}

/// <summary>
///     Code Interpreter tool for message
/// </summary>
public sealed class MessageToolCodeInterpreter : MessageTool
{
    /// <summary>
    ///     default constructor
    /// </summary>
    public MessageToolCodeInterpreter()
    {
        Type = "code_interpreter";
    }
}

/// <summary>
///     File Search tool for message
/// </summary>
public sealed class MessageToolFileSearch : MessageTool
{
    /// <summary>
    ///     default constructor
    /// </summary>
    public MessageToolFileSearch()
    {
        Type = "file_search";
    }
}

internal sealed class MessageToolConverter : JsonConverter<MessageTool>
{
    public override void WriteJson(JsonWriter writer, MessageTool? value, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.FromObject(value!, serializer);
        jsonObject.WriteTo(writer);
    }

    public override MessageTool? ReadJson(JsonReader reader, Type objectType, MessageTool? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;

        JObject jsonObject = JObject.Load(reader);
        string? type = jsonObject["Type"]?.ToString();

        MessageTool? tool = type switch
        {
            "code_interpreter" => new MessageToolCodeInterpreter(),
            "file_search" => new MessageToolFileSearch(),
            _ => null
        };

        if (tool is not null)
        {
            serializer.Populate(jsonObject.CreateReader(), tool);    
        }
        
        return tool;
    }
}