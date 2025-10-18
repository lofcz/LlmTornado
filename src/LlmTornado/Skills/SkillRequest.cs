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
public class CreateSkillRequest
{
    /// <summary>
    /// The name of the skill.
    /// </summary>
    [JsonProperty("display_title")]
    public string DisplayTitle { get; set; }

    /// <summary>
    /// A description of what the skill does.
    /// </summary>
    [JsonProperty("files")]
    public FileUploadRequest[] Files { get; set; }

    public MultipartFormDataContent Content { get; set; } = new MultipartFormDataContent();

    /// <summary>
    /// Creates a new create skill request.
    /// </summary>
    public CreateSkillRequest()
    {
    }

    /// <summary>
    /// Creates a new create skill request with a name and description.
    /// </summary>
    /// <param name="name">The name of the skill</param>
    /// <param name="description">A description of what the skill does</param>
    public CreateSkillRequest(string displayTitle, FileUploadRequest[]? files = null)
    {
        DisplayTitle = displayTitle;
        Files = files;

        Content.Add(new StringContent(displayTitle), "display_title");

        foreach (FileUploadRequest x in files)
        {
            ByteArrayContent bc = new ByteArrayContent(x.Bytes);
            bc.Headers.ContentType = new MediaTypeHeaderValue(x.MimeType ?? "application/pdf");

            Content.Add(bc, "files[]", x.Name);
        }
    }
}



public class CreateSkillResponse
{
    [JsonProperty("display_title")]
    public string DisplayTitle { get; set; }
    /// <summary>
    /// Unique identifier for the skill.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }


    /// <summary>
    /// "custom": the skill was created by a user
    /// "anthropic": the skill was created by Anthropic
    /// </summary>
    [JsonProperty("source")]
    public string Source { get; set; } = "custom";

    /// <summary>
    /// The type of object. Always "skill".
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = "skill";

    /// <summary>
    /// The ID of the currently active version for this skill.
    /// </summary>
    [JsonProperty("latest_version")]
    public string LatestVersion { get; set; }

    /// <summary>
    /// The timestamp when the skill was created.
    /// </summary>
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// The timestamp when the skill was last updated.
    /// </summary>
    [JsonProperty("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

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
