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
    /// The display title for the skill.
    /// This is a human-readable label that is not included in the prompt sent to the model.
    /// </summary>
    [JsonProperty("display_title")]
    public string DisplayTitle { get; set; }

    /// <summary>
    /// Files to upload for the skill.
    /// All files must be in the same top-level directory and must include a SKILL.md file at the root of that directory.
    /// </summary>
    [JsonProperty("files")]
    public FileUploadRequest[] Files { get; set; }

    /// <summary>
    /// Creates a new create skill request.
    /// </summary>
    public CreateSkillRequest()
    {
    }

    /// <summary>
    /// Creates a new create skill request with a display title and optional files.
    /// </summary>
    /// <param name="displayTitle">The display title for the skill</param>
    /// <param name="files">Optional files to upload for the skill</param>
    public CreateSkillRequest(string displayTitle, FileUploadRequest[]? files = null)
    {
        DisplayTitle = displayTitle;
        Files = files ?? Array.Empty<FileUploadRequest>();
    }

    /// <summary>
    /// Converts the request to MultipartFormDataContent for API submission.
    /// </summary>
    /// <returns>MultipartFormDataContent ready for API submission</returns>
    public MultipartFormDataContent ToMultipartContent()
    {
        MultipartFormDataContent content = new MultipartFormDataContent();

        if (!string.IsNullOrEmpty(DisplayTitle))
        {
            content.Add(new StringContent(DisplayTitle), "display_title");
        }

        if (Files != null)
        {
            foreach (FileUploadRequest file in Files)
            {
                ByteArrayContent bc = new ByteArrayContent(file.Bytes);
                bc.Headers.ContentType = new MediaTypeHeaderValue(file.MimeType ?? "application/pdf");
                content.Add(bc, "files", file.Name);
            }
        }

        return content;
    }
}



/// <summary>
/// Response returned when creating a skill.
/// </summary>
public class CreateSkillResponse
{
    /// <summary>
    /// The display title for the skill.
    /// </summary>
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
/// Request to create a new skill version.
/// </summary>
public class CreateSkillVersionRequest
{
    /// <summary>
    /// Files to upload for the skill version.
    /// All files must be in the same top-level directory and must include a SKILL.md file at the root of that directory.
    /// </summary>
    [JsonProperty("files")]
    public FileUploadRequest[] Files { get; set; }

    /// <summary>
    /// Creates a new create skill version request.
    /// </summary>
    public CreateSkillVersionRequest()
    {
    }

    /// <summary>
    /// Creates a new create skill version request with files.
    /// </summary>
    /// <param name="files">Files to upload for the skill version</param>
    public CreateSkillVersionRequest(FileUploadRequest[]? files = null)
    {
        Files = files ?? Array.Empty<FileUploadRequest>();
    }

    /// <summary>
    /// Converts the request to MultipartFormDataContent for API submission.
    /// </summary>
    /// <returns>MultipartFormDataContent ready for API submission</returns>
    public MultipartFormDataContent ToMultipartContent()
    {
        MultipartFormDataContent content = new MultipartFormDataContent();

        if (Files != null)
        {
            foreach (FileUploadRequest file in Files)
            {
                ByteArrayContent bc = new ByteArrayContent(file.Bytes);
                bc.Headers.ContentType = new MediaTypeHeaderValue(file.MimeType ?? "application/pdf");
                content.Add(bc, "files", file.Name);
            }
        }

        return content;
    }
}
