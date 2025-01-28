using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Assistants;

/// <summary>
///     Represents a tool object
/// </summary>
public abstract class AssistantTool
{
    /// <summary>
    ///     Type of the tool, should be always "function" for chat, assistants also accepts values "code_interpreter" and
    ///     "file_search"
    /// </summary>
    [JsonProperty("type", Required = Required.Default)]
    public string Type { get; set; } = null!;
}

/// <summary>
///     Defines the custom converter for polymorphic assistant tool deserialization
/// </summary>
public class AssistantToolConverter : JsonConverter<IReadOnlyList<AssistantTool>?>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="objectType"></param>
    /// <param name="existingValue"></param>
    /// <param name="hasExistingValue"></param>
    /// <param name="serializer"></param>
    /// <returns></returns>
    public override IReadOnlyList<AssistantTool>? ReadJson(JsonReader reader, Type objectType, IReadOnlyList<AssistantTool>? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var tools = new List<AssistantTool>();
        JArray jsonArray = JArray.Load(reader);

        foreach (JObject jsonObject in jsonArray)
        {
            string? toolType = jsonObject["type"]?.ToString();
            AssistantTool? tool = toolType switch
            {
                "function" => jsonObject.ToObject<AssistantToolFunction>(serializer),
                "code_interpreter" => jsonObject.ToObject<AssistantToolCodeInterpreter>(serializer),
                "file_search" => jsonObject.ToObject<AssistantToolFileSearch>(serializer),
                _ => null
            };

            if (tool != null)
            {
                tools.Add(tool);
            }
        }

        return tools.AsReadOnly();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="serializer"></param>
    public override void WriteJson(JsonWriter writer, IReadOnlyList<AssistantTool>? value, JsonSerializer serializer)
    {
        serializer.Serialize(writer, value);
    }

    public override bool CanWrite => true;
}