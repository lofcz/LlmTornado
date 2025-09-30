using A2A;
using LlmTornado.Chat;
using LlmTornado.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LlmTornado.A2A;
/// <summary>
/// ToDo:
/// - Handle more file types (e.g. video, other documents)
/// </summary>
public static partial class A2ATornadoExtension
{
    public static TextPart ToA2ATextPart(this string text) => new TextPart() { Text = text };

    public static FilePart ToA2AFileWithUriPart(this Uri uri) => new FilePart() { File = new FileWithUri() { Uri = uri.AbsoluteUri } };

    public static FilePart ToA2AFilePart(this ChatDocument doc)
    {
        if (doc.Uri != null)
            return new FilePart() { File = new FileWithUri() { Uri = doc.Uri.AbsoluteUri } };
        else
        {
            return new FilePart() { File = new FileWithBytes() { Bytes = doc.Base64 } };
        }
    }

    public static FilePart ToA2AFilePart(this ChatImage img) => new FilePart() { File = new FileWithUri() { Uri = img.Url, MimeType = img.MimeType } };

    public static FilePart ToA2AFilePart(this ChatAudio audio)
    {
        if (audio.Url != null)
            return new FilePart() { File = new FileWithUri() { Uri = audio.Url.AbsoluteUri } };
        else
            return new FilePart() { File = new FileWithBytes() { Bytes = audio.Data, MimeType = audio.MimeType } };
    }

    public static FilePart ToA2AFilePart(this ChatMessageAudio audio)
    {
       return new FilePart() { File = new FileWithBytes() { Bytes = audio.Data, Name = audio.Id, MimeType = audio.MimeType } };
    }

    public static FilePart ToA2AFilePart(this ChatVideo video) => new FilePart() { File = new FileWithUri() { Uri = video.Url.AbsoluteUri } };

    public static TextPart ToA2ATextPart(this ChatMessageReasoningData reasoning)
    {
        if (reasoning.IsRedacted ?? false)
        {
            JsonElement stringElement = JsonDocument.Parse($"{{\"message\": \"{reasoning.Content}\"}}").RootElement.GetProperty("message");
            return new TextPart() { Text = "Reasoning", Metadata = new Dictionary<string, JsonElement>() { { "Content", stringElement } } };
        }
        else
        {
            return new TextPart() { Text = reasoning.Content ?? "Reasoning" };
        }
    }

    public static FilePart ToA2AFilePart(this ChatMessagePartFileLinkData linkdata)
    {
        return new FilePart() { File = new FileWithUri() { Uri = linkdata.FileUri, Name = linkdata.File?.Name ?? "", MimeType = linkdata.MimeType } };
    }

    public static TextPart ToA2ATextPart(this ChatMessagePartExecutableCode code)
    {
        return new TextPart()
        {
            Text = code.Code ?? "Code",
            Metadata = new Dictionary<string, JsonElement>() {
                { "Language", JsonDocument.Parse($"\"{(code.CustomLanguage != null ? code.CustomLanguage : code.Language)}\"").RootElement }
            }
        };
    }
}