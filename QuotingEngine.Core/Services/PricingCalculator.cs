using System;
using QuotingEngine.Core.Models;

namespace QuotingEngine.Core.Services;

public class PricingCalculator
{
    private const decimal GramsPerTroyOunce = 31.1034768m;
    
    public decimal ConvertTroyOuncesToGrams(decimal troyOunces)
    {
        return troyOunces * GramsPerTroyOunce;
    }
    
    public decimal ConvertGramsToTroyOunces(decimal grams)
    {
        return grams / GramsPerTroyOunce;
    }
    
    public LineItem CreateScrapItem(decimal grossWeightGrams, decimal purityPercentage, decimal spotPricePerGram, decimal marginPercentage)
    {
        return new LineItem
        {
            Description = $"Scrap {purityPercentage:P1}",
            GrossWeightGrams = grossWeightGrams,
            PurityPercentage = purityPercentage,
            LiveSpotPricePerGram = spotPricePerGram,
            MarginType = MarginType.Percentage,
            DealerMarginValue = marginPercentage
        };
    }
    
    public LineItem CreateBullionItem(string coinName, decimal fineWeightOunces, decimal spotPricePerOunce, decimal premiumDollarAmount)
    {
        var spotPricePerGram = spotPricePerOunce / GramsPerTroyOunce;
        var grossWeightGrams = ConvertTroyOuncesToGrams(fineWeightOunces);
        
        return new LineItem
        {
            Description = coinName,
            GrossWeightGrams = grossWeightGrams,
            PurityPercentage = 1.0m, // fine bullion
            LiveSpotPricePerGram = spotPricePerGram,
            MarginType = MarginType.FlatDollar,
            DealerMarginValue = premiumDollarAmount
        };
    }
}
