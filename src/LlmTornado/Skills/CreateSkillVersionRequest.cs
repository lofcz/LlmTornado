using LlmTornado.Files;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace LlmTornado.Skills;

/// <summary>
/// Request to create a new skill.
/// </summary>
public class CreateSkillVersionRequest
{

    /// <summary>
    /// A description of what the skill does.
    /// </summary>
    [JsonProperty("files")]
    public TornadoFile[] Files { get; set; }

    /// <summary>
    /// Creates a new create skill request.
    /// </summary>
    public CreateSkillVersionRequest()
    {
    }

    /// <summary>
    /// Creates a new create skill request with a name and description.
    /// </summary>
    /// <param name="name">The name of the skill</param>
    /// <param name="description">A description of what the skill does</param>
    public CreateSkillVersionRequest(TornadoFile[] files = null){
        Files = files;
    }
}
