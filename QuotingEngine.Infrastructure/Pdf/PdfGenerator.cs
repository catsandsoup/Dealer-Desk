using System;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuotingEngine.Core.Models;
using System.Security.Cryptography;

namespace QuotingEngine.Infrastructure.Pdf;

public class PdfGenerator
{
    public static void Initialize()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static string GenerateQuotePdf(Quote quote, DealerConfig dealer)
    {
        Initialize();
        
        var document = new QuoteDocumentTemplate(quote, dealer);
        
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DealerDesk", "Quotes");
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        var timestamp = quote.LockedAtUtc?.ToLocalTime().ToString("yyyyMMdd_HHmmss") ?? DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var filePath = Path.Combine(directory, $"Quote_{quote.ReferenceId}_{timestamp}.pdf");
        
        document.GeneratePdf(filePath);
        return filePath;
    }

    public static string ComputeFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}

public class QuoteDocumentTemplate : IDocument
{
    public Quote Quote { get; }
    public DealerConfig Dealer { get; }

    public QuoteDocumentTemplate(Quote quote, DealerConfig dealer)
    {
        Quote = quote;
        Dealer = dealer;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    public void Compose(IDocumentContainer container)
    {
        container
            .Page(page =>
            {
                page.Margin(50);
                page.Size(PageSizes.A4);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

    void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(Dealer.CompanyName).FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text($"Registration: {Dealer.CompanyAbn}");
                column.Item().PaddingTop(5).Text("OFFICIAL QUOTE / RECEIPT").FontSize(14).Bold();
            });

            row.ConstantItem(150).AlignRight().Column(column =>
            {
                column.Item().Text($"Quote ID: {Quote.ReferenceId}").Bold();
                if (Quote.LockedAtUtc.HasValue)
                {
                    var tz = TimeZoneInfo.FindSystemTimeZoneById(Dealer.TimezoneId ?? "UTC");
                    var localTime = TimeZoneInfo.ConvertTimeFromUtc(Quote.LockedAtUtc.Value, tz);
                    column.Item().Text($"Date: {localTime:g}");
                    
                    var expiresAt = localTime.AddHours(24); // Assuming 24 hr grace period for now
                    column.Item().PaddingTop(5).Text($"VALID UNTIL: {expiresAt:g}").Bold().FontColor(Colors.Red.Medium);
                }
            });
        });
    }

    void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Customer Information:").SemiBold();
                    col.Item().Text($"Name: {Quote.CustomerName}");
                    if (!string.IsNullOrEmpty(Quote.CustomerIdDocument))
                    {
                        col.Item().Text($"ID Document: {Quote.CustomerIdDocument}");
                        col.Item().Text($"ID Number: {MaskId(Quote.CustomerIdNumber)}");
                    }
                });
            });

            column.Item().PaddingTop(20).Element(ComposeTable);
            
            column.Item().PaddingTop(20).AlignRight().Text($"Net Settlement: {Quote.SettlementTotal:C2} {Dealer.BaseCurrency}").FontSize(16).Bold();
            
            if (!string.IsNullOrEmpty(Dealer.CustomTermsAndConditions))
            {
                column.Item().PaddingTop(30).Text("Terms & Conditions").SemiBold();
                column.Item().Text(Dealer.CustomTermsAndConditions).FontSize(8).FontColor(Colors.Grey.Darken2);
            }
        });
    }

    void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(3); // Description
                columns.RelativeColumn(1); // Qty
                columns.RelativeColumn(1.5f); // Gross
                columns.RelativeColumn(1); // Purity
                columns.RelativeColumn(2); // Assay
                columns.RelativeColumn(2); // Spot Melt
                columns.RelativeColumn(2); // Margin
                columns.RelativeColumn(2); // Total
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderStyle).Text("Item");
                header.Cell().Element(HeaderStyle).Text("Qty");
                header.Cell().Element(HeaderStyle).Text("Gross(g)");
                header.Cell().Element(HeaderStyle).Text("Purity");
                header.Cell().Element(HeaderStyle).Text("Assay Tool");
                header.Cell().Element(HeaderStyle).Text($"Melt({Dealer.BaseCurrency})");
                header.Cell().Element(HeaderStyle).Text($"Margin({Dealer.BaseCurrency})");
                header.Cell().Element(HeaderStyle).Text($"Total({Dealer.BaseCurrency})");

                static IContainer HeaderStyle(IContainer c) =>
                    c.Background(Colors.Blue.Darken2).Padding(5).DefaultTextStyle(x => x.Bold().FontColor(Colors.White).FontSize(9));
            });

            foreach (var item in Quote.Items)
            {
                table.Cell().Element(RowStyle).Text(item.Description);
                table.Cell().Element(RowStyle).Text(item.Quantity.ToString());
                table.Cell().Element(RowStyle).Text($"{item.GrossWeightGrams:F2}");
                table.Cell().Element(RowStyle).Text($"{item.PurityPercentage * 100:F1}%");
                table.Cell().Element(RowStyle).Text(item.AssayMethod ?? "N/A");
                table.Cell().Element(RowStyle).Text($"{item.RawFiatValue:N2}");
                table.Cell().Element(RowStyle).Text($"{item.DealerMarginValue:N2}");
                table.Cell().Element(RowStyle).Text($"{item.FinalFiatPrice:N2}");

                static IContainer RowStyle(IContainer c) =>
                    c.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).DefaultTextStyle(x => x.FontSize(9));
            }
        });
    }
    
    void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().AlignLeft().Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
                x.Span(" of ");
                x.TotalPages();
            });
            row.RelativeItem().AlignRight().Text("Dealer Desk Enterprise POS").FontSize(8).FontColor(Colors.Grey.Medium);
        });
    }

    private string MaskId(string? id)
    {
        if (string.IsNullOrEmpty(id) || id.Length <= 4) return id ?? string.Empty;
        return new string('*', id.Length - 4) + id.Substring(id.Length - 4);
    }
}
