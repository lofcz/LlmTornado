using LlmTornado.Code;
using LlmTornado.Internal.Press.Configuration;
using LlmTornado.Internal.Press.Database;
using LlmTornado.Internal.Press.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using LlmTornado.Agents;
using LlmTornado.Agents.DataModels;
using LlmTornado.Chat.Models;
using LlmTornado.Common;
using LlmTornado.Internal.Press.Database.Models;
using LlmTornado.Internal.Press.Export;
using LlmTornado.Internal.Press.Publisher;
using LlmTornado.Mcp;

namespace LlmTornado.Internal.Press;

class Program
{
    private static AppConfiguration config;
    private static bool _databaseInitialized = false;
    private static readonly object _dbInitLock = new object();
    
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== LlmTornado Internal Press - Journalist Agent ===\n");
        
        try
        {
            // Load configuration
            config = AppConfiguration.Load();
            Console.WriteLine($"Objective: {config.Objective}\n");
            
            // Initialize database (basic setup)
            await DatabaseInitializer.InitializeAsync();

            // Create services
            PressDbContext dbContext = new PressDbContext();
            
            TornadoApi client = CreateTornadoClient(true);
            ArticleGenerationService service = new ArticleGenerationService(client, config, dbContext);
            
            if (args.Length == 0)
            {
                // Interactive REPL mode
                await RunInteractiveMode(config, dbContext, service);
            }
            else
            {
                // Single command mode
                await ExecuteCommand(args, config, dbContext, service);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error: {ex.Message}");
            Console.WriteLine($"\nStack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }

    static async Task RunInteractiveMode(AppConfiguration config, PressDbContext dbContext, ArticleGenerationService service)
    {
        Console.WriteLine("Interactive REPL mode. Type 'help' for commands, 'exit' to quit.\n");

        while (true)
        {
            Console.Write("press> ");
            string? input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            string trimmedInput = input.Trim();

            if (trimmedInput.ToLower() == "exit" || trimmedInput.ToLower() == "quit")
            {
                Console.WriteLine("Goodbye!");
                break;
            }

            if (trimmedInput.ToLower() == "clear" || trimmedInput.ToLower() == "cls")
            {
                Console.Clear();
                Console.WriteLine("=== LlmTornado Internal Press - Journalist Agent ===\n");
                continue;
            }

            // Parse command
            string[] parts = trimmedInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            try
            {
                await ExecuteCommand(parts, config, dbContext, service);
                Console.WriteLine(); // Add spacing after command execution
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}\n");
            }
        }
    }

    static async Task ExecuteCommand(string[] args, AppConfiguration config, PressDbContext dbContext, ArticleGenerationService service)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return;
        }

        string command = args[0].ToLower();

        switch (command)
        {
            case "generate":
            case "gen":
                await HandleGenerateCommand(args, service);
                break;

            case "seed-queue":
            case "seed":
                await HandleSeedQueueCommand(args, service);
                break;

            case "status":
            case "stat":
                await HandleStatusCommand(service);
                break;

            case "export-all":
            case "export":
                await HandleExportAllCommand(config, dbContext);
                break;

            case "list-articles":
            case "list":
            case "articles":
                await HandleListArticlesCommand(dbContext);
                break;

            case "publish":
                await HandlePublishCommand(args, config, dbContext);
                break;
                
            case "publish-all":
                await HandlePublishAllCommand(config);
                break;

            case "reset-db":
            case "reset":
                await HandleResetDbCommand();
                break;

            case "migrate-db":
            case "migrate":
                await HandleMigrateDbCommand();
                break;

            case "check-db":
            case "checkdb":
                await HandleCheckDbCommand();
                break;

            case "clear-queue":
            case "clearq":
                await HandleClearQueueCommand(dbContext);
                break;

            case "show-queue":
            case "queue":
            case "showq":
                await HandleShowQueueCommand(dbContext);
                break;

            case "drop-queue":
            case "dropq":
                await HandleDropQueueCommand(args, dbContext);
                break;

            case "drop-all":
            case "dropall":
                await HandleDropAllQueueCommand(dbContext);
                break;

            case "help":
            case "?":
                ShowHelp();
                break;

            default:
                Console.WriteLine($"Unknown command: {command}");
                ShowHelp();
                break;
        }
    }

    static void ShowHelp()
    {
        Console.WriteLine("Available Commands:");
        Console.WriteLine();
        Console.WriteLine("  generate <count>      Generate articles from queue (alias: gen)");
        Console.WriteLine("  seed-queue <count>    Seed the queue with article ideas (alias: seed)");
        Console.WriteLine("  show-queue [count]    Display queue items with details (alias: queue, showq)");
        Console.WriteLine("  drop-queue <id>       Remove specific queue item by ID (alias: dropq)");
        Console.WriteLine("  drop-all              Remove ALL queue items - DELETES EVERYTHING (alias: dropall)");
        Console.WriteLine("  clear-queue           Clear all pending items from queue (alias: clearq)");
        Console.WriteLine("  status                Show queue and article statistics (alias: stat)");
        Console.WriteLine("  list-articles         List all articles with publish status (alias: list, articles)");
        Console.WriteLine("  export-all            Export all articles to markdown/JSON (alias: export)");
        Console.WriteLine("  publish <id> [platforms]  Publish article (platforms: devto,linkedin,medium)");
        Console.WriteLine("  publish-all           Publish all unpublished articles");
        Console.WriteLine("  migrate-db            Apply pending database migrations (alias: migrate)");
        Console.WriteLine("  check-db              Check database status and pending migrations (alias: checkdb)");
        Console.WriteLine("  reset-db              Reset the database - DELETES ALL DATA (alias: reset)");
        Console.WriteLine("  help                  Show this help message (alias: ?)");
        Console.WriteLine("  clear                 Clear the screen (alias: cls)");
        Console.WriteLine("  exit                  Exit the application (alias: quit)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  generate 5            Generate 5 articles");
        Console.WriteLine("  seed 10               Seed queue with 10 ideas");
        Console.WriteLine("  list-articles         List all articles");
        Console.WriteLine("  publish 1             Publish article #1 to all enabled platforms");
        Console.WriteLine("  publish 1 devto       Publish article #1 to dev.to only");
        Console.WriteLine("  publish 1 devto,medium  Publish article #1 to dev.to and Medium");
        Console.WriteLine("  show-queue            Show all pending queue items");
        Console.WriteLine("  drop-queue 3          Remove queue item with ID 3");
        Console.WriteLine("  status                Check current status");
        Console.WriteLine();
        Console.WriteLine("Command-line mode:");
        Console.WriteLine("  dotnet run -- generate 5");
        Console.WriteLine("  dotnet run -- seed-queue 10");
        Console.WriteLine("  dotnet run -- show-queue");
    }

    /// <summary>
    /// Ensures database is fully initialized with migrations applied.
    /// Only runs once per application lifetime.
    /// </summary>
    static async Task EnsureDatabaseReadyAsync()
    {
        // Fast path - already initialized
        if (_databaseInitialized)
            return;

        // Slow path - need to initialize (lock prevents duplicate work)
        lock (_dbInitLock)
        {
            // Double-check after acquiring lockc
            if (_databaseInitialized)
                return;
        }

        Console.WriteLine("Verifying database is up to date...");
        
        try
        {
            await using PressDbContext context = new PressDbContext();
            
            // Check for pending migrations
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            
            if (pendingMigrations.Any())
            {
                Console.WriteLine($"  Found {pendingMigrations.Count()} pending migration(s)");
                Console.WriteLine("  Applying migrations...");
                
                await context.Database.MigrateAsync();
                Console.WriteLine("  ✓ Migrations applied successfully");
            }
            else
            {
                Console.WriteLine("  ✓ Database is up to date");
            }
            
            _databaseInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Database check failed: {ex.Message}");
            Console.WriteLine("  The database may need to be reset. Use 'reset-db' command.");
            throw;
        }
    }

    static async Task HandleGenerateCommand(string[] args, ArticleGenerationService service)
    {
        // Ensure database is ready before any operations
        await EnsureDatabaseReadyAsync();
        
        int count = 1;
        if (args.Length > 1 && int.TryParse(args[1], out int parsedCount))
        {
            count = parsedCount;
        }

        Console.WriteLine($"\nGenerating {count} article(s)...\n");
        
        List<Article> articles = await service.GenerateArticlesAsync(count);
        
        Console.WriteLine($"\n=== Generation Complete ===");
        Console.WriteLine($"Successfully generated: {articles.Count} articles");
    }

    static async Task HandleSeedQueueCommand(string[] args, ArticleGenerationService service)
    {
        // Ensure database is ready before any operations
        await EnsureDatabaseReadyAsync();
        
        int count = 5;
        if (args.Length > 1 && int.TryParse(args[1], out int parsedCount))
        {
            count = parsedCount;
        }

        int added = await service.SeedQueueAsync(count);
        
        Console.WriteLine($"\n=== Queue Seeding Complete ===");
        Console.WriteLine($"Added {added} ideas to queue");
    }

    static async Task HandleStatusCommand(ArticleGenerationService service)
    {
        Console.WriteLine("=== System Status ===\n");
        
        QueueStats stats = await service.GetQueueStatsAsync();
        
        Console.WriteLine($"Queue Status:");
        Console.WriteLine($"  Pending:   {stats.PendingCount}");
        Console.WriteLine($"  Completed: {stats.CompletedCount}");
        Console.WriteLine($"  Failed:    {stats.FailedCount}");
        Console.WriteLine();
        Console.WriteLine($"Total Articles Generated: {stats.TotalArticles}");
    }

    static async Task HandleExportAllCommand(AppConfiguration config, PressDbContext dbContext)
    {
        Console.WriteLine("Exporting all articles...\n");
        
        List<Article> articles = await dbContext.Articles.ToListAsync();
        
        if (articles.Count == 0)
        {
            Console.WriteLine("No articles to export");
            return;
        }

        MarkdownExporter markdownExporter = new Export.MarkdownExporter(config.Output.Directory);
        JsonExporter jsonExporter = new Export.JsonExporter(config.Output.Directory, config.ImageVariations);

        int exported = 0;
        foreach (Article article in articles)
        {
            try
            {
                if (config.Output.ExportMarkdown)
                {
                    await markdownExporter.ExportArticleAsync(article);
                }
                
                if (config.Output.ExportJson)
                {
                    await jsonExporter.ExportArticleAsync(article);
                }
                
                exported++;
                Console.WriteLine($"✓ Exported: {article.Title}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Failed to export {article.Title}: {ex.Message}");
            }
        }

        Console.WriteLine($"\n=== Export Complete ===");
        Console.WriteLine($"Exported {exported}/{articles.Count} articles");
    }

    static async Task HandleListArticlesCommand(PressDbContext dbContext)
    {
        Console.WriteLine("=== Articles ===\n");
        
        var articles = await dbContext.Articles
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();
        
        if (articles.Count == 0)
        {
            Console.WriteLine("No articles found. Generate some with 'generate <count>'\n");
            return;
        }
        
        // Get all publish statuses
        var publishStatuses = await dbContext.ArticlePublishStatus
            .Where(ps => ps.Status == "Published")
            .ToListAsync();
        
        // Print header
        Console.WriteLine("┌──────┬────────────────────────────────────────────┬─────────────────┬──────┬──────────┬──────────┬────────┐");
        Console.WriteLine("│  ID  │ Title                                      │ Created         │ Words│  DevTo   │ LinkedIn │ Medium │");
        Console.WriteLine("├──────┼────────────────────────────────────────────┼─────────────────┼──────┼──────────┼──────────┼────────┤");
        
        foreach (var article in articles)
        {
            string title = Truncate(article.Title, 42);
            string created = article.CreatedDate.ToString("yyyy-MM-dd HH:mm");
            string words = article.WordCount.ToString().PadLeft(5);
            
            // Check publish status for each platform
            var devToStatus = publishStatuses.FirstOrDefault(ps => ps.ArticleId == article.Id && ps.Platform == "devto");
            var linkedInStatus = publishStatuses.FirstOrDefault(ps => ps.ArticleId == article.Id && ps.Platform == "linkedin");
            var mediumStatus = publishStatuses.FirstOrDefault(ps => ps.ArticleId == article.Id && ps.Platform == "medium");
            
            string devToIcon = devToStatus != null ? "\x1b[32m✓\x1b[0m" : "\x1b[90m-\x1b[0m";
            string linkedInIcon = linkedInStatus != null ? "\x1b[32m✓\x1b[0m" : "\x1b[90m-\x1b[0m";
            string mediumIcon = mediumStatus != null ? "\x1b[32m✓\x1b[0m" : "\x1b[90m-\x1b[0m";
            
            Console.WriteLine($"│ {article.Id,4} │ {title,-42} │ {created,-15} │ {words} │    {devToIcon}     │    {linkedInIcon}     │   {mediumIcon}    │");
        }
        
        Console.WriteLine("└──────┴────────────────────────────────────────────┴─────────────────┴──────┴──────────┴──────────┴────────┘");
        Console.WriteLine($"\nTotal: {articles.Count} article(s)");
        Console.WriteLine("\nLegend: \x1b[32m✓\x1b[0m = Published  \x1b[90m-\x1b[0m = Not published\n");
    }

    static async Task HandlePublishCommand(string[] args, AppConfiguration config, PressDbContext dbContext)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: publish <article-id> [platforms]");
            Console.WriteLine("Platforms: devto, linkedin, medium (comma-separated)");
            Console.WriteLine("Example: publish 1 devto,medium");
            return;
        }
        
        if (!int.TryParse(args[1], out int articleId))
        {
            Console.WriteLine("Invalid article ID");
            return;
        }
        
        Article? article = await dbContext.Articles.FindAsync(articleId);
        
        if (article == null)
        {
            Console.WriteLine($"Article {articleId} not found");
            return;
        }
        
        // Parse platform filter if provided
        HashSet<string> targetPlatforms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (args.Length >= 3)
        {
            var platforms = args[2].Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var platform in platforms)
            {
                targetPlatforms.Add(platform.Trim().ToLower());
            }
            Console.WriteLine($"Publishing article {articleId} to: {string.Join(", ", targetPlatforms)}");
        }
        else
        {
            Console.WriteLine($"Publishing article {articleId} to enabled platforms: {article.Title}");
        }
        
        bool publishedAny = false;
        
        // Publish to dev.to if enabled (or explicitly requested)
        bool shouldPublishDevTo = targetPlatforms.Count == 0 
            ? config.Publishing?.DevTo?.Enabled == true 
            : targetPlatforms.Contains("devto");
            
        if (shouldPublishDevTo)
        {
            if (!string.IsNullOrEmpty(config.ApiKeys.DevTo))
            {
                bool success = await DevToPublisher.PublishArticleAsync(article, config.ApiKeys.DevTo, dbContext);
                publishedAny = publishedAny || success;
            }
            else
            {
                Console.WriteLine("⚠ dev.to API key not configured");
            }
        }
        
        // Publish to LinkedIn if enabled (or explicitly requested)
        bool shouldPublishLinkedIn = targetPlatforms.Count == 0 
            ? config.Publishing?.LinkedIn?.Enabled == true 
            : targetPlatforms.Contains("linkedin");
            
        if (shouldPublishLinkedIn)
        {
            if (!string.IsNullOrEmpty(config.ApiKeys.LinkedIn) && 
                !string.IsNullOrEmpty(config.Publishing?.LinkedIn?.AuthorUrn))
            {
                bool success = await LinkedInPublisher.PublishArticleAsync(
                    article, 
                    config.ApiKeys.LinkedIn, 
                    config.Publishing.LinkedIn.AuthorUrn, 
                    dbContext);
                publishedAny = publishedAny || success;
            }
            else
            {
                Console.WriteLine("⚠ LinkedIn access token or authorUrn not configured");
            }
        }
        
        // Publish to Medium if enabled (or explicitly requested)
        bool shouldPublishMedium = targetPlatforms.Count == 0 
            ? config.Publishing?.Medium?.Enabled == true 
            : targetPlatforms.Contains("medium");
            
        if (shouldPublishMedium)
        {
            if (!string.IsNullOrEmpty(config.ApiKeys.Medium?.CookieSid) && 
                !string.IsNullOrEmpty(config.ApiKeys.Medium?.CookieUid))
            {
                bool success = await Publisher.MediumPublisher.PublishArticleAsync(
                    article,
                    config.ApiKeys.Medium.CookieSid,
                    config.ApiKeys.Medium.CookieUid,
                    config.Publishing?.Medium?.Headless ?? false,
                    config.Publishing?.Medium?.DailyPostLimit ?? 3,
                    dbContext);
                publishedAny = publishedAny || success;
            }
            else
            {
                Console.WriteLine("⚠ Medium cookies not configured");
            }
        }
        
        if (publishedAny)
        {
            Console.WriteLine("\n✓ Publish command completed successfully");
        }
        else
        {
            Console.WriteLine("\n⚠ No platforms were published to");
        }
    }

    static async Task HandlePublishAllCommand(AppConfiguration config)
    {
        using PressDbContext context = new PressDbContext();
        
        // Get articles that are published but not yet pushed to platforms
        var articles = await context.Articles
            .Where(a => a.Status == "Published")
            .ToListAsync();
        
        if (articles.Count == 0)
        {
            Console.WriteLine("No published articles to push");
            return;
        }
        
        Console.WriteLine($"Publishing {articles.Count} article(s)...");
        
        int successCount = 0;
        
        foreach (var article in articles)
        {
            // Check if already published to dev.to
            bool alreadyPublished = await context.ArticlePublishStatus
                .AnyAsync(p => p.ArticleId == article.Id && p.Platform == "devto" && p.Status == "Published");
            
            if (alreadyPublished)
            {
                Console.WriteLine($"  [{article.Id}] Already published: {article.Title}");
                continue;
            }
            
            Console.WriteLine($"  [{article.Id}] Publishing: {article.Title}");
            
            // Publish to dev.to
            if (config.Publishing?.DevTo?.Enabled == true && !string.IsNullOrEmpty(config.ApiKeys.DevTo))
            {
                bool success = await DevToPublisher.PublishArticleAsync(article, config.ApiKeys.DevTo, context);
                if (success) successCount++;
            }
            
            // Publish to LinkedIn
            if (config.Publishing?.LinkedIn?.Enabled == true && 
                !string.IsNullOrEmpty(config.ApiKeys.LinkedIn) &&
                !string.IsNullOrEmpty(config.Publishing.LinkedIn.AuthorUrn))
            {
                await LinkedInPublisher.PublishArticleAsync(
                    article, 
                    config.ApiKeys.LinkedIn, 
                    config.Publishing.LinkedIn.AuthorUrn, 
                    context);
            }
        }
        
        Console.WriteLine($"✓ Published {successCount}/{articles.Count} articles");
    }

    static async Task HandleClearQueueCommand(PressDbContext dbContext)
    {
        Console.Write("Clear queue? This will remove all pending queue items (y/n): ");
        string? confirmation = Console.ReadLine();

        if (confirmation?.ToLower() != "y" && confirmation?.ToLower() != "yes")
        {
            Console.WriteLine("✓ Queue clear cancelled");
            return;
        }

        // Get count before clearing
        int pendingCount = await dbContext.ArticleQueue
            .Where(q => q.Status == Database.Models.QueueStatus.Pending || 
                       q.Status == Database.Models.QueueStatus.InProgress)
            .CountAsync();

        if (pendingCount == 0)
        {
            Console.WriteLine("Queue is already empty");
            return;
        }

        // Remove all pending and in-progress items
        List<ArticleQueue> itemsToRemove = await dbContext.ArticleQueue
            .Where(q => q.Status == Database.Models.QueueStatus.Pending || 
                       q.Status == Database.Models.QueueStatus.InProgress)
            .ToListAsync();

        dbContext.ArticleQueue.RemoveRange(itemsToRemove);
        await dbContext.SaveChangesAsync();

        Console.WriteLine($"✓ Cleared {pendingCount} item(s) from queue");
        
        // Show what's left
        int completedCount = await dbContext.ArticleQueue
            .Where(q => q.Status == Database.Models.QueueStatus.Completed)
            .CountAsync();
        int failedCount = await dbContext.ArticleQueue
            .Where(q => q.Status == Database.Models.QueueStatus.Failed)
            .CountAsync();

        if (completedCount > 0 || failedCount > 0)
        {
            Console.WriteLine($"   Kept: {completedCount} completed, {failedCount} failed");
        }
    }

    static async Task HandleShowQueueCommand(PressDbContext dbContext)
    {
        // Parse optional count parameter (default: show all)
        int count = 0; // 0 means all
        
        // This would need to be passed from ExecuteCommand, but for now we'll show all
        Console.WriteLine("=== Article Queue ===\n");

        // Get all queue items ordered by priority and creation date
        List<ArticleQueue> queueItems = await dbContext.ArticleQueue
            .OrderByDescending(q => q.Priority)
            .ThenBy(q => q.CreatedDate)
            .ToListAsync();

        if (queueItems.Count == 0)
        {
            Console.WriteLine("Queue is empty. Use 'seed-queue' to add article ideas.\n");
            return;
        }

        // Group by status
        List<ArticleQueue> pending = queueItems.Where(q => q.Status == Database.Models.QueueStatus.Pending).ToList();
        List<ArticleQueue> inProgress = queueItems.Where(q => q.Status == Database.Models.QueueStatus.InProgress).ToList();
        List<ArticleQueue> completed = queueItems.Where(q => q.Status == Database.Models.QueueStatus.Completed).ToList();
        List<ArticleQueue> failed = queueItems.Where(q => q.Status == Database.Models.QueueStatus.Failed).ToList();

        // Show pending items (most important)
        if (pending.Count > 0)
        {
            Console.WriteLine($"📋 PENDING ({pending.Count}):");
            foreach (ArticleQueue item in pending)
            {
                string tags = string.IsNullOrEmpty(item.Tags) ? "" : $" [{item.Tags.Trim('[', ']').Replace("\"", "")}]";
                Console.WriteLine($"  #{item.Id} │ Priority: {item.Priority} │ Relevance: {item.EstimatedRelevance:F2}");
                Console.WriteLine($"       │ {Truncate(item.Title, 70)}");
                Console.WriteLine($"       │ {Truncate(item.IdeaSummary, 70)}{tags}");
                Console.WriteLine();
            }
        }

        // Show in-progress items
        if (inProgress.Count > 0)
        {
            Console.WriteLine($"⏳ IN PROGRESS ({inProgress.Count}):");
            foreach (ArticleQueue item in inProgress)
            {
                Console.WriteLine($"  #{item.Id} │ {Truncate(item.Title, 70)}");
                Console.WriteLine($"       │ Attempts: {item.AttemptCount}");
                Console.WriteLine();
            }
        }

        // Show recently completed (last 5)
        if (completed.Count > 0)
        {
            Console.WriteLine($"✅ COMPLETED ({completed.Count} total, showing last 5):");
            foreach (ArticleQueue item in completed.TakeLast(5))
            {
                string processedDate = item.ProcessedDate?.ToString("yyyy-MM-dd HH:mm") ?? "Unknown";
                Console.WriteLine($"  #{item.Id} │ {Truncate(item.Title, 50)} │ {processedDate}");
            }
            Console.WriteLine();
        }

        // Show failed items
        if (failed.Count > 0)
        {
            Console.WriteLine($"❌ FAILED ({failed.Count}):");
            foreach (ArticleQueue item in failed)
            {
                string error = string.IsNullOrEmpty(item.ErrorMessage) ? "Unknown" : Truncate(item.ErrorMessage, 30);
                Console.WriteLine($"  #{item.Id} │ {Truncate(item.Title, 50)} │ {error}");
            }
            Console.WriteLine();
        }

        // Summary
        Console.WriteLine($"=== Summary ===");
        Console.WriteLine($"Pending: {pending.Count} │ In Progress: {inProgress.Count} │ Completed: {completed.Count} │ Failed: {failed.Count}");
        Console.WriteLine();
    }

    static async Task HandleDropQueueCommand(string[] args, PressDbContext dbContext)
    {
        if (args.Length < 2 || !int.TryParse(args[1], out int queueId))
        {
            Console.WriteLine("Usage: drop-queue <id>");
            Console.WriteLine("Example: drop-queue 5");
            Console.WriteLine();
            Console.WriteLine("To see queue IDs, use 'show-queue' command first.");
            return;
        }

        // Find the queue item
        ArticleQueue? queueItem = await dbContext.ArticleQueue
            .FirstOrDefaultAsync(q => q.Id == queueId);

        if (queueItem == null)
        {
            Console.WriteLine($"❌ Queue item #{queueId} not found");
            return;
        }

        // Show what we're about to delete
        Console.WriteLine($"\n📋 Queue Item #{queueItem.Id}:");
        Console.WriteLine($"   Title: {queueItem.Title}");
        Console.WriteLine($"   Status: {queueItem.Status}");
        Console.WriteLine($"   Priority: {queueItem.Priority}");
        Console.WriteLine();

        Console.Write("Remove this queue item? (y/n): ");
        string? confirmation = Console.ReadLine();

        if (confirmation?.ToLower() != "y" && confirmation?.ToLower() != "yes")
        {
            Console.WriteLine("✓ Cancelled");
            return;
        }

        // Remove the item
        dbContext.ArticleQueue.Remove(queueItem);
        await dbContext.SaveChangesAsync();

        Console.WriteLine($"✓ Removed queue item #{queueId} from queue");
        
        // Show remaining queue stats
        int remainingPending = await dbContext.ArticleQueue
            .CountAsync(q => q.Status == Database.Models.QueueStatus.Pending);
        Console.WriteLine($"   Remaining pending items: {remainingPending}");
    }

    static async Task HandleDropAllQueueCommand(PressDbContext dbContext)
    {
        // Get all queue items count
        int totalCount = await dbContext.ArticleQueue.CountAsync();

        if (totalCount == 0)
        {
            Console.WriteLine("Queue is already empty");
            return;
        }

        // Show breakdown by status
        int pending = await dbContext.ArticleQueue
            .CountAsync(q => q.Status == Database.Models.QueueStatus.Pending);
        int inProgress = await dbContext.ArticleQueue
            .CountAsync(q => q.Status == Database.Models.QueueStatus.InProgress);
        int completed = await dbContext.ArticleQueue
            .CountAsync(q => q.Status == Database.Models.QueueStatus.Completed);
        int failed = await dbContext.ArticleQueue
            .CountAsync(q => q.Status == Database.Models.QueueStatus.Failed);

        Console.WriteLine($"\n⚠️  WARNING: This will DELETE ALL {totalCount} queue items!");
        Console.WriteLine($"\n   Breakdown:");
        Console.WriteLine($"   - Pending: {pending}");
        Console.WriteLine($"   - In Progress: {inProgress}");
        Console.WriteLine($"   - Completed: {completed}");
        Console.WriteLine($"   - Failed: {failed}");
        Console.WriteLine();
        Console.Write("Type 'DELETE ALL' to confirm: ");
        string? confirmation = Console.ReadLine();

        if (confirmation != "DELETE ALL")
        {
            Console.WriteLine("✓ Cancelled - no items were removed");
            return;
        }

        // Remove ALL queue items
        List<ArticleQueue> allItems = await dbContext.ArticleQueue.ToListAsync();
        dbContext.ArticleQueue.RemoveRange(allItems);
        await dbContext.SaveChangesAsync();

        Console.WriteLine($"✓ Deleted all {totalCount} queue items");
        Console.WriteLine("   Queue is now empty");
    }

    static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return "";
        
        if (text.Length <= maxLength)
            return text;
        
        return text.Substring(0, maxLength - 3) + "...";
    }

    static async Task HandleMigrateDbCommand()
    {
        Console.WriteLine("=== Database Migration ===\n");
        
        try
        {
            Console.WriteLine("Checking for pending migrations...");
            await DatabaseInitializer.MigrateAsync();
            Console.WriteLine("\n✓ Database migrations applied successfully");
            Console.WriteLine("   Database is now up to date");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Migration failed: {ex.Message}");
            Console.WriteLine("\nTIP: If migrations don't exist, you may need to:");
            Console.WriteLine("  1. Delete press.db and let it recreate (use 'reset-db')");
            Console.WriteLine("  2. Or generate migrations using: dotnet ef migrations add <MigrationName>");
        }
    }

    static async Task HandleCheckDbCommand()
    {
        Console.WriteLine("=== Database Status ===\n");
        
        try
        {
            using PressDbContext context = new PressDbContext();
            
            // Check if database exists and can connect
            bool canConnect = await context.Database.CanConnectAsync();
            string dbPath = DatabaseInitializer.GetDatabasePath("Data Source=press.db");
            
            Console.WriteLine($"Database File: {dbPath}");
            Console.WriteLine($"Can Connect: {(canConnect ? "✓ Yes" : "✗ No")}");
            
            if (canConnect)
            {
                // Check for pending migrations
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
                
                Console.WriteLine($"\nApplied Migrations: {appliedMigrations.Count()}");
                if (appliedMigrations.Any())
                {
                    foreach (string migration in appliedMigrations)
                    {
                        Console.WriteLine($"  ✓ {migration}");
                    }
                }
                else
                {
                    Console.WriteLine("  (none - using EnsureCreated)");
                }
                
                Console.WriteLine($"\nPending Migrations: {pendingMigrations.Count()}");
                if (pendingMigrations.Any())
                {
                    foreach (string migration in pendingMigrations)
                    {
                        Console.WriteLine($"  ⚠ {migration}");
                    }
                    Console.WriteLine("\n💡 Run 'migrate-db' to apply pending migrations");
                }
                else
                {
                    Console.WriteLine("  (none - database is up to date)");
                }
                
                // Show table info
                int articleCount = await context.Articles.CountAsync();
                int queueCount = await context.ArticleQueue.CountAsync();
                
                Console.WriteLine($"\nDatabase Contents:");
                Console.WriteLine($"  Articles: {articleCount}");
                Console.WriteLine($"  Queue Items: {queueCount}");
            }
            else
            {
                Console.WriteLine("\n⚠ Database does not exist or cannot connect");
                Console.WriteLine("   It will be created automatically on first use");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ Error checking database: {ex.Message}");
        }
    }

    static async Task HandleResetDbCommand()
    {
        Console.WriteLine("⚠️  WARNING: This will DELETE ALL DATA from the database!");
        Console.Write("Type 'yes' to confirm: ");
        string? confirmation = Console.ReadLine();

        if (confirmation?.ToLower() != "yes")
        {
            Console.WriteLine("✓ Database reset cancelled");
            return;
        }

        Console.WriteLine("Resetting database...");
        await DatabaseInitializer.ResetDatabaseAsync();
        
        // Reset the initialization flag so next operation will re-check
        lock (_dbInitLock)
        {
            _databaseInitialized = false;
        }
        
        Console.WriteLine("✓ Database has been reset");
        Console.WriteLine("  Next database operation will recreate the schema");
    }

    public static TornadoApi CreateTornadoClient(bool checkConfig = false)
    {
        List<ProviderAuthentication> authList = [];
        
        // Validate and add API keys
        if (!string.IsNullOrEmpty(config.ApiKeys.OpenAi) && 
            !config.ApiKeys.OpenAi.Equals("YOUR_OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase))
        {
            authList.Add(new ProviderAuthentication(LLmProviders.OpenAi, config.ApiKeys.OpenAi));
            Log("✓ OpenAI API key configured");
        }
        else
        {
            Log("⚠ OpenAI API key not configured");
        }

        if (!string.IsNullOrEmpty(config.ApiKeys.Anthropic) && 
            !config.ApiKeys.Anthropic.Equals("YOUR_ANTHROPIC_API_KEY", StringComparison.OrdinalIgnoreCase))
        {
            authList.Add(new ProviderAuthentication(LLmProviders.Anthropic, config.ApiKeys.Anthropic));
            Log("✓ Anthropic API key configured");
        }

        if (!string.IsNullOrEmpty(config.ApiKeys.Google) && 
            !config.ApiKeys.Google.Equals("YOUR_GOOGLE_API_KEY", StringComparison.OrdinalIgnoreCase))
        {
            authList.Add(new ProviderAuthentication(LLmProviders.Google, config.ApiKeys.Google));
        }

        if (!string.IsNullOrEmpty(config.ApiKeys.Groq) && 
            !config.ApiKeys.Groq.Equals("YOUR_GROQ_API_KEY", StringComparison.OrdinalIgnoreCase))
        {
            authList.Add(new ProviderAuthentication(LLmProviders.Groq, config.ApiKeys.Groq));
            Log("✓ Groq API key configured");
        }

        // Check Tavily key for research capability
        if (!string.IsNullOrEmpty(config.ApiKeys.Tavily) && 
            !config.ApiKeys.Tavily.Equals("YOUR_TAVILY_API_KEY", StringComparison.OrdinalIgnoreCase))
        {
            Log("✓ Tavily API key configured (research enabled)");
        }
        else
        {
            Log("⚠ Tavily API key not configured (research will be limited)");
        }

        if (authList.Count == 0)
        {
            throw new InvalidOperationException(
                "\n✗ ERROR: No valid API keys configured!\n" +
                "Please edit appCfg.json and add at least one valid API key.\n" +
                "See appCfg.example.json for reference.");
        }

        Console.WriteLine($"\n✓ TornadoApi initialized with {authList.Count} provider(s)\n");
        return new TornadoApi(authList.ToArray());

        void Log(string msg)
        {
            if (checkConfig)
            {
                Console.WriteLine(msg);
            }
        } 
    }
}