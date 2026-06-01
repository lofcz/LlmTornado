using System.Net.Http;
using System.Net.Http.Headers;
using LlmTornado.Files;

namespace LlmTornado.Skills;

/// <summary>
/// Request to upload files for an OpenAI skill or skill version.
/// </summary>
public class OpenAiSkillUploadRequest
{
    /// <summary>
    /// Skill files to upload, or a single zip archive.
    /// </summary>
    public FileUploadRequest[] Files { get; set; } = [];

    /// <summary>
    /// When creating a version, whether to set it as the default version.
    /// </summary>
    public bool? SetAsDefault { get; set; }

    public OpenAiSkillUploadRequest()
    {
    }

    public OpenAiSkillUploadRequest(params FileUploadRequest[] files)
    {
        Files = files;
    }

    public MultipartFormDataContent ToMultipartContent()
    {
        MultipartFormDataContent content = new MultipartFormDataContent();

        if (SetAsDefault is not null)
        {
            content.Add(new StringContent(SetAsDefault.Value ? "true" : "false"), "default");
        }

        foreach (FileUploadRequest file in Files)
        {
            if (file.Bytes is null || string.IsNullOrEmpty(file.Name))
            {
                continue;
            }

            ByteArrayContent bc = new ByteArrayContent(file.Bytes);
            bc.Headers.ContentType = new MediaTypeHeaderValue(file.MimeType ?? "application/octet-stream");
            content.Add(bc, "files", file.Name);
        }

        return content;
    }
}
