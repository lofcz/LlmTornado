using LlmTornado.Files;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace LlmTornado.Skills;

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
        Files = Array.Empty<FileUploadRequest>();
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

        foreach (FileUploadRequest file in Files)
        {
            if (file == null || file.Bytes == null || string.IsNullOrEmpty(file.Name))
            {
                continue;
            }

            ByteArrayContent bc = new ByteArrayContent(file.Bytes);
            bc.Headers.ContentType = new MediaTypeHeaderValue(file.MimeType ?? "application/pdf");
            content.Add(bc, "files[]", file.Name);
        }

        return content;
    }
}
