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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    private static readonly Random _random = Random.Shared;
    private static bool? _c2paRemoverAvailable = null;
    private static readonly object _c2paRemoverLock = new();

    // Diverse image style hints to randomly inject for visual variety
    private static readonly string[] ImageStyleHints =
    [
        // Color & Gradient Styles
        "🎨 **Color Style: Vibrant Gradient Background**: Use a smooth gradient background transitioning between 2-3 complementary colors (e.g., blue to purple, orange to pink). The gradient should be subtle and professional, not overwhelming.",
        "🌈 **Color Style: Monochrome with Accent**: Create a primarily monochrome or grayscale image with a single vibrant accent color used strategically to highlight key elements.",
        "🌅 **Color Style: Warm Sunset Palette**: Use warm colors like oranges, reds, and yellows in a sunset-inspired gradient. Creates a friendly, approachable feeling.",
        "🌊 **Color Style: Cool Ocean Palette**: Use cool colors like blues, teals, and cyans. Creates a calm, professional, tech-forward aesthetic.",
        "⚡ **Color Style: High Contrast Neon**: Use bold neon colors (electric blue, hot pink, lime green) with dark backgrounds for a modern, energetic tech vibe.",
        "🌙 **Color Style: Dark Mode Aesthetic**: Use dark backgrounds (deep blacks, dark grays) with bright, saturated foreground elements. Perfect for developer-focused content.",
        
        // Art & Illustration Styles
        "✏️ **Art Style: Minimalist Line Art**: Use clean, simple line illustrations with minimal shading. Focus on essential shapes and forms. Very modern and clean.",
        "🎭 **Art Style: Isometric 3D Illustration**: Create an isometric perspective view showing objects from a 3/4 angle. Great for technical concepts and architecture diagrams.",
        "🖼️ **Art Style: Flat Design**: Use flat, two-dimensional shapes with solid colors and no gradients or shadows. Modern, clean, and professional.",
        "🎨 **Art Style: Abstract Geometric**: Use geometric shapes, patterns, and abstract forms to represent concepts. Very modern and artistic.",
        "🖌️ **Art Style: Watercolor Wash**: Use soft, flowing watercolor-like effects with translucent colors bleeding into each other. Creates a creative, artistic feel.",
        "📐 **Art Style: Technical Blueprint**: Style the image like a technical blueprint or architectural drawing with grid lines, measurements, and technical annotations.",
        "🎪 **Art Style: Collage/Mixed Media**: Combine different visual elements, textures, and styles in a collage-like composition. Creative and unique.",
        "🌌 **Art Style: Abstract Fluid**: Use flowing, organic shapes and forms that suggest movement and fluidity. Modern and dynamic.",
        
        // Composition & Layout
        "📐 **Composition: Rule of Thirds**: Place main subject off-center using the rule of thirds grid. More visually interesting than centered compositions.",
        "🎯 **Composition: Centered Symmetry**: Use perfect symmetry with the main element centered. Creates balance and focus.",
        "📊 **Composition: Split Screen**: Divide the image into two or more distinct sections showing different concepts or before/after states.",
        "🔍 **Composition: Close-Up Focus**: Zoom in tightly on a specific detail or element, creating an intimate, focused view.",
        "🌆 **Composition: Wide Panoramic**: Use a wide, panoramic view showing a broader scene or landscape. Great for showing context.",
        "⬆️ **Composition: Vertical Stack**: Arrange elements vertically in a stacked or layered composition. Good for showing hierarchy or progression.",
        
        // Visual Effects & Atmosphere
        "✨ **Effect: Soft Glow**: Add a soft, ethereal glow around key elements. Creates a magical, premium feeling.",
        "💫 **Effect: Particle Effects**: Include subtle particle effects, sparkles, or floating elements to add dynamism and energy.",
        "🌫️ **Effect: Depth of Field Blur**: Use depth of field effects with foreground in focus and background softly blurred. Creates professional photography feel.",
        "🔆 **Effect: Dramatic Lighting**: Use dramatic, directional lighting with strong shadows and highlights. Creates depth and drama.",
        "🌃 **Effect: Neon Glow**: Add neon-style glowing edges and outlines to elements. Perfect for tech and futuristic themes.",
        "💎 **Effect: Glass Morphism**: Use translucent, glass-like elements with subtle blur and transparency effects. Very modern UI aesthetic.",
        "🎆 **Effect: Light Rays**: Include visible light rays or beams cutting through the composition. Adds drama and guides the eye.",
        
        // Mood & Atmosphere
        "🚀 **Mood: Futuristic Tech**: Use sleek, high-tech aesthetics with metallic surfaces, holographic elements, and sci-fi inspired design.",
        "🏠 **Mood: Cozy & Warm**: Create a warm, inviting atmosphere with soft lighting, comfortable textures, and homey elements.",
        "⚡ **Mood: Energetic & Dynamic**: Use bold, action-oriented compositions with movement, speed lines, or dynamic angles.",
        "🧘 **Mood: Calm & Serene**: Create a peaceful, zen-like atmosphere with soft colors, gentle curves, and minimal elements.",
        "🎯 **Mood: Professional Corporate**: Use clean, corporate aesthetics with professional color schemes and business-focused imagery.",
        "🎨 **Mood: Creative & Playful**: Use whimsical, creative elements with unexpected colors, shapes, and playful compositions.",
        "🔬 **Mood: Scientific & Precise**: Use clean, precise illustrations with technical accuracy, grid systems, and scientific precision.",
        
        // Texture & Surface
        "🖼️ **Texture: Paper Texture Overlay**: Add subtle paper or canvas texture to give the image a tactile, handcrafted feel.",
        "💎 **Texture: Metallic Surfaces**: Use reflective, metallic surfaces with highlights and reflections. Modern and premium.",
        "🌊 **Texture: Liquid/Glass**: Include liquid or glass-like surfaces with transparency, reflections, and refractions.",
        "🌳 **Texture: Organic/Natural**: Use natural textures like wood grain, stone, or organic patterns. Adds warmth and authenticity.",
        "🔲 **Texture: Grid/Pattern**: Overlay subtle grid patterns or geometric textures. Great for technical and structured themes.",
        
        // Perspective & Camera Angles
        "📸 **Perspective: Bird's Eye View**: Show the scene from directly above, looking down. Great for showing layouts and systems.",
        "👁️ **Perspective: Worm's Eye View**: Show the scene from below, looking up. Creates dramatic, powerful compositions.",
        "🔄 **Perspective: 45-Degree Angle**: Use a 45-degree angled view showing both top and side. Great for 3D objects and concepts.",
        "📐 **Perspective: Isometric View**: Use isometric projection showing objects from a 3/4 angle with parallel lines. Perfect for technical illustrations.",
        
        // Special Techniques
        "🎭 **Technique: Double Exposure**: Blend two images or concepts together in a double exposure style. Creative and artistic.",
        "🌈 **Technique: Prism/Refraction**: Use prism effects, light refraction, or rainbow dispersion to add visual interest.",
        "🔮 **Technique: Holographic Effect**: Add holographic or iridescent effects with shifting colors and rainbow highlights.",
        "💫 **Technique: Motion Blur**: Include motion blur effects to suggest movement and speed. Dynamic and energetic.",
        "🌌 **Technique: Cosmic/Space Theme**: Incorporate space, stars, galaxies, or cosmic elements. Great for futuristic or abstract concepts."
    ];

    public ImageGenerationRunnable(
        TornadoApi client,
        AppConfiguration config,
        Orchestration orchestrator) : base(orchestrator)
    {
        _client = client;
        _config = config;

        // Agent to generate image prompts
        string instructions = $"""
            You are an expert at creating GPT Image generation prompts for technical blog articles.
            Your role is to generate descriptive, specific prompts that will create professional hero images.
            
            Guidelines for image prompts:
            1. Be specific and descriptive
            2. Include style guidance (e.g., "modern", "professional", "technical illustration")
            3. Specify composition and perspective
            4. Avoid text in images (GPT Image models can struggle with text)
            5. Focus on visual metaphors for technical concepts
            6. Keep prompts under 400 characters
            
            Style preferences:
            - Modern, clean aesthetic
            - Professional business/tech style
            - Suitable for blog hero images
            - Avoid cluttered or overly complex compositions
            
            Given an article title and description, generate a compelling GPT Image prompt
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
            
            // Step 1: Generate image prompt with style hints
            string styleHintsSection = "";
            int hintCount = _config.ImageGeneration.ImageStyleHints;
            if (hintCount > 0)
            {
                string[] selectedHints = GetRandomImageStyleHints(count: hintCount);
                if (selectedHints.Length > 0)
                {
                    Console.WriteLine($"  [ImageGeneration] 🎨 Selected {selectedHints.Length} image style hint(s):");
                    foreach (string hint in selectedHints)
                    {
                        // Extract just the hint title for logging (first part before colon)
                        string hintTitle = hint.Split(':')[0].Trim();
                        Console.WriteLine($"    • {hintTitle}");
                    }
                    
                    styleHintsSection = "\n\n**✨ VISUAL STYLE GUIDELINES FOR THIS IMAGE:**\n\n";
                    foreach (string hint in selectedHints)
                    {
                        styleHintsSection += $"{hint}\n\n";
                    }
                    styleHintsSection += "---\n\n";
                }
            }
            
            string promptRequest = $"""
                                    Generate a GPT Image prompt for an article with:
                Title: {article.Title}
                Description: {article.Description}
                {styleHintsSection}
                Create a professional, modern hero image prompt that captures the essence of this article.
                Incorporate the visual style guidelines above into your prompt.
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
            
            // Step 3b: Remove C2PA metadata if c2paremover is available
            string originalImagePath = tempImagePath;
            if (File.Exists(tempImagePath) && await IsC2PARemoverAvailableAsync())
            {
                Console.WriteLine($"  [ImageGeneration] 🔍 Checking for C2PA metadata...");
                bool hasC2PA = await CheckC2PAMetadataAsync(tempImagePath);
                
                if (hasC2PA)
                {
                    Console.WriteLine($"  [ImageGeneration] 🧹 Removing C2PA metadata...");
                    string cleanedPath = await RemoveC2PAMetadataAsync(tempImagePath);
                    
                    // Use cleaned file if removal was successful
                    if (cleanedPath != tempImagePath && File.Exists(cleanedPath))
                    {
                        tempImagePath = cleanedPath;
                    }
                }
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
            // Delete the current temp image (may be cleaned version)
            if (File.Exists(tempImagePath))
            {
                try { File.Delete(tempImagePath); } catch { /* Ignore cleanup errors */ }
            }
            
            // Delete original image if it's different from the cleaned version
            if (originalImagePath != tempImagePath && File.Exists(originalImagePath))
            {
                try { File.Delete(originalImagePath); } catch { /* Ignore cleanup errors */ }
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

    /// <summary>
    /// Selects random image style hints to add diversity to image generation
    /// </summary>
    private static string[] GetRandomImageStyleHints(int count)
    {
        if (count <= 0 || count > ImageStyleHints.Length)
            return [];

        // Create a copy of indices and shuffle
        List<int> indices = Enumerable.Range(0, ImageStyleHints.Length).ToList();
        
        // Fisher-Yates shuffle
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Take the first 'count' indices and return corresponding hints
        return indices.Take(count)
            .Select(i => ImageStyleHints[i])
            .ToArray();
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

    /// <summary>
    /// Checks if c2paremover command is available in the system PATH
    /// </summary>
    private static async Task<bool> IsC2PARemoverAvailableAsync()
    {
        lock (_c2paRemoverLock)
        {
            // Return cached result if we already checked
            if (_c2paRemoverAvailable.HasValue)
                return _c2paRemoverAvailable.Value;
        }

        string whereCommand = OperatingSystem.IsWindows() ? "where" : "which";
        
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = whereCommand,
            Arguments = "c2paremover",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using Process? process = Process.Start(startInfo);
            if (process == null)
            {
                lock (_c2paRemoverLock)
                {
                    _c2paRemoverAvailable = false;
                }
                return false;
            }

            // Read output asynchronously to prevent buffer deadlock
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();
            
            // Ensure streams are fully read
            await outputTask;
            await errorTask;

            bool available = process.ExitCode == 0;
            
            lock (_c2paRemoverLock)
            {
                _c2paRemoverAvailable = available;
            }
            
            return available;
        }
        catch
        {
            lock (_c2paRemoverLock)
            {
                _c2paRemoverAvailable = false;
            }
            return false;
        }
    }

    /// <summary>
    /// Checks if the image contains C2PA metadata
    /// </summary>
    private static async Task<bool> CheckC2PAMetadataAsync(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"  [ImageGeneration]   ⚠ Image file not found: {imagePath}");
            return false;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "c2paremover",
            Arguments = $"check \"{imagePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using Process? process = Process.Start(startInfo);
            if (process == null)
            {
                Console.WriteLine($"  [ImageGeneration]   ⚠ Failed to start c2paremover check process");
                return false;
            }

            // Read output asynchronously to prevent buffer deadlock
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            string output = await outputTask;
            string error = await errorTask;

            // Exit code 1 means C2PA metadata was detected
            if (process.ExitCode == 1)
            {
                Console.WriteLine($"  [ImageGeneration]   ⚠ C2PA metadata detected");
                return true;
            }
            else if (process.ExitCode == 0)
            {
                Console.WriteLine($"  [ImageGeneration]   ✓ No C2PA metadata found");
                return false;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine($"  [ImageGeneration]   ⚠ c2paremover check error: {error.Trim()}");
                }
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [ImageGeneration]   ⚠ Exception running c2paremover check: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Removes C2PA metadata from the image and returns the path to the cleaned file
    /// </summary>
    private static async Task<string> RemoveC2PAMetadataAsync(string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            Console.WriteLine($"  [ImageGeneration]   ⚠ Image file not found: {imagePath}");
            return imagePath;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "c2paremover",
            Arguments = $"remove \"{imagePath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            using Process? process = Process.Start(startInfo);
            if (process == null)
            {
                Console.WriteLine($"  [ImageGeneration]   ⚠ Failed to start c2paremover remove process");
                return imagePath;
            }

            // Read output asynchronously to prevent buffer deadlock
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            string output = await outputTask;
            string error = await errorTask;

            if (process.ExitCode != 0)
            {
                if (!string.IsNullOrWhiteSpace(error))
                {
                    Console.WriteLine($"  [ImageGeneration]   ⚠ c2paremover remove error: {error.Trim()}");
                }
                return imagePath;
            }

            // Construct cleaned filename: {baseName}_cleaned.{extension}
            string directory = Path.GetDirectoryName(imagePath) ?? ".";
            string fileName = Path.GetFileNameWithoutExtension(imagePath);
            string extension = Path.GetExtension(imagePath);
            string cleanedPath = Path.Combine(directory, $"{fileName}_cleaned{extension}");

            if (File.Exists(cleanedPath))
            {
                Console.WriteLine($"  [ImageGeneration]   ✓ C2PA metadata removed, cleaned file: {Path.GetFileName(cleanedPath)}");
                return cleanedPath;
            }
            else
            {
                Console.WriteLine($"  [ImageGeneration]   ⚠ Cleaned file not found at expected path: {cleanedPath}");
                return imagePath;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [ImageGeneration]   ⚠ Exception running c2paremover remove: {ex.Message}");
            return imagePath;
        }
    }
}

