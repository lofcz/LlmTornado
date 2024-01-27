// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace OpenAiNg.Threads
{
    public sealed class SubmitToolOutputs
    {
        /// <summary>
        /// A list of the relevant tool calls.
        /// </summary>
        [JsonInclude]
        [JsonProperty("tool_calls")]
        public IReadOnlyList<ToolCall> ToolCalls { get; private set; }
    }
}