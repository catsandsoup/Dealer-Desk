using System;

namespace QuotingEngine.Core.Models;

public enum CatalogItemType
{
    Scrap,
    Bullion
}

public class CatalogItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Name { get; set; } = string.Empty;
    
    public CatalogItemType Type { get; set; }
    
    /// <summary>
    /// For bullion, this is the exact minted weight.
    /// For scrap, this might be 0, requiring the scale to provide the weight.
    /// </summary>
    public decimal GrossWeightGrams { get; set; }
    
    /// <summary>
    /// The expected purity (e.g., 0.9999 for 24k bullion, 0.585 for 14k scrap).
    /// </summary>
    public decimal PurityPercentage { get; set; }
    
    /// <summary>
    /// The default margin to apply when this item is selected.
    /// e.g. -0.05 (-5%) for scrap, +0.03 (+3%) for sovereign bullion.
    /// </summary>
    public decimal DefaultMarginPercentage { get; set; }
}
