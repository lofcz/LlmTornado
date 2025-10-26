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

        var optionsBuilder = new DbContextOptionsBuilder<PressDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        await using var context = new PressDbContext(optionsBuilder.Options);
        
        // Ensure database is created and all migrations are applied
        await context.Database.EnsureCreatedAsync();
        
        Console.WriteLine($"Database initialized at: {GetDatabasePath(connectionString)}");
    }

    public static async Task MigrateAsync(string? connectionString = null)
    {
        connectionString ??= "Data Source=press.db";

        var optionsBuilder = new DbContextOptionsBuilder<PressDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        await using var context = new PressDbContext(optionsBuilder.Options);
        
        // Apply any pending migrations
        await context.Database.MigrateAsync();
        
        Console.WriteLine("Database migrations applied successfully");
    }

    public static async Task<bool> DatabaseExistsAsync(string? connectionString = null)
    {
        connectionString ??= "Data Source=press.db";

        var optionsBuilder = new DbContextOptionsBuilder<PressDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        await using var context = new PressDbContext(optionsBuilder.Options);
        return await context.Database.CanConnectAsync();
    }

    public static string GetDatabasePath(string connectionString)
    {
        // Extract file path from connection string
        var parts = connectionString.Split(';');
        foreach (var part in parts)
        {
            if (part.Trim().StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                var path = part.Substring(part.IndexOf('=') + 1).Trim();
                return Path.GetFullPath(path);
            }
        }
        return "press.db";
    }

    public static async Task ResetDatabaseAsync(string? connectionString = null)
    {
        connectionString ??= "Data Source=press.db";

        var optionsBuilder = new DbContextOptionsBuilder<PressDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        await using var context = new PressDbContext(optionsBuilder.Options);
        
        // Delete and recreate the database
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        
        Console.WriteLine("Database reset successfully");
    }
}

