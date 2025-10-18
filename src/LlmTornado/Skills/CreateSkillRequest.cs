using LlmTornado.Files;
using Newtonsoft.Json;
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
