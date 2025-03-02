using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace LlmTornado.Images.Vendors.Google;

internal class VendorGoogleImageResult : VendorImageGenerationResult
{
    [JsonProperty("predictions")]
    public List<ImageResultPrediction> Predictions { get; set; }

    public class ImageResultPrediction
    {
        [JsonProperty("bytesBase64Encoded")]
        public string BytesBase64Encoded { get; set; }
        
        [JsonProperty("mimeType")]
        public string MimeType { get; set; }
    }
    
    public override ImageGenerationResult ToChatResult(string? postData)
    {
        return new ImageGenerationResult
        {
            Data = Predictions.Select(x => new TornadoGeneratedImage
            {
                Base64 = x.BytesBase64Encoded,
                MimeType = x.MimeType
            }).ToList()
        };
    }
}