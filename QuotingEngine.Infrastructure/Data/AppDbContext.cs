using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QuotingEngine.Core.Models;

namespace QuotingEngine.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public DbSet<Quote> Quotes { get; set; }
    public DbSet<LineItem> LineItems { get; set; }
    public DbSet<CatalogItem> CatalogItems { get; set; }
    
    private readonly string _dbPath;
    
    public AppDbContext(string dbPath = "quoting_engine.db")
    {
        _dbPath = dbPath;
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Using PostgreSQL for robust decimal precision and multi-user support
        // Hardcoded for development; should be moved to appsettings.json or secure store in production
        var connectionString = "Host=localhost;Database=DealerDesk;Username=postgres;Password=postgres";
        optionsBuilder.UseNpgsql(connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // For strict precision as per PRD, storing decimal as exact NUMERIC in Postgres
        modelBuilder.Entity<LineItem>().Property(p => p.GrossWeightGrams).HasColumnType("numeric(18,6)");
        modelBuilder.Entity<LineItem>().Property(p => p.FineWeightGrams).HasColumnType("numeric(18,6)");
        modelBuilder.Entity<LineItem>().Property(p => p.PurityPercentage).HasColumnType("numeric(18,4)");
        modelBuilder.Entity<LineItem>().Property(p => p.DealerMarginPercentage).HasColumnType("numeric(18,4)");
        modelBuilder.Entity<LineItem>().Property(p => p.StoredSpotPriceG).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<LineItem>().Property(p => p.DefaultPremiumPct).HasColumnType("numeric(18,4)");
        modelBuilder.Entity<LineItem>().Property(p => p.ManagerOverrideSpreadDollar).HasColumnType("numeric(18,2)");

        modelBuilder.Entity<CatalogItem>().Property(p => p.GrossWeightGrams).HasColumnType("numeric(18,6)");
        modelBuilder.Entity<CatalogItem>().Property(p => p.PurityPercentage).HasColumnType("numeric(18,4)");
        modelBuilder.Entity<CatalogItem>().Property(p => p.DefaultMarginPercentage).HasColumnType("numeric(18,4)");

        modelBuilder.Entity<Quote>().Property(p => p.LockedSpotPricePerGram).HasColumnType("numeric(18,2)");
        
        // Define relations
        modelBuilder.Entity<Quote>()
            .HasMany(q => q.Items)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        // Seed default Catalog Items
        modelBuilder.Entity<CatalogItem>().HasData(
            new CatalogItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "1 oz South African Krugerrand", Type = CatalogItemType.Bullion, GrossWeightGrams = 33.930m, PurityPercentage = 0.9167m, DefaultMarginPercentage = 0.03m },
            new CatalogItem { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "1 oz US Mint Eagle", Type = CatalogItemType.Bullion, GrossWeightGrams = 33.931m, PurityPercentage = 0.9167m, DefaultMarginPercentage = 0.04m },
            new CatalogItem { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "1 oz Canadian Maple Leaf", Type = CatalogItemType.Bullion, GrossWeightGrams = 31.103m, PurityPercentage = 0.9999m, DefaultMarginPercentage = 0.03m },
            new CatalogItem { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Generic Scrap 9k", Type = CatalogItemType.Scrap, GrossWeightGrams = 0m, PurityPercentage = 0.375m, DefaultMarginPercentage = -0.15m },
            new CatalogItem { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Generic Scrap 14k", Type = CatalogItemType.Scrap, GrossWeightGrams = 0m, PurityPercentage = 0.585m, DefaultMarginPercentage = -0.10m },
            new CatalogItem { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Generic Scrap 18k", Type = CatalogItemType.Scrap, GrossWeightGrams = 0m, PurityPercentage = 0.750m, DefaultMarginPercentage = -0.08m },
            new CatalogItem { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "Generic Scrap 22k", Type = CatalogItemType.Scrap, GrossWeightGrams = 0m, PurityPercentage = 0.916m, DefaultMarginPercentage = -0.05m },
            new CatalogItem { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), Name = "Generic Scrap 24k", Type = CatalogItemType.Scrap, GrossWeightGrams = 0m, PurityPercentage = 0.999m, DefaultMarginPercentage = -0.05m }
        );
    }
}
