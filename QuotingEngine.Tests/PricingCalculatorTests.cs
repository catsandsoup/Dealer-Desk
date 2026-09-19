using QuotingEngine.Core.Services;
using Xunit;

namespace QuotingEngine.Tests;

public class PricingCalculatorTests
{
    private readonly PricingCalculator _calculator = new();

    [Fact]
    public void Test_TroyOunceToGramConversion_IsExact()
    {
        // 1 Troy Ounce = 31.1034768 grams exactly as per PRD
        var grams = _calculator.ConvertTroyOuncesToGrams(1m);
        Assert.Equal(31.1034768m, grams);
    }

    [Fact]
    public void Test_ScrapItem_CalculatesCorrectFiatValue_WithBankersRounding()
    {
        // 10 grams of 58.5% (14K) Gold at $82.50/g spot, with a -5% dealer margin
        // Fine weight = 10 * 0.585 = 5.85g
        // Spot value = 5.85 * 82.50 = 482.625
        // Margin = 482.625 * (1 - 0.05) = 458.49375
        // Final Fiat = Bank round of 458.49375 to 2 decimal places = 458.49
        
        var item = _calculator.CreateScrapItem(10m, 0.585m, 82.50m, -0.05m);
        
        Assert.Equal(5.85m, item.FineWeightGrams);
        Assert.Equal(482.625m, item.FineWeightGrams * item.LiveSpotPricePerGram);
        Assert.Equal(458.49375m, item.RawFiatValue);
        Assert.Equal(458.49m, item.FinalFiatPrice);
    }
    
    [Fact]
    public void Test_BankersRounding_EdgeCase()
    {
        // Bank rounding rounds .5 to the nearest even number.
        // Let's create an item that lands exactly on X.XX5
        // 1g * 1.0 purity * 1.0 spot * (1 + 0.005 margin) = 1.005
        // Rounding 1.005 to 2 places -> 1.00
        
        var item1 = _calculator.CreateScrapItem(1m, 1m, 1m, 0.005m);
        Assert.Equal(1.00m, item1.FinalFiatPrice);
        
        // 1g * 1.0 purity * 1.0 spot * (1 + 0.015 margin) = 1.015
        // Rounding 1.015 to 2 places -> 1.02
        var item2 = _calculator.CreateScrapItem(1m, 1m, 1m, 0.015m);
        Assert.Equal(1.02m, item2.FinalFiatPrice);
    }
}
