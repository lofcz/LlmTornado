
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OpenAiNg;

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