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
        modelBuilder.Entity<LineItem>().Ignore(p => p.FineWeightGrams);
        modelBuilder.Entity<LineItem>().Property(p => p.PurityPercentage).HasColumnType("numeric(18,4)");
        modelBuilder.Entity<LineItem>().Property(p => p.DealerMarginValue).HasColumnType("numeric(18,4)");
        modelBuilder.Entity<LineItem>().Property(p => p.StoredSpotPriceG).HasColumnType("numeric(18,2)");
        modelBuilder.Entity<LineItem>().Property(p => p.DefaultPremiumPct).HasColumnType("numeric(18,4)");
        modelBuilder.Entity<LineItem>().Property(p => p.ManagerOverrideSpreadDollar).HasColumnType("numeric(18,2)");

        modelBuilder.Entity<CatalogItem>().Property(p => p.GrossWeightGrams).HasColumnType("numeric(18,6)");
        modelBuilder.Entity<CatalogItem>().Property(p => p.PurityPercentage).HasColumnType("numeric(18,4)");
        modelBuilder.Entity<CatalogItem>().Property(p => p.DefaultMarginValue).HasColumnType("numeric(18,4)");

        modelBuilder.Entity<Quote>().Property(p => p.LockedSpotPricePerGram).HasColumnType("numeric(18,2)");
        
        // Define relations
        modelBuilder.Entity<Quote>()
            .HasMany(q => q.Items)
            .WithOne()
            .OnDelete(DeleteBehavior.Cascade);

        // Seed default Catalog Items with standard industry weights and purities
        // 1 Troy Ounce = 31.1035 grams
        modelBuilder.Entity<CatalogItem>().HasData(
            // US Mint
            new CatalogItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "1 oz American Gold Eagle", Type = CatalogItemType.Bullion, GrossWeightGrams = 33.931m, PurityPercentage = 0.9167m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 100m },
            new CatalogItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111112"), Name = "1 oz American Gold Buffalo", Type = CatalogItemType.Bullion, GrossWeightGrams = 31.1035m, PurityPercentage = 0.9999m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 100m },
            new CatalogItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111113"), Name = "1 oz American Silver Eagle", Type = CatalogItemType.Bullion, GrossWeightGrams = 31.1035m, PurityPercentage = 0.999m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 5m },
            new CatalogItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111114"), Name = "Pre-1933 $20 Liberty/St. Gaudens", Type = CatalogItemType.Bullion, GrossWeightGrams = 33.436m, PurityPercentage = 0.900m, MarginType = MarginType.Percentage, DefaultMarginValue = 0.05m },
            new CatalogItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111115"), Name = "Morgan/Peace Silver Dollar", Type = CatalogItemType.Bullion, GrossWeightGrams = 26.73m, PurityPercentage = 0.900m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 8m },
            new CatalogItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111116"), Name = "$1 Face Value 90% Silver (Junk)", Type = CatalogItemType.Bullion, GrossWeightGrams = 25.00m, PurityPercentage = 0.900m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 2m },
            new CatalogItem { Id = Guid.Parse("11111111-1111-1111-1111-111111111117"), Name = "1 oz American Platinum Eagle", Type = CatalogItemType.Bullion, GrossWeightGrams = 31.1035m, PurityPercentage = 0.9995m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 80m },

            // Perth Mint
            new CatalogItem { Id = Guid.Parse("22222222-2222-2222-2222-222222222221"), Name = "1 oz Gold Kangaroo", Type = CatalogItemType.Bullion, GrossWeightGrams = 31.1035m, PurityPercentage = 0.9999m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 80m },
            new CatalogItem { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "1 oz Silver Kangaroo", Type = CatalogItemType.Bullion, GrossWeightGrams = 31.1035m, PurityPercentage = 0.9999m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 3m },
            new CatalogItem { Id = Guid.Parse("22222222-2222-2222-2222-222222222223"), Name = "1 oz Platinum Kangaroo", Type = CatalogItemType.Bullion, GrossWeightGrams = 31.1035m, PurityPercentage = 0.9995m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 60m },

            // Royal Canadian Mint
            new CatalogItem { Id = Guid.Parse("33333333-3333-3333-3333-333333333331"), Name = "1 oz Gold Maple Leaf", Type = CatalogItemType.Bullion, GrossWeightGrams = 31.1035m, PurityPercentage = 0.9999m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 80m },
            new CatalogItem { Id = Guid.Parse("33333333-3333-3333-3333-333333333332"), Name = "1 oz Silver Maple Leaf", Type = CatalogItemType.Bullion, GrossWeightGrams = 31.1035m, PurityPercentage = 0.9999m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 4m },
            new CatalogItem { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "1 oz Platinum Maple Leaf", Type = CatalogItemType.Bullion, GrossWeightGrams = 31.1035m, PurityPercentage = 0.9995m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 70m },

            // South African Mint
            new CatalogItem { Id = Guid.Parse("44444444-4444-4444-4444-444444444441"), Name = "1 oz Gold Krugerrand", Type = CatalogItemType.Bullion, GrossWeightGrams = 33.930m, PurityPercentage = 0.9167m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 80m },

            // Austrian Mint
            new CatalogItem { Id = Guid.Parse("55555555-5555-5555-5555-555555555551"), Name = "1 oz Gold Philharmonic", Type = CatalogItemType.Bullion, GrossWeightGrams = 31.1035m, PurityPercentage = 0.9999m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 80m },
            new CatalogItem { Id = Guid.Parse("55555555-5555-5555-5555-555555555552"), Name = "1 oz Silver Philharmonic", Type = CatalogItemType.Bullion, GrossWeightGrams = 31.1035m, PurityPercentage = 0.999m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 3m },

            // PAMP Suisse / Generic Bars
            new CatalogItem { Id = Guid.Parse("66666666-6666-6666-6666-666666666661"), Name = "1 oz PAMP Gold Bar", Type = CatalogItemType.Bullion, GrossWeightGrams = 31.1035m, PurityPercentage = 0.9999m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 60m },
            new CatalogItem { Id = Guid.Parse("66666666-6666-6666-6666-666666666662"), Name = "1 oz Generic Silver Round", Type = CatalogItemType.Bullion, GrossWeightGrams = 31.1035m, PurityPercentage = 0.999m, MarginType = MarginType.FlatDollar, DefaultMarginValue = 2m },

            // Scrap
            new CatalogItem { Id = Guid.Parse("99999999-9999-9999-9999-999999999991"), Name = "Generic Scrap 9k", Type = CatalogItemType.Scrap, GrossWeightGrams = 0m, PurityPercentage = 0.375m, MarginType = MarginType.Percentage, DefaultMarginValue = -0.15m },
            new CatalogItem { Id = Guid.Parse("99999999-9999-9999-9999-999999999992"), Name = "Generic Scrap 14k", Type = CatalogItemType.Scrap, GrossWeightGrams = 0m, PurityPercentage = 0.585m, MarginType = MarginType.Percentage, DefaultMarginValue = -0.10m },
            new CatalogItem { Id = Guid.Parse("99999999-9999-9999-9999-999999999993"), Name = "Generic Scrap 18k", Type = CatalogItemType.Scrap, GrossWeightGrams = 0m, PurityPercentage = 0.750m, MarginType = MarginType.Percentage, DefaultMarginValue = -0.08m },
            new CatalogItem { Id = Guid.Parse("99999999-9999-9999-9999-999999999994"), Name = "Generic Scrap 22k", Type = CatalogItemType.Scrap, GrossWeightGrams = 0m, PurityPercentage = 0.916m, MarginType = MarginType.Percentage, DefaultMarginValue = -0.05m },
            new CatalogItem { Id = Guid.Parse("99999999-9999-9999-9999-999999999995"), Name = "Generic Scrap 24k", Type = CatalogItemType.Scrap, GrossWeightGrams = 0m, PurityPercentage = 0.999m, MarginType = MarginType.Percentage, DefaultMarginValue = -0.05m }
        );
    }
}

public class AppDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        return new AppDbContext();
    }
}
