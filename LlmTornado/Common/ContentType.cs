using System.Runtime.Serialization;
using Argon;

namespace LlmTornado;

[JsonConverter(typeof(StringEnumConverter))]
public enum ContentType
{
    [EnumMember(Value = "text")] 
    Text,
    [EnumMember(Value = "image_url")] 
    ImageUrl,
    [EnumMember(Value = "image_file")] 
    ImageFile
}