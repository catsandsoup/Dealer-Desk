using System;
using System.Collections.Generic;
using System.Linq;

namespace QuotingEngine.Core.Models;

public class Quote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // 8-character string for easy counter reference
    public string ReferenceId { get; set; } = Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
    
    public string CustomerName { get; set; } = string.Empty;
    
    // AML/KYC fields
    public string CustomerIdDocument { get; set; } = string.Empty;
    public string CustomerIdNumber { get; set; } = string.Empty;

    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;
    
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    
    public DateTime? LockedAtUtc { get; set; }
    
    // Number of hours for the grace period (0 = instant)
    public int GracePeriodHours { get; set; }
    
    public DateTime? ExpiresAtUtc { get; set; }
    
    /// <summary>
    /// PRD §4.5: The exact spot price per gram at the moment the quote was locked.
    /// This creates an immutable snapshot of the market tick for audit/dispute resolution.
    /// </summary>
    public decimal? LockedSpotPricePerGram { get; set; }
    
    /// <summary>
    /// PRD §4.5/§4.7: SHA-256 hash of the generated PDF binary.
    /// Used for tamper-proof verification — the customer's printed slip must match
    /// the server record hash. No quote should be half-locked without an associated PDF hash.
    /// </summary>
    public string? PdfSha256Hash { get; set; }
    
    public List<LineItem> Items { get; set; } = new();
    
    // Settlement total is sum of all items (positive = dealer pays customer, negative = customer pays dealer)
    public decimal SettlementTotal => Items.Sum(i => i.FinalFiatPrice);
    
    // Validates if the quote is still within its grace period
    public bool IsExpired()
    {
        if (Status != QuoteStatus.Locked) return false;
        if (!ExpiresAtUtc.HasValue) return false;
        
        return DateTime.UtcNow > ExpiresAtUtc.Value;
    }
}
