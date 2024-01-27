// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using OpenAiNg.Common;

namespace OpenAiNg.Threads
{
    public sealed class MessageFileResponse : BaseResponse
    {
        /// <summary>
        /// The identifier, which can be referenced in API endpoints.
        /// </summary>
        [JsonInclude]
        [JsonProperty("id")]
        public string Id { get; private set; }

        /// <summary>
        /// The object type, which is always thread.message.file.
        /// </summary>
        [JsonInclude]
        [JsonProperty("object")]
        public string Object { get; private set; }

        /// <summary>
        /// The Unix timestamp (in seconds) for when the message file was created.
        /// </summary>
        [JsonInclude]
        [JsonProperty("created_at")]
        public int CreatedAtUnixTimeSeconds { get; private set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnixTimeSeconds).DateTime;

        /// <summary>
        /// The ID of the message that the File is attached to.
        /// </summary>
        [JsonInclude]
        [JsonProperty("message_id")]
        public string MessageId { get; private set; }

        public static implicit operator string(MessageFileResponse response) => response?.ToString();

        public override string ToString() => Id;
    }
}