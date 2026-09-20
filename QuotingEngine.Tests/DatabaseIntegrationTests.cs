using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using QuotingEngine.Core.Models;
using QuotingEngine.Infrastructure.Data;

namespace QuotingEngine.Tests;

public class DatabaseIntegrationTests : IDisposable
{
    private readonly AppDbContext _dbContext;

    public DatabaseIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
            
        _dbContext = new AppDbContext(options);
        // Ensure schema is created in memory
        _dbContext.Database.OpenConnection();
        _dbContext.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _dbContext.Database.CloseConnection();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task Can_Write_And_Read_Quote()
    {
        // Arrange
        var newQuote = new Quote
        {
            CustomerName = "Test Integration Customer",
            ReferenceId = "TEST-REF-123",
            CreatedAtUtc = DateTime.UtcNow
        };
        newQuote.Items.Add(new LineItem 
        {
            Description = "Gold Bar",
            GrossWeightGrams = 10m,
            LiveSpotPricePerGram = 100m,
            Type = TransactionType.Buy
        });

        // Act
        _dbContext.Quotes.Add(newQuote);
        await _dbContext.SaveChangesAsync();

        var retrievedQuote = await _dbContext.Quotes
            .Include(q => q.Items)
            .FirstOrDefaultAsync(q => q.ReferenceId == "TEST-REF-123");

        // Assert
        Assert.NotNull(retrievedQuote);
        Assert.Equal("Test Integration Customer", retrievedQuote.CustomerName);
        Assert.Single(retrievedQuote.Items);
        Assert.Equal("Gold Bar", retrievedQuote.Items.First().Description);
    }
}
