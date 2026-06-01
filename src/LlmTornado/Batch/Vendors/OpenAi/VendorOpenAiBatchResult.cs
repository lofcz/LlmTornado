using LlmTornado.Videos;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LlmTornado.Batch.Vendors.OpenAi;

/// <summary>
/// Handles deserialization of OpenAI batch results.
/// </summary>
internal static class VendorOpenAiBatchResult
{
    /// <summary>
    /// Deserializes an OpenAI batch result from JSON.
    /// </summary>
    /// <param name="jsonData">The JSON data to deserialize.</param>
    /// <returns>The deserialized batch result.</returns>
    public static BatchResult? Deserialize(string jsonData)
    {
        JObject root = JObject.Parse(jsonData);
        BatchResult? result = root.ToObject<BatchResult>();
        if (result is null)
        {
            return null;
        }

        result.RawResponse = jsonData;

        JToken? bodyToken = root.SelectToken("response.body");
        if (bodyToken is JObject bodyObject && bodyObject.Value<string>("object") == "video")
        {
            result.ResponseInternal ??= new BatchResultResponse();
            result.ResponseInternal.VideoBody = bodyObject.ToObject<VideoJob>();
        }

        return result;
    }
}
