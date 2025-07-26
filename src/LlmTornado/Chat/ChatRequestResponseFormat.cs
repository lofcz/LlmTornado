using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using LlmTornado.Code;
using LlmTornado.Common;
using LlmTornado.Infra;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace LlmTornado.Chat;

/// <summary>
///     Represents requested type of response
/// </summary>
public class ChatRequestResponseFormats
{
    internal class ChatRequestResponseJsonSchema
    {
        [JsonProperty("strict")]
        public bool? Strict { get; set; }
        
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("description")]
        public string? Description { get; set; }
        
        [JsonProperty("schema")]
        public object Schema { get; set; }
        
        [JsonIgnore]
        public Delegate? Delegate { get; set; }
        
        [JsonIgnore]
        internal DelegateMetadata? DelegateMetadata { get; set; }
        
        [JsonIgnore]
        internal ToolMetadata? ToolMetadata { get; set; }
    }
    
    /// <summary>
    ///     Type of the response
    /// </summary>
    [JsonProperty("type")]
    public ChatRequestResponseFormatTypes? Type { get; set; }

    [JsonProperty("json_schema", NullValueHandling = NullValueHandling.Ignore)]
    internal ChatRequestResponseJsonSchema? Schema { get; set; }
    
    internal ChatRequestResponseFormats() { }

    /// <summary>
    /// Serializes schema for a given provider.
    /// </summary>
    public void Serialize(IEndpointProvider provider)
    {
        if (Schema?.Delegate is null)
        {
            return;
        }

        DelegateMetadata cd = ToolFactory.CreateFromMethod(Schema.Delegate, null, provider);
        Schema.DelegateMetadata = cd;
        Schema.Schema = cd.ToolFunction.Parameters;
    }

    /// <summary>
    /// Invokes the <see cref="Delegate"/> with the given JSON data.
    /// </summary>
    public async ValueTask<MethodInvocationResult> Invoke(string data)
    {
        return await Clr.Invoke(Schema?.Delegate, Schema?.DelegateMetadata, data);
    }

    /// <summary>
    ///     Signals the output should be plaintext.
    /// </summary>
    public static readonly ChatRequestResponseFormats Text = new ChatRequestResponseFormats
    {
        Type = ChatRequestResponseFormatTypes.Text
    };

    /// <summary>
    ///     Signals output should be JSON. The string "JSON" needs to be included in either system or user message in the conversation.<br/>
    ///     <b>This is legacy tech. Consider switching to <see cref="ChatRequestResponseFormats.StructuredJson"/>.</b>
    /// </summary>
    public static readonly ChatRequestResponseFormats Json = new ChatRequestResponseFormats
    {
        Type = ChatRequestResponseFormatTypes.Json
    };
    
    /// <summary>
    ///     Signals output should be structured JSON. The provided schema will always be followed.
    /// </summary>
    public static ChatRequestResponseFormats StructuredJson(string name, object schema, bool? strict = true)
    {
        return new ChatRequestResponseFormats
        {
            Type = ChatRequestResponseFormatTypes.StructuredJson,
            Schema = new ChatRequestResponseJsonSchema
            {
                Name = name,
                Strict = strict,
                Schema = schema
            }
        };
    }

    /// <summary>
    ///     Signals output should be structured JSON. The provided schema will always be followed.
    /// </summary>
    internal static ChatRequestResponseFormats StructuredJson(Delegate function)
    {
        return new ChatRequestResponseFormats
        {
            Type = ChatRequestResponseFormatTypes.StructuredJson,
            Schema = new ChatRequestResponseJsonSchema
            {
                Name = "output",
                Strict = true,
                Delegate = function
            }
        };
    }
    
    /// <summary>
    ///     Signals output should be structured JSON. The provided schema will always be followed.
    /// </summary>
    internal static ChatRequestResponseFormats StructuredJson(Delegate function, string name, bool? strict = true)
    {
        return new ChatRequestResponseFormats
        {
            Type = ChatRequestResponseFormatTypes.StructuredJson,
            Schema = new ChatRequestResponseJsonSchema
            {
                Name = name,
                Strict = strict,
                Delegate = function
            }
        };
    }
    
    /// <summary>
    ///     Signals output should be structured JSON. The provided schema will always be followed.
    /// </summary>
    internal static ChatRequestResponseFormats StructuredJson(Delegate function, string name, string description, bool? strict = true)
    {
        return new ChatRequestResponseFormats
        {
            Type = ChatRequestResponseFormatTypes.StructuredJson,
            Schema = new ChatRequestResponseJsonSchema
            {
                Name = name,
                Strict = strict,
                Delegate = function,
                Description = description
            }
        };
    }
    
    /// <summary>
    ///     Signals output should be structured JSON. The provided schema will always be followed.
    /// </summary>
    internal static ChatRequestResponseFormats StructuredJson(Delegate function, ToolMetadata metadata, bool? strict = true)
    {
        return new ChatRequestResponseFormats
        {
            Type = ChatRequestResponseFormatTypes.StructuredJson,
            Schema = new ChatRequestResponseJsonSchema
            {
                Name = "output",
                Strict = strict,
                Delegate = function,
                ToolMetadata = metadata
            }
        };
    }
    
    /// <summary>
    ///     Signals output should be structured JSON. The provided schema will always be followed.
    /// </summary>
    internal static ChatRequestResponseFormats StructuredJson(Delegate function, string name, ToolMetadata metadata, bool? strict = true)
    {
        return new ChatRequestResponseFormats
        {
            Type = ChatRequestResponseFormatTypes.StructuredJson,
            Schema = new ChatRequestResponseJsonSchema
            {
                Name = name,
                Strict = strict,
                Delegate = function,
                ToolMetadata = metadata
            }
        };
    }
    
    /// <summary>
    ///     Signals output should be structured JSON. The provided schema will always be followed.
    /// </summary>
    internal static ChatRequestResponseFormats StructuredJson(Delegate function, string name, string description, ToolMetadata metadata, bool? strict = true)
    {
        return new ChatRequestResponseFormats
        {
            Type = ChatRequestResponseFormatTypes.StructuredJson,
            Schema = new ChatRequestResponseJsonSchema
            {
                Name = name,
                Strict = strict,
                Delegate = function,
                ToolMetadata = metadata,
                Description = description
            }
        };
    }
}

/// <summary>
///     Represents response types 
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ChatRequestResponseFormatTypes
{
    /// <summary>
    /// Response should be in plaintext format, default.
    /// </summary>
    [EnumMember(Value = "text")]
    Text,
    
    /// <summary>
    /// Response should be in JSON. System prompt must include "JSON" substring.
    /// </summary>
    [EnumMember(Value = "json_object")]
    Json,
    
    /// <summary>
    /// Response should be in structured JSON. The model will always follow the provided schema.
    /// </summary>
    [EnumMember(Value = "json_schema")]
    StructuredJson
}