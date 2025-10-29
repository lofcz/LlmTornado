using Flurl.Http;
using LlmTornado.Internal.Press.Database;
using LlmTornado.Internal.Press.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace LlmTornado.Internal.Press.Publisher;

public static class LinkedInPublisher
{
    private const string API_BASE_URL = "https://api.linkedin.com/v2/ugcPosts";
    private const int MAX_RETRIES = 2;
    
    public static async Task<bool> PublishArticleAsync(
        Article article, 
        string accessToken,
        string authorUrn,
        PressDbContext dbContext)
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
        
        // Get dev.to URL - LinkedIn shares the dev.to article
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
        
        // Build request body - share the dev.to article
        var body = new Dictionary<string, object>
        {
            ["author"] = authorUrn,
            ["lifecycleState"] = "PUBLISHED",
            ["specificContent"] = new Dictionary<string, object>
            {
                ["com.linkedin.ugc.ShareContent"] = new Dictionary<string, object>
                {
                    ["shareCommentary"] = new Dictionary<string, object>
                    {
                        ["text"] = article.Description ?? article.Title
                    },
                    ["shareMediaCategory"] = "ARTICLE",
                    ["media"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["status"] = "READY",
                            ["description"] = new Dictionary<string, object>
                            {
                                ["text"] = article.Description ?? article.Title
                            },
                            ["originalUrl"] = devToStatus.PublishedUrl,
                            ["title"] = new Dictionary<string, object>
                            {
                                ["text"] = article.Title
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
}

