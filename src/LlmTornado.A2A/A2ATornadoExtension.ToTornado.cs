using A2A;
using LlmTornado.Chat;
using LlmTornado.Code;

namespace LlmTornado.A2A;
/// <summary>
/// ToDo:
/// - Handle more file types (e.g. video, other documents)
/// </summary>
public static partial class A2ATornadoExtension
{
    public static ChatMessagePart ToTornadoMessagePart(this TextPart textPart) => new ChatMessagePart { Text = textPart.Text };
    public static ChatMessagePart? ToTornadoMessagePart(this FilePart filePart)
    {
        FileContent file = filePart.File;

        if (file.Uri is not null)
        {
            return ChatMessagePart.Create(file.Uri, ChatMessageTypes.Image);
        }
        
        if (file.Bytes is not null)
        {
            if (file.MimeType?.Contains("image") == true)
            {
                return new ChatMessagePart(file.Bytes, Images.ImageDetail.Auto);
            }
            
            if (file.MimeType?.Contains("audio") == true)
            {
                switch (file.MimeType)
                {
                    case "audio/wav" or "audio/x-wav":
                        return ChatMessagePart.Create(file.Bytes, ChatAudioFormats.Wav);
                    case "audio/mpeg" or "audio/mp3" or "audio/x-mp3":
                        return ChatMessagePart.Create(file.Bytes, ChatAudioFormats.Mp3);
                }
            }

            return new ChatMessagePart(file.Bytes, DocumentLinkTypes.Base64);
        }
        
        return null;
    }
}