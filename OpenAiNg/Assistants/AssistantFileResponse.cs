// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OpenAiNg.Assistants
{
    /// <summary>
    /// File attached to an assistant.
    /// </summary>
    public sealed class AssistantFileResponse
    {
        /// <summary>
        /// The identifier, which can be referenced in API endpoints.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; private set; }

        /// <summary>
        /// The object type, which is always assistant.file.
        /// </summary>
        [JsonProperty("object")]
        public string Object { get; private set; }

        /// <summary>
        /// The Unix timestamp (in seconds) for when the assistant file was created.
        /// </summary>
        [JsonProperty("created_at")]
        public int CreatedAtUnixTimeSeconds { get; private set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnixTimeSeconds).DateTime;

        /// <summary>
        /// The assistant ID that the file is attached to.
        /// </summary>
        [JsonProperty("assistant_id")]
        public string AssistantId { get; private set; }

        public static implicit operator string?(AssistantFileResponse? file) => file?.ToString();

        public override string ToString() => Id;
    }
}