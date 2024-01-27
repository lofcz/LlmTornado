// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OpenAiNg.Threads
{
    public sealed class RequiredAction
    {
        [JsonInclude]
        [JsonProperty("type")]
        public string Type { get; private set; }

        /// <summary>
        /// Details on the tool outputs needed for this run to continue.
        /// </summary>
        [JsonInclude]
        [JsonProperty("submit_tool_outputs")]
        public SubmitToolOutputs SubmitToolOutputs { get; private set; }
    }
}