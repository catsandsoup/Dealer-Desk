using System.Text.Json.Serialization;

namespace QuotingEngine.Core.Models;

public class DealerConfig
{
    public bool IsConfigured { get; set; } = false;
    
    // Company Profile
    public string CompanyName { get; set; } = "Acme Precious Metals";
    public string CompanyAbn { get; set; } = "ABN 12 345 678 901";
    public string CompanyLogoPath { get; set; } = "";
    public string CustomTermsAndConditions { get; set; } = "All quotes are binding once locked, subject to physical verification of the items presented. Payouts over $10,000 require valid government ID.";
    
    // Localization
    public string BaseCurrency { get; set; } = "AUD";
    public string TimezoneId { get; set; } = "AUS Eastern Standard Time"; // Windows timezone ID
    
    // Assay Tools
    public System.Collections.Generic.List<string> AvailableAssayTools { get; set; } = new();
    
    // Security
    public string ManagerPin { get; set; } = "1234";
    
    // Margins
    public decimal DefaultScrapMargin { get; set; } = -0.05m;
    
    // Data Providers (MetalPriceAPI)
    public string MetalPriceApiKey { get; set; } = "";
    public int MetalPriceCacheHours { get; set; } = 24;

    // Hardware Mapping
    public string ScaleComPort { get; set; } = "";
    public string XrfComPort { get; set; } = "";
}
