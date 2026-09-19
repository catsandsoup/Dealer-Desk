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
            DealerMarginPercentage = marginPercentage
        };
    }
    
    public LineItem CreateBullionItem(string coinName, decimal fineWeightOunces, decimal spotPricePerOunce, decimal premiumDollarAmount)
    {
        // Convert input spot from oz to gram internally, and calculate premium as a relative percentage of the spot,
        // or we could adjust LineItem to support flat premiums. The PRD says margin models can be flat dollar spread.
        // For simplicity with the current model, we convert flat premium to an effective margin %.
        
        var spotPricePerGram = spotPricePerOunce / GramsPerTroyOunce;
        var grossWeightGrams = ConvertTroyOuncesToGrams(fineWeightOunces);
        
        // Effective spot for the whole item
        var totalSpot = fineWeightOunces * spotPricePerOunce;
        var targetPrice = totalSpot + premiumDollarAmount;
        var margin = (targetPrice - totalSpot) / totalSpot;
        
        return new LineItem
        {
            Description = coinName,
            GrossWeightGrams = grossWeightGrams,
            PurityPercentage = 1.0m, // fine bullion
            LiveSpotPricePerGram = spotPricePerGram,
            DealerMarginPercentage = margin
        };
    }
}
