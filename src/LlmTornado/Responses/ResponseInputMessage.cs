using System;
using System.Collections.Generic;
using System.Linq;
using LlmTornado.Code;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

/// <summary>
/// Base class for input items
/// </summary>
[JsonConverter(typeof(InputItemJsonConverter))]
public abstract class ResponseInputItem
{
    /// <summary>
    /// The type of the input item
    /// </summary>
    [JsonProperty("type")]
    public abstract string Type { get; }
}

/// <summary>
/// Input message with role and content
/// </summary>
public class ResponseInputMessage : ResponseInputItem
{
    /// <summary>
    /// The type of the message input. Always "message".
    /// </summary>
    public override string Type => "message";

    /// <summary>
    /// The role of the message input.
    /// </summary>
    [JsonProperty("role")]
    public ChatMessageRoles Role { get; set; } = ChatMessageRoles.User;

    /// <summary>
    /// The status of item. Populated when items are returned via API.
    /// </summary>
    [JsonProperty("status")]
    public ResponseMessageStatuses? Status { get; set; }

    /// <summary>
    /// A list of one or many input items to the model, containing different content types.
    /// </summary>
    [JsonProperty("content")]
    public List<InputContent> Content { get; set; } = [];

    /// <summary>
    /// Creates a new empty message.
    /// </summary>
    public ResponseInputMessage()
    {
        
    }

    public ResponseInputMessage(ChatMessageRoles role, List<InputContent> content)
    {
        Role = role;
        Content = content;
    }

    public ResponseInputMessage(ChatMessageRoles role, string text)
    {
        Role = role;
        Content = [
            new InputTextContent(text)
        ];
    }
}

/// <summary>
/// Item reference parameter
/// </summary>
public class ItemReferenceParam : ResponseInputItem
{
    /// <summary>
    /// The type of the item reference. Always "item_reference".
    /// </summary>
    public override string Type => "item_reference";

    /// <summary>
    /// The ID of the item to reference
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    public ItemReferenceParam()
    {
        
    }

    public ItemReferenceParam(string id)
    {
        Id = id;
    }
}

/// <summary>
/// JSON converter for InputItem types
/// </summary>
internal class InputItemJsonConverter : JsonConverter<ResponseInputItem>
{
    public override void WriteJson(JsonWriter writer, ResponseInputItem? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteStartObject();
        writer.WritePropertyName("type");
        writer.WriteValue(value.Type);

        switch (value)
        {
            case ResponseInputMessage msg:
            {
                writer.WritePropertyName("role");
                serializer.Serialize(writer, msg.Role);
                writer.WritePropertyName("content");
                serializer.Serialize(writer, msg.Content);
                if (msg.Status != null)
                {
                    writer.WritePropertyName("status");
                    serializer.Serialize(writer, msg.Status);
                }
                break;
            }
            case ItemReferenceParam refParam:
            {
                writer.WritePropertyName("id");
                writer.WriteValue(refParam.Id);
                break;
            }
            default:
            {
                throw new JsonSerializationException($"Unknown InputItem type: {value.GetType()}");
            }
        }

        writer.WriteEndObject();
    }

    public override ResponseInputItem? ReadJson(JsonReader reader, Type objectType, ResponseInputItem? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType is JsonToken.Null)
        {
            return null;
        }

        JObject jo = JObject.Load(reader);
        string? type = jo["type"]?.ToString();

        return type switch
        {
            "message" => DeserializeMessage(jo, serializer),
            "item_reference" => jo.ToObject<ItemReferenceParam>(serializer),
            _ => throw new JsonSerializationException($"Unknown input item type: {type}")
        };
    }

    private static ResponseInputItem? DeserializeMessage(JObject jo, JsonSerializer serializer)
    {
        return jo.ToObject<ResponseInputMessage>(serializer);
    }
} 