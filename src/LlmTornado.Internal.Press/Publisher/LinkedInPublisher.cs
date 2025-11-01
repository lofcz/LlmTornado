using Flurl.Http;
using LlmTornado.Internal.Press.Database;
using LlmTornado.Internal.Press.Database.Models;
using Microsoft.EntityFrameworkCore;
using LlmTornado.Chat;
using LlmTornado.Chat.Models;
using System.Text.Json;
using System.IO;
using LlmTornado.Internal.Press.Configuration;
using LlmTornado.Internal.Press.DataModels;
using System.Linq;
using System.Net.Http;

namespace LlmTornado.Internal.Press.Publisher;

public static class LinkedInPublisher
{
    private const string API_BASE_URL = "https://api.linkedin.com/v2/ugcPosts";
    private const string ASSETS_API_URL = "https://api.linkedin.com/v2/assets?action=registerUpload";
    private const int MAX_RETRIES = 2;
    
    public static async Task<bool> PublishArticleAsync(
        Article article, 
        string accessToken,
        string authorUrn,
        PressDbContext dbContext,
        TornadoApi? aiClient = null,
        AppConfiguration? config = null,
        string? summaryJson = null)
    {
        Console.WriteLine($"[LinkedIn] 📤 Publishing: {article.Title}");
        
        // Check if already published
        var publishStatus = await dbContext.ArticlePublishStatus
            .FirstOrDefaultAsync(p => p.ArticleId == article.Id && p.Platform == "linkedin");
            
        if (publishStatus?.Status == "Published")
        {
            Console.WriteLine($"[LinkedIn] ℹ️ Already published: Post ID {publishStatus.PlatformArticleId}");
            return true;
        }
        
        // Get dev.to URL - will be included at the end of the post
        var devToStatus = await dbContext.ArticlePublishStatus
            .FirstOrDefaultAsync(p => p.ArticleId == article.Id && p.Platform == "devto" && p.Status == "Published");
            
        if (devToStatus == null || string.IsNullOrEmpty(devToStatus.PublishedUrl))
        {
            Console.WriteLine($"[LinkedIn] ⚠️ Article not published to dev.to yet - skipping LinkedIn share");
            return false;
        }
        
        // Create or update status record
        publishStatus ??= new ArticlePublishStatus
        {
            ArticleId = article.Id,
            Platform = "linkedin",
            Status = "Pending"
        };
        
        // Generate AI clickbait description (with summary if available)
        Console.WriteLine($"[LinkedIn] 🎯 Generating clickbait post description...");
        string clickbaitPost = await GenerateClickbaitPostAsync(article, devToStatus.PublishedUrl, aiClient, config, summaryJson);
        
        // Check if we have an image to share (local file or HTTP URL)
        bool hasImage = !string.IsNullOrEmpty(article.ImageUrl);
        string? localImagePath = null;
        
        if (hasImage)
        {
            // Check if it's a local file or HTTP URL
            if (article.ImageUrl!.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                article.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // Download HTTP image to temp file
                Console.WriteLine($"[LinkedIn] 🌐 Downloading image from URL: {article.ImageUrl}");
                try
                {
                    using var httpClient = new HttpClient();
                    byte[] imageBytes = await httpClient.GetByteArrayAsync(article.ImageUrl);
                    
                    // Create temp file
                    localImagePath = Path.Combine(Path.GetTempPath(), $"linkedin_image_{Guid.NewGuid()}.png");
                    await File.WriteAllBytesAsync(localImagePath, imageBytes);
                    Console.WriteLine($"[LinkedIn] ✓ Downloaded to temp file: {localImagePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[LinkedIn] ⚠️ Failed to download image: {ex.Message}");
                    hasImage = false;
                }
            }
            else if (File.Exists(article.ImageUrl))
            {
                // Local file path
                localImagePath = article.ImageUrl;
                Console.WriteLine($"[LinkedIn] 📁 Using local image: {localImagePath}");
            }
            else
            {
                Console.WriteLine($"[LinkedIn] ⚠️ Image not found: {article.ImageUrl}");
                hasImage = false;
            }
        }
        
        Dictionary<string, object> body;
        
        if (hasImage && localImagePath != null)
        {
            Console.WriteLine($"[LinkedIn] 🖼️ Image ready, uploading and sharing as IMAGE post...");
            
            // Upload image and get asset URN
            string? assetUrn = await UploadImageAsync(localImagePath, accessToken, authorUrn);
            
            // Clean up temp file if we downloaded it
            if (article.ImageUrl!.StartsWith("http", StringComparison.OrdinalIgnoreCase) && 
                File.Exists(localImagePath))
            {
                try
                {
                    File.Delete(localImagePath);
                    Console.WriteLine($"[LinkedIn] 🗑️ Cleaned up temp file");
                }
                catch { /* Ignore cleanup errors */ }
            }
            
            if (assetUrn != null)
            {
                // Share with image
                body = BuildImageShareBody(authorUrn, clickbaitPost, assetUrn, article.Title, devToStatus.PublishedUrl);
                Console.WriteLine($"[LinkedIn] ✨ Sharing with IMAGE");
            }
            else
            {
                // Fallback to article share if image upload failed
                Console.WriteLine($"[LinkedIn] ⚠️ Image upload failed, falling back to ARTICLE share");
                body = BuildArticleShareBody(authorUrn, clickbaitPost, devToStatus.PublishedUrl, article.Title, article.Description);
            }
        }
        else
        {
            Console.WriteLine($"[LinkedIn] 📄 No image available, sharing as ARTICLE");
            body = BuildArticleShareBody(authorUrn, clickbaitPost, devToStatus.PublishedUrl, article.Title, article.Description);
        }
        
        // Attempt publishing with retries
        for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
        {
            try
            {
                publishStatus.AttemptCount = attempt;
                publishStatus.LastAttemptDate = DateTime.UtcNow;
                
                Console.WriteLine($"[LinkedIn] 🔄 Attempt {attempt}/{MAX_RETRIES}...");
                
                var response = await API_BASE_URL
                    .WithHeaders(new
                    {
                        Authorization = $"Bearer {accessToken}",
                        LinkedIn_Version = "202210",
                        X_Restli_Protocol_Version = "2.0.0"
                    })
                    .PostJsonAsync(body)
                    .ReceiveString();
                
                // Get the post ID from X-RestLi-Id header (would need to access response headers)
                // For now, we'll mark as published
                publishStatus.Status = "Published";
                publishStatus.PublishedUrl = devToStatus.PublishedUrl; // LinkedIn shares the dev.to URL
                publishStatus.PlatformArticleId = "published"; // LinkedIn doesn't return post ID in body
                publishStatus.PublishedDate = DateTime.UtcNow;
                publishStatus.LastError = null;
                
                if (publishStatus.Id == 0)
                    dbContext.ArticlePublishStatus.Add(publishStatus);
                
                await dbContext.SaveChangesAsync();
                
                Console.WriteLine($"[LinkedIn] ✅ Published: Shared {devToStatus.PublishedUrl}");
                return true;
            }
            catch (FlurlHttpException ex)
            {
                // Check if it's an auth error (don't retry)
                if (ex.StatusCode == 401)
                {
                    string error = "Unauthorized - check access token";
                    Console.WriteLine($"[LinkedIn] 🔒 {error}");
                    
                    publishStatus.Status = "Failed";
                    publishStatus.LastError = error;
                    
                    if (publishStatus.Id == 0)
                        dbContext.ArticlePublishStatus.Add(publishStatus);
                    
                    await dbContext.SaveChangesAsync();
                    return false;
                }
                
                // Other HTTP error - will retry
                string errorMsg = await ex.GetResponseStringAsync() ?? ex.Message;
                publishStatus.LastError = $"HTTP {ex.StatusCode}: {errorMsg}";
                
                Console.WriteLine($"[LinkedIn] ⚠️ Attempt {attempt} failed: {publishStatus.LastError}");
                
                if (attempt < MAX_RETRIES)
                {
                    await Task.Delay(2000); // Wait 2s before retry
                }
            }
            catch (Exception ex)
            {
                publishStatus.LastError = ex.Message;
                Console.WriteLine($"[LinkedIn] ❌ Attempt {attempt} error: {ex.Message}{(ex.InnerException is null ? string.Empty : $", inner exception: {ex.InnerException.Message}")}");
                
                if (attempt < MAX_RETRIES)
                {
                    await Task.Delay(2000);
                }
            }
        }
        
        // All retries failed
        publishStatus.Status = "Failed";
        
        if (publishStatus.Id == 0)
            dbContext.ArticlePublishStatus.Add(publishStatus);
        
        await dbContext.SaveChangesAsync();
        
        Console.WriteLine($"[LinkedIn] 💥 Failed after {MAX_RETRIES} attempts");
        return false;
    }
    
    /// <summary>
    /// Generate a clickbait LinkedIn post using AI with emojis, hooks, and engagement tactics
    /// </summary>
    private static async Task<string> GenerateClickbaitPostAsync(
        Article article, 
        string articleUrl, 
        TornadoApi? aiClient, 
        AppConfiguration? config,
        string? summaryJson = null)
    {
        if (aiClient == null)
        {
            // Fallback to simple format if no AI client provided
            return $"🚀 {article.Title}\n\n{article.Description}\n\n📖 Read more: {articleUrl}";
        }
        
        try
        {
            // Parse summary if available for enhanced context
            ArticleSummary? summary = null;
            if (!string.IsNullOrEmpty(summaryJson))
            {
                try
                {
                    summary = JsonSerializer.Deserialize<ArticleSummary>(summaryJson);
                    Console.WriteLine($"[LinkedIn] 📊 Using article summary for enhanced post generation");
                }
                catch
                {
                    Console.WriteLine($"[LinkedIn] ⚠️ Failed to parse summary, using article data only");
                }
            }
            
            // Build enhanced prompt with summary context
            string summaryContext = "";
            if (summary != null)
            {
                summaryContext = $"""
                                 
                                 **Article Analysis (Use this to craft a compelling post):**
                                 
                                 Executive Summary: {summary.ExecutiveSummary}
                                 
                                 Key Technical Points:
                                 {string.Join("\n", summary.KeyPoints.Select(p => $"- {p}"))}
                                 
                                 Target Audience: {summary.TargetAudience}
                                 Emotional Tone: {summary.EmotionalTone}
                                 
                                 Pre-crafted Hook (you can use or improve): {summary.SocialMediaHook}
                                 """;
            }
            
            string prompt = $"""
                            Generate a highly engaging LinkedIn post for the following article.
                            
                            Article Title: {article.Title}
                            Article Description: {article.Description}{summaryContext}
                            
                            REQUIREMENTS FOR MAXIMUM ENGAGEMENT:
                            
                            1. **Hook (First Line)**: Start with a provocative statement, shocking stat, or bold question
                               - Examples: "Here's why 90% of developers are doing X wrong..."
                                          "What if I told you that X could Y in half the time?"
                                          "🔥 This changed everything I thought I knew about X..."
                            
                            2. **Use Emojis Strategically**: 
                               - 3-5 relevant emojis throughout (not too many)
                               - Use: 🚀 💡 🔥 ⚡ 🎯 💪 🧠 ✨ 📈 👀 💻 🔧
                            
                            3. **Structure**:
                               - Opening hook (1 line)
                               - 2-3 sentences of value/insight
                               - Curiosity gap ("What I discovered will surprise you...")
                               - End with engaging question
                               - Link at the VERY END with "Read more:" or "Full story:"
                            
                            4. **Engagement Tactics**:
                               - Ask a question at the end to encourage comments
                               - Use "you" and "your" to make it personal
                               - Create FOMO (fear of missing out)
                               - Tease the outcome without revealing everything
                               - Use line breaks for readability
                            
                            5. **Tone**: Professional but energetic, like a successful developer sharing a breakthrough
                            
                            6. **Length**: 150-250 characters max (LinkedIn optimal length)
                            
                            7. **Link Placement**: Put the article URL at the VERY END with a call-to-action like:
                               "📖 Read the full breakdown: {articleUrl}"
                            
                            ANTI-PATTERNS TO AVOID:
                            - Don't use hashtags (looks spammy)
                            - Don't be overly promotional
                            - Don't give away everything in the post
                            - Don't use generic phrases like "Check this out"
                            
                            Generate ONLY the post text, nothing else. Include the article URL at the end.
                            """;

            // Get model from config or use default
            string modelName = config?.Models.LinkedInPost ?? "gpt-4o-mini";
            ChatModel model = new ChatModel(modelName);
            
            var conversation = aiClient.Chat.CreateConversation(new ChatRequest
            {
                Model = model
            });
            conversation.AppendSystemMessage("You are a LinkedIn engagement expert who creates viral posts for developers.");
            conversation.AppendUserInput(prompt);
            
            Console.WriteLine($"[LinkedIn] 🤖 Using model: {modelName}");
            
            var response = await conversation.GetResponse();
            string generatedPost = response.Trim();
            
            // Ensure URL is at the end if not already there
            if (!generatedPost.Contains(articleUrl))
            {
                generatedPost += $"\n\n📖 Read more: {articleUrl}";
            }
            
            Console.WriteLine($"[LinkedIn] ✨ Generated clickbait post ({generatedPost.Length} chars)");
            return generatedPost;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LinkedIn] ⚠️ AI generation failed: {ex.Message}, using fallback");
            return $"🚀 {article.Title}\n\n{article.Description}\n\nWhat's your take on this? 🤔\n\n📖 Read more: {articleUrl}";
        }
    }
    
    /// <summary>
    /// Upload an image to LinkedIn and get the asset URN
    /// </summary>
    private static async Task<string?> UploadImageAsync(string imagePath, string accessToken, string authorUrn)
    {
        try
        {
            Console.WriteLine($"[LinkedIn] 📤 Registering image upload...");
            
            // Step 1: Register the upload
            var registerBody = new Dictionary<string, object>
            {
                ["registerUploadRequest"] = new Dictionary<string, object>
                {
                    ["recipes"] = new[] { "urn:li:digitalmediaRecipe:feedshare-image" },
                    ["owner"] = authorUrn,
                    ["serviceRelationships"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["relationshipType"] = "OWNER",
                            ["identifier"] = "urn:li:userGeneratedContent"
                        }
                    }
                }
            };
            
            var registerResponse = await ASSETS_API_URL
                .WithHeaders(new
                {
                    Authorization = $"Bearer {accessToken}",
                    LinkedIn_Version = "202210",
                    X_Restli_Protocol_Version = "2.0.0"
                })
                .PostJsonAsync(registerBody)
                .ReceiveJson<JsonElement>();
            
            // Extract upload URL and asset URN
            string uploadUrl = registerResponse
                .GetProperty("value")
                .GetProperty("uploadMechanism")
                .GetProperty("com.linkedin.digitalmedia.uploading.MediaUploadHttpRequest")
                .GetProperty("uploadUrl")
                .GetString()!;
                
            string assetUrn = registerResponse
                .GetProperty("value")
                .GetProperty("asset")
                .GetString()!;
            
            Console.WriteLine($"[LinkedIn] 📤 Uploading image binary...");
            
            // Step 2: Upload the image binary
            byte[] imageData = await File.ReadAllBytesAsync(imagePath);
            
            var uploadResponse = await uploadUrl
                .WithHeader("Authorization", $"Bearer {accessToken}")
                .PutAsync(new ByteArrayContent(imageData));
            
            if (uploadResponse.StatusCode == 201 || uploadResponse.StatusCode == 200)
            {
                Console.WriteLine($"[LinkedIn] ✅ Image uploaded successfully: {assetUrn}");
                return assetUrn;
            }
            else
            {
                Console.WriteLine($"[LinkedIn] ⚠️ Image upload failed with status: {uploadResponse.StatusCode}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LinkedIn] ❌ Image upload error: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Build request body for sharing with an image
    /// </summary>
    private static Dictionary<string, object> BuildImageShareBody(
        string authorUrn, 
        string commentary, 
        string assetUrn, 
        string title,
        string articleUrl)
    {
        return new Dictionary<string, object>
        {
            ["author"] = authorUrn,
            ["lifecycleState"] = "PUBLISHED",
            ["specificContent"] = new Dictionary<string, object>
            {
                ["com.linkedin.ugc.ShareContent"] = new Dictionary<string, object>
                {
                    ["shareCommentary"] = new Dictionary<string, object>
                    {
                        ["text"] = commentary
                    },
                    ["shareMediaCategory"] = "IMAGE",
                    ["media"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["status"] = "READY",
                            ["description"] = new Dictionary<string, object>
                            {
                                ["text"] = $"From: {articleUrl}"
                            },
                            ["media"] = assetUrn,
                            ["title"] = new Dictionary<string, object>
                            {
                                ["text"] = title
                            }
                        }
                    }
                }
            },
            ["visibility"] = new Dictionary<string, object>
            {
                ["com.linkedin.ugc.MemberNetworkVisibility"] = "PUBLIC"
            }
        };
    }
    
    /// <summary>
    /// Build request body for sharing an article (no image)
    /// </summary>
    private static Dictionary<string, object> BuildArticleShareBody(
        string authorUrn,
        string commentary,
        string articleUrl,
        string title,
        string? description)
    {
        return new Dictionary<string, object>
        {
            ["author"] = authorUrn,
            ["lifecycleState"] = "PUBLISHED",
            ["specificContent"] = new Dictionary<string, object>
            {
                ["com.linkedin.ugc.ShareContent"] = new Dictionary<string, object>
                {
                    ["shareCommentary"] = new Dictionary<string, object>
                    {
                        ["text"] = commentary
                    },
                    ["shareMediaCategory"] = "ARTICLE",
                    ["media"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["status"] = "READY",
                            ["description"] = new Dictionary<string, object>
                            {
                                ["text"] = description ?? title
                            },
                            ["originalUrl"] = articleUrl,
                            ["title"] = new Dictionary<string, object>
                            {
                                ["text"] = title
                            }
                        }
                    }
                }
            },
            ["visibility"] = new Dictionary<string, object>
            {
                ["com.linkedin.ugc.MemberNetworkVisibility"] = "PUBLIC"
            }
        };
    }
}

