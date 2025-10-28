using LlmTornado.Agents;
using LlmTornado.Agents.ChatRuntime.Orchestration;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using LlmTornado.Code;
using LlmTornado.Images;
using LlmTornado.Internal.Press.Configuration;
using LlmTornado.Internal.Press.DataModels;
using LlmTornado.Internal.Press.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Agents;

/// <summary>
/// Represents a generated image that can be either a URL or base64 data
/// </summary>
internal class GeneratedImageResult
{
    public string? Url { get; set; }
    public string? Base64 { get; set; }
    public bool IsBase64 => !string.IsNullOrEmpty(Base64);
    public bool IsUrl => !string.IsNullOrEmpty(Url);
    public bool IsEmpty => string.IsNullOrEmpty(Url) && string.IsNullOrEmpty(Base64);
}

public class ImageGenerationRunnable : OrchestrationRunnable<ArticleOutput, ImageOutput>
{
    private readonly TornadoAgent _promptAgent;
    private readonly TornadoApi _client;
    private readonly AppConfiguration _config;

    public ImageGenerationRunnable(
        TornadoApi client,
        AppConfiguration config,
        Orchestration orchestrator) : base(orchestrator)
    {
        _client = client;
        _config = config;

        // Agent to generate image prompts
        string instructions = $"""
            You are an expert at creating DALL-E image generation prompts for technical blog articles.
            Your role is to generate descriptive, specific prompts that will create professional hero images.
            
            Guidelines for image prompts:
            1. Be specific and descriptive
            2. Include style guidance (e.g., "modern", "professional", "technical illustration")
            3. Specify composition and perspective
            4. Avoid text in images (DALL-E struggles with text)
            5. Focus on visual metaphors for technical concepts
            6. Keep prompts under 400 characters
            
            Style preferences:
            - Modern, clean aesthetic
            - Professional business/tech style
            - Suitable for blog hero images
            - Avoid cluttered or overly complex compositions
            
            Given an article title and description, generate a compelling DALL-E prompt
            that creates an appropriate hero image.
            """;

        ChatModel model = new ChatModel(config.Models.ImagePrompt);

        _promptAgent = new TornadoAgent(
            client: client,
            model: model,
            name: "Image Prompt Generator",
            instructions: instructions);
    }

    public override async ValueTask<ImageOutput> Invoke(RunnableProcess<ArticleOutput, ImageOutput> process)
    {
        if (!_config.ImageGeneration.Enabled)
        {
            Console.WriteLine("  [ImageGeneration] Image generation disabled");
            return new ImageOutput
            {
                Url = string.Empty,
                AltText = process.Input.Title,
                PromptUsed = "Image generation disabled",
                Provider = "none"
            };
        }

        process.RegisterAgent(_promptAgent);

        ArticleOutput article = process.Input;

        try
        {
            Console.WriteLine($"  [ImageGeneration] Generating image for: {article.Title}");
            
            // Step 1: Generate image prompt
            string promptRequest = $"""
                                    Generate a DALL-E 3 image prompt for an article with:
                Title: {article.Title}
                Description: {article.Description}
                
                Create a professional, modern hero image prompt that captures the essence of this article.
                Return only the prompt text, nothing else.
                """;

            Conversation promptConversation = await _promptAgent.Run(promptRequest, singleTurn: true);
            string imagePrompt = promptConversation.Messages.Last().Content?.Trim() ?? 
                            $"Modern technical illustration representing {article.Title}";

            Console.WriteLine($"  [ImageGeneration] Prompt: {Snippet(imagePrompt, 100)}");

            // Step 2: Generate image with retry logic
            GeneratedImageResult imageResult = await GenerateImageWithRetry(imagePrompt, maxRetries: 2);
            
            if (imageResult.IsEmpty)
            {
                Console.WriteLine("  [ImageGeneration] ⚠ Failed to generate image after retries, continuing without image");
                return new ImageOutput
                {
                    Url = string.Empty,
                    AltText = article.Title,
                    PromptUsed = imagePrompt,
                    Provider = "skipped-after-retries"
                };
            }

            Console.WriteLine($"  [ImageGeneration] ✓ Image generated successfully");
            
            // Step 3: Save image locally (either from URL or base64)
            string tempImagePath = Path.Combine(Path.GetTempPath(), $"hero_{Guid.NewGuid()}.png");
            
            if (imageResult.IsBase64)
            {
                // Save base64 directly to file
                Console.WriteLine($"  [ImageGeneration]   Saving base64 image to temp file...");
                byte[] imageBytes = Convert.FromBase64String(imageResult.Base64!);
                await File.WriteAllBytesAsync(tempImagePath, imageBytes);
            }
            else if (imageResult.IsUrl)
            {
                // Download from URL
                Console.WriteLine($"  [ImageGeneration]   Downloading image from URL...");
                await MemeService.DownloadImageFromUrlAsync(
                    imageResult.Url!, 
                    Path.GetTempPath(), 
                    Path.GetFileName(tempImagePath));
            }
            
            // Step 4: Generate image variations if enabled
            Dictionary<string, string> variationPaths = new Dictionary<string, string>();
            if (_config.ImageVariations.Enabled && File.Exists(tempImagePath))
            {
                List<ImageVariationService.ImageVariation> variations = _config.ImageVariations.Formats
                    .Select(f => new ImageVariationService.ImageVariation
                    {
                        Width = f.Width,
                        Height = f.Height,
                        Description = f.Description
                    })
                    .ToList();

                Dictionary<string, string> generatedPaths = await ImageVariationService.GenerateVariationsAsync(
                    tempImagePath,
                    variations,
                    "ImageGeneration");

                // Map the generated variations back to their configured names
                // generatedPaths uses "1000x420" format, we need to map to config names
                // IMPORTANT: Don't upload yet, just map the local paths
                Dictionary<string, string> localVariationPaths = new Dictionary<string, string>();
                foreach (Configuration.ImageVariationFormat format in _config.ImageVariations.Formats)
                {
                    string sizeKey = $"{format.Width}x{format.Height}";
                    if (generatedPaths.TryGetValue(sizeKey, out string? path))
                    {
                        localVariationPaths[format.Name] = path;
                    }
                }
                
                // Step 4b: Upload variations if upload service is enabled
                if (_config.ImageUpload.Enabled && localVariationPaths.Count > 0)
                {
                    Console.WriteLine($"  [ImageGeneration] 🔼 Uploading {localVariationPaths.Count} variation(s)...");
                    foreach (KeyValuePair<string, string> kvp in localVariationPaths)
                    {
                        string publicVariationUrl = await ImageUploadService.ProcessImageUrlAsync(
                            kvp.Value,
                            _config.ImageUpload,
                            $"ImageGeneration/{kvp.Key}");
                        
                        // Store the public URL instead of local path
                        variationPaths[kvp.Key] = publicVariationUrl;
                    }
                    Console.WriteLine($"  [ImageGeneration] ✓ Uploaded {variationPaths.Count} variation(s)");
                }
                else
                {
                    // If upload is disabled, use local paths
                    variationPaths = localVariationPaths;
                }
            }
            
            // Step 5: Process main image through upload service if enabled
            // For base64, we upload the local file; for URL, we upload the URL
            string publicUrl;
            if (imageResult.IsBase64)
            {
                // Upload the temp file we just saved
                publicUrl = await ImageUploadService.ProcessImageUrlAsync(
                    tempImagePath,
                    _config.ImageUpload,
                    "ImageGeneration");
            }
            else
            {
                // Upload the URL
                publicUrl = await ImageUploadService.ProcessImageUrlAsync(
                    imageResult.Url!,
                    _config.ImageUpload,
                    "ImageGeneration");
            }
            
            // Step 6: Clean up temp files
            if (File.Exists(tempImagePath))
            {
                try { File.Delete(tempImagePath); } catch { /* Ignore cleanup errors */ }
            }
            
            // Clean up variation temp files
            foreach (string variationPath in variationPaths.Values)
            {
                // Only delete if it's a local file (starts with temp path or relative path)
                if (File.Exists(variationPath) && !variationPath.StartsWith("http"))
                {
                    try { File.Delete(variationPath); } catch { /* Ignore cleanup errors */ }
                }
            }
            
            return new ImageOutput
            {
                Url = publicUrl,
                AltText = article.Title,
                PromptUsed = imagePrompt,
                Provider = _config.ImageGeneration.Model,
                Variations = variationPaths.Count > 0 ? variationPaths : null
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [ImageGeneration] ✗ Critical error: {ex.Message}");
            Console.WriteLine($"  [ImageGeneration] Continuing without image");
            
            return new ImageOutput
            {
                Url = string.Empty,
                AltText = article.Title,
                PromptUsed = $"Error: {ex.Message}",
                Provider = "error"
            };
        }
    }

    private async Task<GeneratedImageResult> GenerateImageWithRetry(string prompt, int maxRetries)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    int delay = attempt * 2000; // 2s, 4s
                    Console.WriteLine($"  [ImageGeneration]   Retry {attempt}/{maxRetries} after {delay}ms delay...");
                    await Task.Delay(delay);
                }

                var imageResult = await GenerateImage(prompt);
                
                if (!imageResult.IsEmpty)
                {
                    if (attempt > 0)
                    {
                        Console.WriteLine($"  [ImageGeneration]   ✓ Retry {attempt} succeeded");
                    }
                    return imageResult;
                }
                
                Console.WriteLine($"  [ImageGeneration]   Attempt {attempt + 1} returned empty result");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  [ImageGeneration]   Attempt {attempt + 1} failed: {ex.Message}");
                
                if (attempt == maxRetries)
                {
                    Console.WriteLine($"  [ImageGeneration]   ✗ All {maxRetries + 1} attempts exhausted");
                    return new GeneratedImageResult();
                }
            }
        }

        return new GeneratedImageResult();
    }

    private string Snippet(string text, int maxLength = 100)
    {
        if (string.IsNullOrEmpty(text))
            return "[empty]";

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }

    private async Task<GeneratedImageResult> GenerateImage(string prompt)
    {
        try
        {
            // LlmTornado handles multi-provider routing automatically
            ImageGenerationRequest imageRequest = new ImageGenerationRequest(prompt)
            {
                Model = _config.ImageGeneration.Model,
                NumOfImages = 1
            };

            ImageGenerationResult? result = await _client.ImageGenerations.CreateImage(imageRequest);
            
            if (result?.Data != null && result.Data.Count > 0)
            {
                var imageData = result.Data[0];
                
                // Check for base64 first (some providers return this)
                if (!string.IsNullOrEmpty(imageData.Base64))
                {
                    Console.WriteLine($"  [ImageGeneration]   Received base64 image ({imageData.Base64.Length} chars)");
                    return new GeneratedImageResult { Base64 = imageData.Base64 };
                }
                
                // Otherwise return URL
                if (!string.IsNullOrEmpty(imageData.Url))
                {
                    Console.WriteLine($"  [ImageGeneration]   Received image URL");
                    return new GeneratedImageResult { Url = imageData.Url };
                }
            }

            return new GeneratedImageResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Image generation API error: {ex.Message}");
            return new GeneratedImageResult();
        }
    }
}

