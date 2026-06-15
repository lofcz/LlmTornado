using System.Collections.Generic;
using System.Linq;
using LlmTornado.Chat.Vendors.Cohere;
using Newtonsoft.Json;

namespace LlmTornado.Images.Vendors.Google;

internal class VendorGoogleImageResult : VendorImageGenerationResult
{
    [JsonProperty("predictions")]
    public List<ImageResultPrediction>? Predictions { get; set; }

    [JsonProperty("candidates")]
    public List<VendorGoogleChatResult.VendorGoogleChatResultMessage>? Candidates { get; set; }

    public class ImageResultPrediction
    {
        [JsonProperty("bytesBase64Encoded")]
        public string BytesBase64Encoded { get; set; }
        
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }
    }
    
    public override ImageGenerationResult ToChatResult(string? postData)
    {
        if (Candidates?.Count > 0)
        {
            return new ImageGenerationResult
            {
                Data = Candidates
                    .SelectMany(c => c.Content?.Parts ?? [])
                    .Where(p => p.InlineData is not null)
                    .Select(p => new TornadoGeneratedImage
                    {
                        Base64 = p.InlineData!.Data,
                        MimeType = p.InlineData.MimeType
                    })
                    .ToList()
            };
        }

        return new ImageGenerationResult
        {
            Data = Predictions?.Select(x => new TornadoGeneratedImage
            {
                Base64 = x.BytesBase64Encoded,
                MimeType = x.MimeType
            }).ToList()
        };
    }
}