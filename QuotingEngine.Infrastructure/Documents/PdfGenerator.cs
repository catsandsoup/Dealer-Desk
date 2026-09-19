using System;
using System.IO;
using System.Security.Cryptography;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuotingEngine.Core.Models;
using QuotingEngine.Infrastructure.Configuration;

namespace QuotingEngine.Infrastructure.Documents;

public class PdfGenerator
{
    private readonly DealerConfig _config;

    public PdfGenerator()
    {
        // Must configure QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
        _config = ConfigurationManager.Load();
    }

    public (byte[] PdfBytes, string Sha256Hash) GenerateQuotePdf(Quote quote)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Element(compose => ComposeHeader(compose, quote));
                page.Content().Element(compose => ComposeContent(compose, quote));
                page.Footer().Element(compose => ComposeFooter(compose));
            });
        });

        var bytes = document.GeneratePdf();
        using var sha256 = SHA256.Create();
        var hash = Convert.ToHexString(sha256.ComputeHash(bytes));

        return (bytes, hash);
    }

    private void ComposeHeader(IContainer container, Quote quote)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text($"{_config.CompanyName} - OFFICIAL QUOTE").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text($"ABN/EIN: {_config.CompanyAbn}");
                column.Item().PaddingBottom(10);

                column.Item().Text($"Reference: {quote.ReferenceId}");
                column.Item().Text($"Customer: {quote.CustomerName}");
                
                if (!string.IsNullOrWhiteSpace(quote.CustomerIdDocument) && !string.IsNullOrWhiteSpace(quote.CustomerIdNumber))
                {
                    column.Item().Text($"ID Verified: {quote.CustomerIdDocument} / {quote.CustomerIdNumber}");
                }

                column.Item().Text($"Created: {quote.CreatedAtUtc:O}");
                if (quote.ExpiresAtUtc.HasValue)
                {
                    column.Item().Text($"VALID UNTIL: {quote.ExpiresAtUtc.Value:O}").SemiBold().FontColor(Colors.Red.Medium);
                }
                else
                {
                    column.Item().Text("VALID UNTIL: INSTANT").SemiBold().FontColor(Colors.Red.Medium);
                }
            });
        });
    }

    private void ComposeContent(IContainer container, Quote quote)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Spacing(5);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(1); // Action (Buy/Sell)
                    columns.RelativeColumn(3); // Description
                    columns.RelativeColumn(1); // Gross Wt
                    columns.RelativeColumn(1); // Purity
                    columns.RelativeColumn(1); // Fine Wt
                    columns.RelativeColumn(2); // Final Price
                });

                table.Header(header =>
                {
                    header.Cell().Text("Action").SemiBold();
                    header.Cell().Text("Description").SemiBold();
                    header.Cell().AlignRight().Text("Gross (g)").SemiBold();
                    header.Cell().AlignRight().Text("Purity").SemiBold();
                    header.Cell().AlignRight().Text("Fine (g)").SemiBold();
                    header.Cell().AlignRight().Text("Total Fiat").SemiBold();
                    
                    header.Cell().ColumnSpan(6).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                });

                foreach (var item in quote.Items)
                {
                    table.Cell().Text(item.Type.ToString());
                    table.Cell().Text(item.Description);
                    table.Cell().AlignRight().Text($"{item.GrossWeightGrams:F2}");
                    table.Cell().AlignRight().Text($"{item.PurityPercentage:P2}");
                    table.Cell().AlignRight().Text($"{item.FineWeightGrams:F4}");
                    table.Cell().AlignRight().Text($"${item.FinalFiatPrice:F2}");
                }
            });

            column.Item().PaddingTop(25).AlignRight().Text($"Settlement Total: ${quote.SettlementTotal:F2}").FontSize(14).SemiBold();
            
            column.Item().PaddingTop(10).AlignRight().Text(quote.SettlementTotal >= 0 ? "Dealer Owes Customer" : "Customer Owes Dealer").FontSize(12).Italic();
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().PaddingBottom(10).Text(_config.CustomTermsAndConditions).FontSize(9).FontColor(Colors.Grey.Darken1);
            
            column.Item().AlignCenter().Text(x =>
            {
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });
        });
    }
}
