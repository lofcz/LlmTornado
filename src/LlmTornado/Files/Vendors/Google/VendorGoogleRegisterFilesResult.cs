using System.Collections.Generic;
using LlmTornado.Chat;
using LlmTornado.Files;
using Newtonsoft.Json;

namespace LlmTornado.Files.Vendors.Google;

internal class VendorGoogleRegisterFilesRequest
{
    [JsonProperty("uris")]
    public List<string> Uris { get; set; } = [];
}

internal class VendorGoogleRegisterFilesResult
{
    [JsonProperty("files")]
    public List<VendorGoogleTornadoFileContent>? Files { get; set; }

    public List<TornadoFile> ToFiles()
    {
        if (Files is null)
        {
            return [];
        }

        List<TornadoFile> result = [];
        foreach (VendorGoogleTornadoFileContent file in Files)
        {
            result.Add(file.ToFile(null));
        }

        return result;
    }
}

/// <summary>
/// Registers Google Cloud Storage objects with the Gemini File API for use in generation requests.
/// Requires OAuth credentials with <c>devstorage.read_only</c> scope; API keys alone are not sufficient.
/// </summary>
public class GeminiRegisterGcsFilesRequest
{
    /// <summary>
    /// GCS URIs to register, e.g. <c>gs://my-bucket/path/file.pdf</c>.
    /// </summary>
    public List<string> Uris { get; set; } = [];

    /// <summary>
    /// OAuth 2.0 access token with Cloud Storage read access for the target buckets.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Google Cloud project id sent via <c>x-goog-user-project</c>. Required for some billing setups.
    /// </summary>
    public string? GoogleCloudProject { get; set; }
}
