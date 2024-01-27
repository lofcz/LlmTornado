// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using OpenAiNg.Common;

namespace OpenAiNg.FineTuning
{
    [Obsolete("Use ListResponse<EventResponse>")]
    public sealed class EventList
    {
        [JsonInclude]
        [JsonProperty("object")]
        public string Object { get; private set; }

        [JsonInclude]
        [JsonProperty("data")]
        public IReadOnlyList<Event> Events { get; private set; }

        [JsonInclude]
        [JsonProperty("has_more")]
        public bool HasMore { get; private set; }
    }
}