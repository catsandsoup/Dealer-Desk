using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuotingEngine.Core.Api;
using QuotingEngine.Core.Models;
using QuotingEngine.Infrastructure.Data;
using QuotingEngine.Infrastructure.Documents;
using Xunit;

namespace QuotingEngine.Tests;

public class IntegrationTests : IDisposable
{
    private readonly AppDbContext _db;
    private readonly string _testDbPath;

    public IntegrationTests()
    {
        _testDbPath = $"test_db_{Guid.NewGuid()}.db";
        _db = new AppDbContext(_testDbPath);
        _db.Database.EnsureCreated();
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        if (File.Exists(_testDbPath))
        {
            File.Delete(_testDbPath);
        }
    }

    [Fact]
    public void Test_SQLiteDatabaseRoundTrip()
    {
        // 1. Create Quote
        var quote = new Quote
        {
            CustomerName = "John Doe Test",
            Status = QuoteStatus.Locked,
            LockedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        };
        
        var calc = new QuotingEngine.Core.Services.PricingCalculator();
        var item = calc.CreateScrapItem(10m, 1m, 80m, 0);
        item.Type = TransactionType.Buy;

        // 2. Save
        quote.Items.Add(item);
        _db.Quotes.Add(quote);
        _db.SaveChanges();

        // 3. Load
        var loadedQuote = _db.Quotes.Include(q => q.Items).FirstOrDefault(q => q.ReferenceId == quote.ReferenceId);
        
        Assert.NotNull(loadedQuote);
        Assert.Equal("John Doe Test", loadedQuote.CustomerName);
        Assert.Single(loadedQuote.Items);
        Assert.Equal(800m, loadedQuote.Items[0].FinalFiatPrice);
    }

    [Fact]
    public void Test_PdfGeneration_ProducesValidByteArray()
    {
        var quote = new Quote
        {
            CustomerName = "PDF Test User",
            Status = QuoteStatus.Locked,
            LockedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
        };
        var calc = new QuotingEngine.Core.Services.PricingCalculator();
        var item = calc.CreateScrapItem(10m, 1m, 80m, 0);
        quote.Items.Add(item);

        var generator = new PdfGenerator();
        var result = generator.GenerateQuotePdf(quote);
        var pdfBytes = result.PdfBytes;

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 0);
        
        // PDF magic number check (%PDF-)
        Assert.Equal(0x25, pdfBytes[0]);
        Assert.Equal(0x50, pdfBytes[1]);
        Assert.Equal(0x44, pdfBytes[2]);
        Assert.Equal(0x46, pdfBytes[3]);
    }

    [Fact]
    public async Task Test_SpotPriceClient_WithoutApiKey_SetsStale()
    {
        var client = new SpotPriceClient("", "AUD", 24);
        var tcs = new TaskCompletionSource<bool>();

        // We can just assert that it starts stale and stays stale, or we can check IsStale directly.
        Assert.True(client.IsStale, "Client should be initialized as stale.");
    }


    [Fact]
    public void Test_BuySell_DirectionalityMath()
    {
        // Spot = 100/g
        // Weight = 10g, Purity = 1.0 -> 10g fine
        // Base value = $1000
        
        var buyItem = new LineItem
        {
            Type = TransactionType.Buy,
            GrossWeightGrams = 10m,
            PurityPercentage = 1m,
            MarginType = MarginType.Percentage,
            DealerMarginValue = -0.05m, // 5% feediscount
            LiveSpotPricePerGram = 100m
        };
        
        // Dealer buys, owes customer. Base 1000 * 0.95 = +$950
        Assert.Equal(950m, buyItem.FinalFiatPrice);

        var sellItem = new LineItem
        {
            Type = TransactionType.Sell,
            GrossWeightGrams = 10m,
            PurityPercentage = 1m,
            MarginType = MarginType.FlatDollar,
            DealerMarginValue = 100m, // $100 premium
            LiveSpotPricePerGram = 100m
        };

        // Dealer sells, customer owes dealer. Base 1000 + 100 Flat Premium = $1100, but negative because Sell
        Assert.Equal(-1100m, sellItem.FinalFiatPrice);
    }

    [Fact]
    public void Test_QuoteGracePeriod_ExpirationLogic()
    {
        var quote = new Quote
        {
            Status = QuoteStatus.Locked,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5) // 5 minutes ago
        };

        Assert.True(quote.IsExpired());

        quote.ExpiresAtUtc = DateTime.UtcNow.AddHours(24);
        Assert.False(quote.IsExpired());

        // Instant quotes (no expiry)
        quote.ExpiresAtUtc = null;
        Assert.False(quote.IsExpired());
    }
}
