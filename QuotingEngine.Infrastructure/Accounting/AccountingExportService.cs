using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuotingEngine.Core.Models;

namespace QuotingEngine.Infrastructure.Accounting;

public class JournalEntry
{
    public DateTime Date { get; set; }
    public string ReferenceId { get; set; } = string.Empty;
    public string Account { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class AccountingExportService
{
    public IEnumerable<JournalEntry> GenerateJournalEntries(IEnumerable<Quote> settledQuotes)
    {
        var entries = new List<JournalEntry>();

        foreach (var quote in settledQuotes.Where(q => q.Status == QuoteStatus.Settled))
        {
            decimal totalNetCash = 0;

            foreach (var item in quote.Items)
            {
                var entry = new JournalEntry
                {
                    Date = quote.LockedAtUtc ?? quote.CreatedAtUtc,
                    ReferenceId = quote.ReferenceId,
                    Description = item.Description
                };

                if (item.Type == TransactionType.Buy)
                {
                    // Dealer buys item. 
                    // Debit Inventory, Credit nothing yet (cash leg comes later)
                    entry.Account = "Inventory - Precious Metals";
                    entry.Debit = item.FinalFiatPrice;
                    totalNetCash += item.FinalFiatPrice; // Dealer owes customer
                }
                else
                {
                    // Dealer sells item. 
                    // Credit COGS / Inventory
                    entry.Account = "COGS - Precious Metals";
                    entry.Credit = Math.Abs(item.FinalFiatPrice);
                    totalNetCash += item.FinalFiatPrice; // Customer owes dealer (negative value)
                }

                entries.Add(entry);
            }

            // Create the balancing cash entry
            var cashEntry = new JournalEntry
            {
                Date = quote.LockedAtUtc ?? quote.CreatedAtUtc,
                ReferenceId = quote.ReferenceId,
                Description = "Net Settlement"
            };

            if (totalNetCash > 0)
            {
                // Dealer pays customer
                cashEntry.Account = "Cash / Bank";
                cashEntry.Credit = totalNetCash;
            }
            else if (totalNetCash < 0)
            {
                // Customer pays dealer
                cashEntry.Account = "Cash / Bank";
                cashEntry.Debit = Math.Abs(totalNetCash);
            }

            if (totalNetCash != 0)
            {
                entries.Add(cashEntry);
            }
        }

        return entries;
    }

    public string ExportToCsv(IEnumerable<JournalEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Date,ReferenceId,Account,Debit,Credit,Description");

        foreach (var entry in entries)
        {
            sb.AppendLine($"{entry.Date:yyyy-MM-dd},{entry.ReferenceId},{entry.Account},{entry.Debit:F2},{entry.Credit:F2},{entry.Description}");
        }

        return sb.ToString();
    }
}
