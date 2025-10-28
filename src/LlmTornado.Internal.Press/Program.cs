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
using LlmTornado.Mcp;

namespace LlmTornado.Internal.Press;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== LlmTornado Internal Press - Journalist Agent ===\n");

        try
        {
            // Load configuration
            var config = AppConfiguration.Load();
            Console.WriteLine($"Objective: {config.Objective}\n");

            // Initialize database
            await DatabaseInitializer.InitializeAsync();

            // Create services
            var dbContext = new PressDbContext();
            var client = CreateTornadoClient(config);
            var service = new ArticleGenerationService(client, config, dbContext);

            new Common.Tool(() => { }, "my_name");
            
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
            var input = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            var trimmedInput = input.Trim();

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
            var parts = trimmedInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
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

        var command = args[0].ToLower();

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

            case "reset-db":
            case "reset":
                await HandleResetDbCommand();
                break;

            case "clear-queue":
            case "clearq":
                await HandleClearQueueCommand(dbContext);
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
        Console.WriteLine("  clear-queue           Clear all pending items from queue (alias: clearq)");
        Console.WriteLine("  status                Show queue and article statistics (alias: stat)");
        Console.WriteLine("  export-all            Export all articles to markdown/JSON (alias: export)");
        Console.WriteLine("  reset-db              Reset the database - DELETES ALL DATA (alias: reset)");
        Console.WriteLine("  help                  Show this help message (alias: ?)");
        Console.WriteLine("  clear                 Clear the screen (alias: cls)");
        Console.WriteLine("  exit                  Exit the application (alias: quit)");
        Console.WriteLine();
        Console.WriteLine("Examples:");
        Console.WriteLine("  generate 5            Generate 5 articles");
        Console.WriteLine("  seed 10               Seed queue with 10 ideas");
        Console.WriteLine("  status                Check current status");
        Console.WriteLine();
        Console.WriteLine("Command-line mode:");
        Console.WriteLine("  dotnet run -- generate 5");
        Console.WriteLine("  dotnet run -- seed-queue 10");
    }

    static async Task HandleGenerateCommand(string[] args, ArticleGenerationService service)
    {
        int count = 1;
        if (args.Length > 1 && int.TryParse(args[1], out int parsedCount))
        {
            count = parsedCount;
        }

        Console.WriteLine($"Generating {count} article(s)...\n");
        
        var articles = await service.GenerateArticlesAsync(count);
        
        Console.WriteLine($"\n=== Generation Complete ===");
        Console.WriteLine($"Successfully generated: {articles.Count} articles");
    }

    static async Task HandleSeedQueueCommand(string[] args, ArticleGenerationService service)
    {
        int count = 5;
        if (args.Length > 1 && int.TryParse(args[1], out int parsedCount))
        {
            count = parsedCount;
        }

        var added = await service.SeedQueueAsync(count);
        
        Console.WriteLine($"\n=== Queue Seeding Complete ===");
        Console.WriteLine($"Added {added} ideas to queue");
    }

    static async Task HandleStatusCommand(ArticleGenerationService service)
    {
        Console.WriteLine("=== System Status ===\n");
        
        var stats = await service.GetQueueStatsAsync();
        
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
        
        var articles = await dbContext.Articles.ToListAsync();
        
        if (articles.Count == 0)
        {
            Console.WriteLine("No articles to export");
            return;
        }

        var markdownExporter = new Export.MarkdownExporter(config.Output.Directory);
        var jsonExporter = new Export.JsonExporter(config.Output.Directory);

        int exported = 0;
        foreach (var article in articles)
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

    static async Task HandleClearQueueCommand(PressDbContext dbContext)
    {
        Console.Write("Clear queue? This will remove all pending queue items (y/n): ");
        var confirmation = Console.ReadLine();

        if (confirmation?.ToLower() != "y" && confirmation?.ToLower() != "yes")
        {
            Console.WriteLine("✓ Queue clear cancelled");
            return;
        }

        // Get count before clearing
        var pendingCount = await dbContext.ArticleQueue
            .Where(q => q.Status == Database.Models.QueueStatus.Pending || 
                       q.Status == Database.Models.QueueStatus.InProgress)
            .CountAsync();

        if (pendingCount == 0)
        {
            Console.WriteLine("Queue is already empty");
            return;
        }

        // Remove all pending and in-progress items
        var itemsToRemove = await dbContext.ArticleQueue
            .Where(q => q.Status == Database.Models.QueueStatus.Pending || 
                       q.Status == Database.Models.QueueStatus.InProgress)
            .ToListAsync();

        dbContext.ArticleQueue.RemoveRange(itemsToRemove);
        await dbContext.SaveChangesAsync();

        Console.WriteLine($"✓ Cleared {pendingCount} item(s) from queue");
        
        // Show what's left
        var completedCount = await dbContext.ArticleQueue
            .Where(q => q.Status == Database.Models.QueueStatus.Completed)
            .CountAsync();
        var failedCount = await dbContext.ArticleQueue
            .Where(q => q.Status == Database.Models.QueueStatus.Failed)
            .CountAsync();

        if (completedCount > 0 || failedCount > 0)
        {
            Console.WriteLine($"   Kept: {completedCount} completed, {failedCount} failed");
        }
    }

    static async Task HandleResetDbCommand()
    {
        Console.WriteLine("⚠️  WARNING: This will DELETE ALL DATA from the database!");
        Console.Write("Type 'yes' to confirm: ");
        var confirmation = Console.ReadLine();

        if (confirmation?.ToLower() != "yes")
        {
            Console.WriteLine("✓ Database reset cancelled");
            return;
        }

        Console.WriteLine("Resetting database...");
        await DatabaseInitializer.ResetDatabaseAsync();
        Console.WriteLine("✓ Database has been reset");
    }

    static TornadoApi CreateTornadoClient(AppConfiguration config)
    {
        var authList = new System.Collections.Generic.List<ProviderAuthentication>();

        // Validate and add API keys
        if (!string.IsNullOrEmpty(config.ApiKeys.OpenAi) && 
            !config.ApiKeys.OpenAi.Equals("YOUR_OPENAI_API_KEY", StringComparison.OrdinalIgnoreCase))
        {
            authList.Add(new ProviderAuthentication(LLmProviders.OpenAi, config.ApiKeys.OpenAi));
            Console.WriteLine("✓ OpenAI API key configured");
        }
        else
        {
            Console.WriteLine("⚠ OpenAI API key not configured");
        }

        if (!string.IsNullOrEmpty(config.ApiKeys.Anthropic) && 
            !config.ApiKeys.Anthropic.Equals("YOUR_ANTHROPIC_API_KEY", StringComparison.OrdinalIgnoreCase))
        {
            authList.Add(new ProviderAuthentication(LLmProviders.Anthropic, config.ApiKeys.Anthropic));
            Console.WriteLine("✓ Anthropic API key configured");
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
            Console.WriteLine("✓ Groq API key configured");
        }

        // Check Tavily key for research capability
        if (!string.IsNullOrEmpty(config.ApiKeys.Tavily) && 
            !config.ApiKeys.Tavily.Equals("YOUR_TAVILY_API_KEY", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("✓ Tavily API key configured (research enabled)");
        }
        else
        {
            Console.WriteLine("⚠ Tavily API key not configured (research will be limited)");
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
    }
}