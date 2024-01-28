using System.Runtime.Serialization;

namespace OpenAiNg;

public enum ContentType
{
    [EnumMember(Value = "text")] Text,
    [EnumMember(Value = "image_url")] ImageUrl,
    [EnumMember(Value = "image_file")] ImageFile
}