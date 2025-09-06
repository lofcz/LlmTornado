using Microsoft.JSInterop;

namespace LlmTornado.Chat.Web.Services;

/// <summary>
/// Service for handling file uploads and conversion to Base64
/// </summary>
public class FileUploadService
{
    private readonly IJSRuntime _jsRuntime;

    public FileUploadService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Opens file dialog and converts selected file to Base64 with MIME type
    /// </summary>
    /// <param name="acceptTypes">Accepted file types (e.g., "image/*", ".pdf", etc.)</param>
    /// <returns>Base64 string in format "data:{mimeType};base64,{base64String}" or null if cancelled</returns>
    public async Task<string?> SelectAndConvertFileAsync(string acceptTypes = "*/*")
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("fileUploadHelper.selectAndConvertFile", acceptTypes);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"File upload error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Converts a file input element to Base64
    /// </summary>
    /// <param name="inputElement">The file input element reference</param>
    /// <returns>Base64 string in format "data:{mimeType};base64,{base64String}" or null if no file</returns>
    public async Task<string?> ConvertFileToBase64Async(IJSObjectReference inputElement)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("fileUploadHelper.convertFileToBase64", inputElement);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"File conversion error: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets the MIME type and size information for a file
    /// </summary>
    /// <param name="base64Data">Base64 data string with MIME type prefix</param>
    /// <returns>File information object</returns>
    public FileInfo GetFileInfo(string base64Data)
    {
        if (string.IsNullOrEmpty(base64Data) || !base64Data.StartsWith("data:"))
        {
            return new FileInfo { IsValid = false };
        }

        try
        {
            var parts = base64Data.Split(';');
            if (parts.Length < 2) return new FileInfo { IsValid = false };

            var mimeType = parts[0].Substring(5); // Remove "data:" prefix
            var base64Part = parts[1];
            
            if (!base64Part.StartsWith("base64,"))
            {
                return new FileInfo { IsValid = false };
            }

            var base64String = base64Part.Substring(7); // Remove "base64," prefix
            var sizeInBytes = (base64String.Length * 3) / 4; // Approximate size

            return new FileInfo
            {
                IsValid = true,
                MimeType = mimeType,
                SizeInBytes = sizeInBytes,
                Base64Data = base64String
            };
        }
        catch
        {
            return new FileInfo { IsValid = false };
        }
    }
}

/// <summary>
/// Information about an uploaded file
/// </summary>
public class FileInfo
{
    public bool IsValid { get; set; }
    public string MimeType { get; set; } = "";
    public long SizeInBytes { get; set; }
    public string Base64Data { get; set; } = "";
    
    public string FormattedSize
    {
        get
        {
            if (SizeInBytes < 1024) return $"{SizeInBytes} B";
            if (SizeInBytes < 1024 * 1024) return $"{SizeInBytes / 1024:F1} KB";
            if (SizeInBytes < 1024 * 1024 * 1024) return $"{SizeInBytes / (1024 * 1024):F1} MB";
            return $"{SizeInBytes / (1024 * 1024 * 1024):F1} GB";
        }
    }
    
    public bool IsImage => MimeType.StartsWith("image/");
    public bool IsPdf => MimeType == "application/pdf";
    public bool IsAudio => MimeType.StartsWith("audio/");
    public bool IsVideo => MimeType.StartsWith("video/");
}