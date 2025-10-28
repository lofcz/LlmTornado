using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace LlmTornado.Internal.Press.Database;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(string? connectionString = null)
    {
        connectionString ??= "Data Source=press.db";

        DbContextOptionsBuilder<PressDbContext> optionsBuilder = new DbContextOptionsBuilder<PressDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        await using PressDbContext context = new PressDbContext(optionsBuilder.Options);
        
        // Apply all pending migrations (creates database if it doesn't exist)
        await context.Database.MigrateAsync();
        
        Console.WriteLine($"Database initialized at: {GetDatabasePath(connectionString)}");
    }

    public static async Task MigrateAsync(string? connectionString = null)
    {
        connectionString ??= "Data Source=press.db";

        DbContextOptionsBuilder<PressDbContext> optionsBuilder = new DbContextOptionsBuilder<PressDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        await using PressDbContext context = new PressDbContext(optionsBuilder.Options);
        
        // Apply any pending migrations
        await context.Database.MigrateAsync();
        
        Console.WriteLine("Database migrations applied successfully");
    }

    public static async Task<bool> DatabaseExistsAsync(string? connectionString = null)
    {
        connectionString ??= "Data Source=press.db";

        DbContextOptionsBuilder<PressDbContext> optionsBuilder = new DbContextOptionsBuilder<PressDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        await using PressDbContext context = new PressDbContext(optionsBuilder.Options);
        return await context.Database.CanConnectAsync();
    }

    public static string GetDatabasePath(string connectionString)
    {
        // Extract file path from connection string
        string[] parts = connectionString.Split(';');
        foreach (string part in parts)
        {
            if (part.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                string path = part.Substring(part.IndexOf('=') + 1).Trim();
                return Path.GetFullPath(path);
            }
        }
        return "press.db";
    }

    public static async Task ResetDatabaseAsync(string? connectionString = null)
    {
        connectionString ??= "Data Source=press.db";

        DbContextOptionsBuilder<PressDbContext> optionsBuilder = new DbContextOptionsBuilder<PressDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        await using PressDbContext context = new PressDbContext(optionsBuilder.Options);
        
        // Delete and recreate the database
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        
        Console.WriteLine("Database reset successfully");
    }
}

