using LlmTornado.Internal.Press.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace LlmTornado.Internal.Press.Database;

public class PressDbContext : DbContext
{
    public DbSet<Article> Articles { get; set; } = null!;
    public DbSet<ArticleQueue> ArticleQueue { get; set; } = null!;
    public DbSet<TrendingTopic> TrendingTopics { get; set; } = null!;
    public DbSet<WorkHistory> WorkHistory { get; set; } = null!;
    public DbSet<ArticlePublishStatus> ArticlePublishStatus { get; set; } = null!;

    public PressDbContext(DbContextOptions<PressDbContext> options) : base(options)
    {
    }

    public PressDbContext() : base()
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=press.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Article configuration
        modelBuilder.Entity<Article>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedDate);
            entity.HasIndex(e => e.Slug).IsUnique();
            
            entity.HasMany(e => e.WorkHistory)
                .WithOne(e => e.Article)
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ArticleQueue configuration
        modelBuilder.Entity<ArticleQueue>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Priority);
            entity.HasIndex(e => e.CreatedDate);
            entity.HasIndex(e => e.ScheduledDate);
        });

        // TrendingTopic configuration
        modelBuilder.Entity<TrendingTopic>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Topic);
            entity.HasIndex(e => e.DiscoveredDate);
            entity.HasIndex(e => e.Relevance);
            entity.HasIndex(e => e.IsActive);
        });

        // WorkHistory configuration
        modelBuilder.Entity<WorkHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ArticleId);
            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.Timestamp);
        });

        // ArticlePublishStatus configuration
        modelBuilder.Entity<ArticlePublishStatus>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ArticleId, e.Platform }).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PublishedDate);
            
            entity.HasOne(e => e.Article)
                .WithMany()
                .HasForeignKey(e => e.ArticleId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

