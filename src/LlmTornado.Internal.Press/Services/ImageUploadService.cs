using LlmTornado.Internal.Press.Configuration;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Services;

/// <summary>
/// Static service for uploading images to public hosting services
/// </summary>
public static class ImageUploadService
{
    private static readonly HttpClient _httpClient = new HttpClient();

    /// <summary>
    /// Uploads an image URL or local file to the configured hosting service.
    /// Returns the original URL if upload is disabled or fails (graceful degradation).
    /// </summary>
    /// <param name="imageUrl">Image URL or local file path</param>
    /// <param name="config">Image upload configuration</param>
    /// <param name="logPrefix">Prefix for log messages (e.g., "ImageGeneration", "MemeGenerator")</param>
    /// <returns>Public URL (either uploaded or original)</returns>
    public static async Task<string> ProcessImageUrlAsync(
        string imageUrl,
        ImageUploadConfiguration config,
        string logPrefix = "ImageUpload")
    {
        // If upload is disabled, return original URL
        if (!config.Enabled)
        {
            return imageUrl;
        }

        // If input is empty, return it as-is
        if (string.IsNullOrEmpty(imageUrl))
        {
            return imageUrl;
        }

        try
        {
            Console.WriteLine($"  [{logPrefix}] 🔼 Uploading to {config.Provider}.host...");

            // Determine if this is a URL or local file path
            bool isUrl = imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                        imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            string? uploadedUrl = null;

            // Route to appropriate provider
            switch (config.Provider.ToLowerInvariant())
            {
                case "freeimage":
                    uploadedUrl = await UploadToFreeImageHostAsync(imageUrl, config.ApiKey, isUrl, logPrefix);
                    break;

                default:
                    Console.WriteLine($"  [{logPrefix}] ⚠ Unknown provider: {config.Provider}");
                    return imageUrl;
            }

            if (!string.IsNullOrEmpty(uploadedUrl))
            {
                Console.WriteLine($"  [{logPrefix}] ✓ Uploaded successfully: {Snippet(uploadedUrl, 60)}");
                return uploadedUrl;
            }
            else
            {
                Console.WriteLine($"  [{logPrefix}] ⚠ Upload returned empty URL");
                Console.WriteLine($"  [{logPrefix}] → Using original URL as fallback");
                return imageUrl;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [{logPrefix}] ⚠ Upload failed: {ex.Message}");
            Console.WriteLine($"  [{logPrefix}] → Using original URL as fallback");
            return imageUrl;
        }
    }

    /// <summary>
    /// Uploads an image to freeimage.host API
    /// </summary>
    private static async Task<string?> UploadToFreeImageHostAsync(
        string imagePathOrUrl,
        string apiKey,
        bool isUrl,
        string logPrefix)
    {
        const string apiEndpoint = "https://freeimage.host/api/1/upload";

        try
        {
            byte[] imageBytes;
            string tempFilePath = null;

            // If it's a URL, download it first
            if (isUrl)
            {
                Console.WriteLine($"  [{logPrefix}]   Downloading image from URL...");
                imageBytes = await _httpClient.GetByteArrayAsync(imagePathOrUrl);
                Console.WriteLine($"  [{logPrefix}]   Downloaded {imageBytes.Length} bytes");
            }
            else
            {
                // It's a local file
                if (!File.Exists(imagePathOrUrl))
                {
                    Console.WriteLine($"  [{logPrefix}]   ✗ Local file not found: {imagePathOrUrl}");
                    return null;
                }

                imageBytes = await File.ReadAllBytesAsync(imagePathOrUrl);
                Console.WriteLine($"  [{logPrefix}]   Read {imageBytes.Length} bytes from local file");
            }

            // Convert to base64
            string base64Image = Convert.ToBase64String(imageBytes);
            Console.WriteLine($"  [{logPrefix}]   Converted to base64 ({base64Image.Length} chars)");

            // Prepare POST request
            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(apiKey), "key");
            formData.Add(new StringContent(base64Image), "source");
            formData.Add(new StringContent("json"), "format");

            Console.WriteLine($"  [{logPrefix}]   Sending POST request to freeimage.host...");

            // Send request
            var response = await _httpClient.PostAsync(apiEndpoint, formData);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"  [{logPrefix}]   Response status: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"  [{logPrefix}]   Response body: {Snippet(responseContent, 200)}");
                return null;
            }

            // Parse response
            var apiResponse = JsonConvert.DeserializeObject<FreeImageHostResponse>(responseContent);

            if (apiResponse?.Image?.Url != null)
            {
                return apiResponse.Image.Url;
            }
            else
            {
                Console.WriteLine($"  [{logPrefix}]   Failed to extract URL from response");
                Console.WriteLine($"  [{logPrefix}]   Response: {Snippet(responseContent, 300)}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [{logPrefix}]   Exception during upload: {ex.Message}");
            return null;
        }
    }

    private static string Snippet(string text, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text))
            return "[empty]";

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }
}

/// <summary>
/// Response model for freeimage.host API
/// </summary>
internal class FreeImageHostResponse
{
    [JsonProperty("status_code")]
    public int StatusCode { get; set; }

    [JsonProperty("success")]
    public FreeImageHostSuccess? Success { get; set; }

    [JsonProperty("image")]
    public FreeImageHostImage? Image { get; set; }

    [JsonProperty("status_txt")]
    public string? StatusText { get; set; }
}

internal class FreeImageHostSuccess
{
    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("code")]
    public int Code { get; set; }
}

internal class FreeImageHostImage
{
    [JsonProperty("url")]
    public string Url { get; set; } = string.Empty;

    [JsonProperty("display_url")]
    public string? DisplayUrl { get; set; }

    [JsonProperty("size")]
    public int Size { get; set; }

    [JsonProperty("filename")]
    public string? Filename { get; set; }
}

