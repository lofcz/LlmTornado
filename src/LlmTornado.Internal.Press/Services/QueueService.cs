using LlmTornado.Internal.Press.Database;
using LlmTornado.Internal.Press.Database.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Services;

public class QueueService
{
    private readonly PressDbContext _context;

    public QueueService(PressDbContext context)
    {
        _context = context;
    }

    public async Task<ArticleQueue> AddToQueueAsync(string title, string ideaSummary, string[] tags, double relevance, int priority = 0)
    {
        ArticleQueue queueItem = new ArticleQueue
        {
            Title = title,
            IdeaSummary = ideaSummary,
            Tags = JsonConvert.SerializeObject(tags),
            EstimatedRelevance = relevance,
            Priority = priority,
            Status = QueueStatus.Pending,
            CreatedDate = DateTime.UtcNow
        };

        _context.ArticleQueue.Add(queueItem);
        await _context.SaveChangesAsync();

        return queueItem;
    }

    public async Task<ArticleQueue?> GetNextPendingAsync()
    {
        // Get items that are either pending, or stuck in progress with failed attempts < 3
        return await _context.ArticleQueue
            .Where(q => q.Status == QueueStatus.Pending || 
                       (q.Status == QueueStatus.InProgress && q.AttemptCount < 3))
            .OrderByDescending(q => q.Priority)
            .ThenBy(q => q.CreatedDate)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ArticleQueue>> GetPendingQueueAsync(int count = 10)
    {
        return await _context.ArticleQueue
            .Where(q => q.Status == QueueStatus.Pending)
            .OrderByDescending(q => q.Priority)
            .ThenBy(q => q.CreatedDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task UpdateStatusAsync(int queueId, string status, string? errorMessage = null)
    {
        ArticleQueue? queueItem = await _context.ArticleQueue.FindAsync(queueId);
        if (queueItem != null)
        {
            queueItem.Status = status;
            queueItem.ErrorMessage = errorMessage;
            
            if (status == QueueStatus.InProgress)
            {
                queueItem.AttemptCount++;
            }
            else if (status == QueueStatus.Completed)
            {
                queueItem.ProcessedDate = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task LinkArticleAsync(int queueId, int articleId)
    {
        ArticleQueue? queueItem = await _context.ArticleQueue.FindAsync(queueId);
        if (queueItem != null)
        {
            queueItem.ArticleId = articleId;
            queueItem.Status = QueueStatus.Completed;
            queueItem.ProcessedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetPendingCountAsync()
    {
        return await _context.ArticleQueue.CountAsync(q => q.Status == QueueStatus.Pending);
    }

    public async Task<int> GetCompletedCountAsync()
    {
        return await _context.ArticleQueue.CountAsync(q => q.Status == QueueStatus.Completed);
    }

    public async Task<int> GetFailedCountAsync()
    {
        return await _context.ArticleQueue.CountAsync(q => q.Status == QueueStatus.Failed);
    }

    public async Task<List<ArticleQueue>> GetRecentlyProcessedAsync(int count = 10)
    {
        return await _context.ArticleQueue
            .Where(q => q.ProcessedDate != null)
            .OrderByDescending(q => q.ProcessedDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task ClearFailedAsync()
    {
        List<ArticleQueue> failed = await _context.ArticleQueue
            .Where(q => q.Status == QueueStatus.Failed)
            .ToListAsync();

        _context.ArticleQueue.RemoveRange(failed);
        await _context.SaveChangesAsync();
    }

    public async Task RetryFailedAsync()
    {
        List<ArticleQueue> failed = await _context.ArticleQueue
            .Where(q => q.Status == QueueStatus.Failed && q.AttemptCount < 3)
            .ToListAsync();

        foreach (ArticleQueue item in failed)
        {
            item.Status = QueueStatus.Pending;
            item.ErrorMessage = null;
        }

        await _context.SaveChangesAsync();
    }
}

