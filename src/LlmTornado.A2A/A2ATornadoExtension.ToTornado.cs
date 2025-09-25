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
    public static ChatMessagePart ToTornadoMessagePart(this TextPart textPart) => new ChatMessagePart { Text = textPart.Text };
    public static ChatMessagePart ToTornadoMessagePart(this FilePart filePart)
    {
        if (filePart.File is FileWithUri fileWithUri)
        {
            return ChatMessagePart.Create(new Uri(fileWithUri.Uri), ChatMessageTypes.Image);
        }
        else if (filePart.File is FileWithBytes fileWithBytes)
        {
            if (fileWithBytes.Bytes.Contains("image"))
            {
                return new ChatMessagePart(fileWithBytes.Bytes, Images.ImageDetail.Auto);
            }
            else if (fileWithBytes.Bytes.Contains("audio"))
            {
                if (fileWithBytes.MimeType == "audio/wav" || fileWithBytes.MimeType == "audio/x-wav")
                    return ChatMessagePart.Create(fileWithBytes.Bytes, ChatAudioFormats.Wav);
                else if (fileWithBytes.MimeType == "audio/mpeg" || fileWithBytes.MimeType == "audio/mp3" || fileWithBytes.MimeType == "audio/x-mp3")
                    return ChatMessagePart.Create(fileWithBytes.Bytes, ChatAudioFormats.Mp3);
            }
            //ToDo - handle other types
            return new ChatMessagePart(fileWithBytes.Bytes, DocumentLinkTypes.Base64);
        }
        return null;
    }
}