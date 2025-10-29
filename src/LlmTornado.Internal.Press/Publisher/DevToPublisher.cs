using System.Text.Json;
using Flurl.Http;
using LlmTornado.Internal.Press.Database;
using LlmTornado.Internal.Press.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace LlmTornado.Internal.Press.Publisher;

public static class DevToPublisher
{
    private const string API_BASE_URL = "https://dev.to/api";
    private const int MAX_RETRIES = 2;
    
    public static async Task<bool> PublishArticleAsync(
        Article article, 
        string apiKey,
        PressDbContext dbContext)
    {
        Console.WriteLine($"[DevTo] 📤 Publishing: {article.Title}");
        
        // Check if already published
        var publishStatus = await dbContext.ArticlePublishStatus
            .FirstOrDefaultAsync(p => p.ArticleId == article.Id && p.Platform == "devto");
            
        if (publishStatus?.Status == "Published")
        {
            Console.WriteLine($"[DevTo] ℹ️ Already published: {publishStatus.PublishedUrl}");
            return true;
        }
        
        // Create or update status record
        publishStatus ??= new ArticlePublishStatus
        {
            ArticleId = article.Id,
            Platform = "devto",
            Status = "Pending"
        };
        
        // Get main image (prefer devto variation)
        string? mainImage = null;
        if (!string.IsNullOrEmpty(article.ImageVariationsJson))
        {
            try
            {
                var variations = JsonSerializer.Deserialize<Dictionary<string, string>>(article.ImageVariationsJson);
                mainImage = variations?.GetValueOrDefault("devto") ?? variations?.GetValueOrDefault("featured-seo");
            }
            catch { }
        }
        mainImage ??= article.ImageUrl;
        
        // Prepare tags (max 4)
        string tags = article.Tags != null && article.Tags.Length > 0
            ? string.Join(",", article.Tags.Take(4))
            : "";
        
        // Build request body
        var body = new
        {
            article = new
            {
                title = article.Title,
                body_markdown = article.Body,
                published = true,
                description = article.Description,
                tags = tags,
                main_image = mainImage
            }
        };
        
        // Attempt publishing with retries
        for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
        {
            try
            {
                publishStatus.AttemptCount = attempt;
                publishStatus.LastAttemptDate = DateTime.UtcNow;
                
                Console.WriteLine($"[DevTo] 🔄 Attempt {attempt}/{MAX_RETRIES}...");
                
                var result = await $"{API_BASE_URL}/articles"
                    .WithHeaders(new
                    {
                        api_key = apiKey,
                        User_Agent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/141.0.0.0 Safari/537.36"
                    })
                    .PostJsonAsync(body)
                    .ReceiveJson<DevToArticleResponse>();
                
                publishStatus.Status = "Published";
                publishStatus.PublishedUrl = result?.url;
                publishStatus.PlatformArticleId = result?.id.ToString();
                publishStatus.PublishedDate = DateTime.UtcNow;
                publishStatus.LastError = null;
                
                if (publishStatus.Id == 0)
                    dbContext.ArticlePublishStatus.Add(publishStatus);
                
                await dbContext.SaveChangesAsync();
                
                Console.WriteLine($"[DevTo] ✅ Published: {publishStatus.PublishedUrl}");
                return true;
            }
            catch (FlurlHttpException ex)
            {
                // Check if it's an auth error (don't retry)
                if (ex.StatusCode == 401)
                {
                    string error = "Unauthorized - check API key";
                    Console.WriteLine($"[DevTo] 🔒 {error}");
                    
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
                
                Console.WriteLine($"[DevTo] ⚠️ Attempt {attempt} failed: {publishStatus.LastError}");
                
                if (attempt < MAX_RETRIES)
                {
                    await Task.Delay(2000); // Wait 2s before retry
                }
            }
            catch (Exception ex)
            {
                publishStatus.LastError = ex.Message;
                Console.WriteLine($"[DevTo] ❌ Attempt {attempt} error: {ex.Message}{(ex.InnerException is null ? string.Empty : $", inner exception: {ex.InnerException.Message}")}");
                
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
        
        Console.WriteLine($"[DevTo] 💥 Failed after {MAX_RETRIES} attempts");
        return false;
    }
    
    private class DevToArticleResponse
    {
        public int id { get; set; }
        public string? url { get; set; }
    }
}

