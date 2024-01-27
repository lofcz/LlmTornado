// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using OpenAiNg.Chat;
using OpenAiNg.Common;
using OpenAiNg.Models;

namespace OpenAiNg.Assistants
{
    /// <summary>
    /// A request to create an assistant
    /// </summary>
    public sealed class CreateAssistantRequest
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="assistant">Fetched/created assistant, all fields from this parameter are copied over to the new request</param>
        /// <param name="model">
        /// ID of the model to use.
        /// You can use the List models API to see all of your available models,
        /// or see our Model overview for descriptions of them.
        /// </param>
        /// <param name="name">
        /// The name of the assistant.
        /// The maximum length is 256 characters.
        /// </param>
        /// <param name="description">
        /// The description of the assistant.
        /// The maximum length is 512 characters.
        /// </param>
        /// <param name="instructions">
        /// The system instructions that the assistant uses.
        /// The maximum length is 32768 characters.
        /// </param>
        /// <param name="tools">
        /// A list of tool enabled on the assistant.
        /// There can be a maximum of 128 tools per assistant.
        /// Tools can be of types 'code_interpreter', 'retrieval', or 'function'.
        /// </param>
        /// <param name="files">
        /// A list of file IDs attached to this assistant.
        /// There can be a maximum of 20 files attached to the assistant.
        /// Files are ordered by their creation date in ascending order.
        /// </param>
        /// <param name="metadata">
        /// Set of 16 key-value pairs that can be attached to an object.
        /// This can be useful for storing additional information about the object in a structured format.
        /// Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
        /// </param>
        public CreateAssistantRequest(AssistantResponse assistant, Model? model = null, string? name = null, string? description = null, string? instructions = null, IEnumerable<Tool>? tools = null, IEnumerable<string>? files = null, IReadOnlyDictionary<string, string>? metadata = null)
            : this(model ?? assistant.Model, name ?? assistant.Name, description ?? assistant.Description, instructions ?? assistant.Instructions, tools ?? assistant.Tools, files ?? assistant.FileIds, metadata ?? assistant.Metadata)
        {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="model">
        /// ID of the model to use.
        /// You can use the List models API to see all of your available models,
        /// or see our Model overview for descriptions of them.
        /// </param>
        /// <param name="name">
        /// The name of the assistant.
        /// The maximum length is 256 characters.
        /// </param>
        /// <param name="description">
        /// The description of the assistant.
        /// The maximum length is 512 characters.
        /// </param>
        /// <param name="instructions">
        /// The system instructions that the assistant uses.
        /// The maximum length is 32768 characters.
        /// </param>
        /// <param name="tools">
        /// A list of tool enabled on the assistant.
        /// There can be a maximum of 128 tools per assistant.
        /// Tools can be of types 'code_interpreter', 'retrieval', or 'function'.
        /// </param>
        /// <param name="files">
        /// A list of file IDs attached to this assistant.
        /// There can be a maximum of 20 files attached to the assistant.
        /// Files are ordered by their creation date in ascending order.
        /// </param>
        /// <param name="metadata">
        /// Set of 16 key-value pairs that can be attached to an object.
        /// This can be useful for storing additional information about the object in a structured format.
        /// Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
        /// </param>
        public CreateAssistantRequest(Model? model = null, string? name = null, string? description = null, string? instructions = null, IEnumerable<Tool>? tools = null, IEnumerable<string>? files = null, IReadOnlyDictionary<string, string>? metadata = null)
        {
            Model = string.IsNullOrWhiteSpace(model?.ModelID) ? Models.Model.GPT35_Turbo : model;
            Name = name;
            Description = description;
            Instructions = instructions;
            Tools = tools?.ToList();
            FileIds = files?.ToList();
            Metadata = metadata;
        }

        /// <summary>
        /// ID of the model to use.
        /// You can use the List models API to see all of your available models,
        /// or see our Model overview for descriptions of them.
        /// </summary>
        [JsonProperty("model")]
        public string Model { get; }

        /// <summary>
        /// The name of the assistant.
        /// The maximum length is 256 characters.
        /// </summary>
        [JsonProperty("name")]
        public string? Name { get; }

        /// <summary>
        /// The description of the assistant.
        /// The maximum length is 512 characters.
        /// </summary>
        [JsonProperty("description")]
        public string? Description { get; }

        /// <summary>
        /// The system instructions that the assistant uses.
        /// The maximum length is 32768 characters.
        /// </summary>
        [JsonProperty("instructions")]
        public string? Instructions { get; }

        /// <summary>
        /// A list of tool enabled on the assistant.
        /// There can be a maximum of 128 tools per assistant.
        /// Tools can be of types 'code_interpreter', 'retrieval', or 'function'.
        /// </summary>
        [JsonProperty("tools")]
        public IReadOnlyList<Tool>? Tools { get; }

        /// <summary>
        /// A list of file IDs attached to this assistant.
        /// There can be a maximum of 20 files attached to the assistant.
        /// Files are ordered by their creation date in ascending order.
        /// </summary>
        [JsonProperty("file_ids")]
        public IReadOnlyList<string>? FileIds { get; }

        /// <summary>
        /// Set of 16 key-value pairs that can be attached to an object.
        /// This can be useful for storing additional information about the object in a structured format.
        /// Keys can be a maximum of 64 characters long and values can be a maximum of 512 characters long.
        /// </summary>
        [JsonProperty("metadata")]
        public IReadOnlyDictionary<string, string>? Metadata { get; }
    }
}