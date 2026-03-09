using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Responses;

/// <summary>
/// Represents the conversation that a response belongs to.
/// Can be a plain string (conversation ID) or an object with an <c>id</c> field.
/// </summary>
[JsonConverter(typeof(ResponseConversationJsonConverter))]
public interface IResponseConversation
{
    /// <summary>
    /// The unique ID of the conversation.
    /// </summary>
    string Id { get; }
}

/// <summary>
/// String-form conversation reference — serializes as a plain string.
/// </summary>
public class StringResponseConversation : IResponseConversation
{
    /// <inheritdoc/>
    public string Id { get; set; }

    /// <summary>
    /// Creates a new string conversation reference.
    /// </summary>
    public StringResponseConversation(string id)
    {
        Id = id;
    }

    /// <summary>
    /// Creates an empty string conversation reference.
    /// </summary>
    public StringResponseConversation()
    {
        Id = string.Empty;
    }

    /// <summary>
    /// Implicit conversion from string.
    /// </summary>
    public static implicit operator StringResponseConversation(string id) => new(id);

    /// <summary>
    /// Implicit conversion to string.
    /// </summary>
    public static implicit operator string(StringResponseConversation conversation) => conversation.Id;
}

/// <summary>
/// Object-form conversation reference — serializes as <c>{ "id": "..." }</c>.
/// This is the form returned in API responses.
/// </summary>
public class ObjectResponseConversation : IResponseConversation
{
    /// <inheritdoc/>
    [JsonProperty("id")]
    public string Id { get; set; }

    /// <summary>
    /// Creates a new object conversation reference.
    /// </summary>
    public ObjectResponseConversation(string id)
    {
        Id = id;
    }

    /// <summary>
    /// Creates an empty object conversation reference.
    /// </summary>
    public ObjectResponseConversation()
    {
        Id = string.Empty;
    }
}

/// <summary>
/// Handles serialization and deserialization of <see cref="IResponseConversation"/>.
/// Writes <see cref="StringResponseConversation"/> as a plain JSON string,
/// <see cref="ObjectResponseConversation"/> as <c>{ "id": "..." }</c>.
/// Reads a JSON string token as <see cref="StringResponseConversation"/>,
/// a JSON object token as <see cref="ObjectResponseConversation"/>.
/// </summary>
internal class ResponseConversationJsonConverter : JsonConverter<IResponseConversation>
{
    public override void WriteJson(JsonWriter writer, IResponseConversation? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        switch (value)
        {
            case StringResponseConversation str:
                writer.WriteValue(str.Id);
                break;
            case ObjectResponseConversation obj:
                writer.WriteStartObject();
                writer.WritePropertyName("id");
                writer.WriteValue(obj.Id);
                writer.WriteEndObject();
                break;
            default:
                throw new JsonSerializationException($"Unexpected conversation type: {value.GetType()}");
        }
    }

    public override IResponseConversation? ReadJson(JsonReader reader, Type objectType, IResponseConversation? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType is JsonToken.Null)
        {
            return null;
        }

        if (reader.TokenType is JsonToken.String)
        {
            string? id = reader.Value?.ToString();
            return id is not null ? new StringResponseConversation(id) : null;
        }

        if (reader.TokenType is JsonToken.StartObject)
        {
            JObject jo = JObject.Load(reader);
            string? id = jo["id"]?.ToString();
            return id is not null ? new ObjectResponseConversation(id) : null;
        }

        throw new JsonSerializationException($"Unexpected token type for conversation: {reader.TokenType}");
    }

    public override bool CanRead => true;
    public override bool CanWrite => true;
}
