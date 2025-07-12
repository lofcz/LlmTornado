using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Chat
{
    /// <summary>
    /// Json converter for <see cref="IChatMessagePartCitation"/> which handles the discriminator field <c>type</c>.
    /// </summary>
    internal sealed class ChatMessagePartCitationJsonConverter : JsonConverter<IChatMessagePartCitation>
    {
        private static readonly IReadOnlyDictionary<string, Type> TypeMap = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            { "char_location", typeof(ChatMessagePartCitationCharLocation) },
            { "page_location", typeof(ChatMessagePartCitationPageLocation) },
            { "content_block_location", typeof(ChatMessagePartCitationContentBlockLocation) },
            { "web_search_result_location", typeof(ChatMessagePartCitationWebSearchResultLocation) },
            { "search_result_location", typeof(ChatMessagePartCitationSearchResultLocation) },
            // Additional types will be added here as they are implemented.
        };

        public override bool CanWrite => false; // default writer is fine for objects.

        public override void WriteJson(JsonWriter writer, IChatMessagePartCitation? value, JsonSerializer serializer)
        {
            // We should never get here because CanWrite is false, but keep fallback for safety.
            serializer.Serialize(writer, value);
        }

        public override IChatMessagePartCitation? ReadJson(JsonReader reader, Type objectType, IChatMessagePartCitation? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            JObject obj = JObject.Load(reader);
            string? type = obj["type"]?.ToString();
            if (type is null)
            {
                throw new JsonSerializationException("Citation object does not contain a 'type' discriminator.");
            }

            if (!TypeMap.TryGetValue(type, out Type? target))
            {
                // Unknown citation type – fall back to an opaque implementation that just stores the raw JSON.
                // For now, throw to make the issue obvious. This can be relaxed later if needed.
                throw new JsonSerializationException($"Unsupported citation type '{type}'.");
            }

            return (IChatMessagePartCitation)obj.ToObject(target, serializer)!;
        }
    }
} 