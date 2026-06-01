using System.Threading;
using System.Threading.Tasks;
using LlmTornado.Audio.Models;
using LlmTornado.Audio.Models.Google;
using LlmTornado.Chat.Vendors.Cohere;
using LlmTornado.Code;
using LlmTornado.Code.Vendor;
using LlmTornado.Common;
using Newtonsoft.Json;

namespace LlmTornado.Audio.Vendors.Google;

/// <summary>
/// Handles Lyria 3 music generation via Google's generateContent API.
/// </summary>
internal static class VendorGoogleMusicHandler
{
    public static async Task<MusicGenerationResult?> Generate(
        MusicGenerationRequest request,
        IEndpointProvider provider,
        EndpointBase endpoint,
        CancellationToken cancellationToken = default)
    {
        AudioModel model = request.Model ?? AudioModel.Google.Lyria.Lyria3ClipPreview;
        VendorGoogleMusicRequest googleRequest = new VendorGoogleMusicRequest(request);
        string body = JsonConvert.SerializeObject(googleRequest, EndpointBase.NullSettings);
        string url = $"{provider.ApiUrl(CapabilityEndpoints.Chat, null)}/{model.Name}:generateContent";
        
        HttpCallResult<VendorGoogleChatResult> result = await endpoint.HttpPost<VendorGoogleChatResult>(
            provider,
            CapabilityEndpoints.Music,
            url,
            postData: body,
            ct: cancellationToken
        ).ConfigureAwait(false);
        
        if (!result.Ok || result.Data is null)
        {
            return null;
        }
        
        return VendorGoogleMusicResult.ToResult(result.Data);
    }
}
