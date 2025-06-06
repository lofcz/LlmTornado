using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LlmTornado.Chat.Vendors.Cohere;

/// <summary>
/// Dictates how the prompt will be constructed.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum ChatVendorCohereExtensionPromptTruncation
{
    /// <summary>
    /// No elements will be dropped. If the sum of the inputs exceeds the model’s context length limit, a TooManyTokens error will be returned.
    /// </summary>
    [EnumMember(Value = "OFF")] 
    Off,
    /// <summary>
    /// Only compatible with: Cohere Platform Only. Some elements from chat_history and documents will be dropped in an attempt to construct a prompt that fits within the model’s context length limit. During this process the order of the documents and chat history will be changed and ranked by relevance.
    /// </summary>
    [EnumMember(Value = "AUTO")] 
    Auto,
    /// <summary>
    /// Only compatible with: Azure, AWS Sagemaker/Bedrock, Private Deployments. Some elements from chat_history and documents will be dropped in an attempt to construct a prompt that fits within the model’s context length limit. During this process the order of the documents and chat history will be preserved as they are inputted into the API.
    /// </summary>
    [EnumMember(Value = "AUTO_PRESERVE_ORDER")] 
    AutoPreserveOrder
}