using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LlmTornado.ManagedAgents.Anthropic;

internal static class VendorAnthropicManagedAgentsJson
{
    internal static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
    {
        NullValueHandling = NullValueHandling.Ignore,
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        }
    };

    internal static string Serialize(object value) => JsonConvert.SerializeObject(value, Settings);
}
