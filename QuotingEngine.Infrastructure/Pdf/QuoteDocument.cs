using System;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuotingEngine.Core.Models;

namespace QuotingEngine.Infrastructure.Pdf;

public class QuoteDocument : IDocument
{
    private readonly Quote _quote;
    private readonly DealerConfig _config;

    public QuoteDocument(Quote quote, DealerConfig config)
    {
        _quote = quote;
        _config = config;
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
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text(_config.CompanyName).FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text(_config.CompanyAbn).FontSize(12).FontColor(Colors.Grey.Darken2);
                column.Item().PaddingTop(10).Text($"Customer: {_quote.CustomerName}").FontSize(14);
                if (!string.IsNullOrEmpty(_quote.CustomerIdDocument))
                {
                    column.Item().Text($"ID: {_quote.CustomerIdDocument} - {_quote.CustomerIdNumber}");
                }
            });

            row.ConstantItem(200).Column(column =>
            {
                column.Item().AlignRight().Text($"Quote #{_quote.ReferenceId}").FontSize(16).SemiBold();
                column.Item().AlignRight().Text($"Date: {_quote.CreatedAtUtc.ToLocalTime():g}");
                
                if (_quote.ExpiresAtUtc.HasValue)
                {
                    DateTime localExpiry;
                    try
                    {
                        var tz = TimeZoneInfo.FindSystemTimeZoneById(_config.TimezoneId);
                        localExpiry = TimeZoneInfo.ConvertTimeFromUtc(_quote.ExpiresAtUtc.Value, tz);
                    }
                    catch
                    {
                        localExpiry = _quote.ExpiresAtUtc.Value.ToLocalTime();
                    }
                    
                    column.Item().PaddingTop(5).Background(Colors.Red.Lighten2).Padding(5)
                        .AlignRight()
                        .Text($"VALID UNTIL: {localExpiry:MMM dd, yyyy - HH:mm:ss}").FontColor(Colors.White).Bold();
                }
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.PaddingVertical(20).Column(column => 
        {
            column.Item().Element(ComposeTable);

            var total = _quote.SettlementTotal;
            var directionText = total >= 0 ? "Dealer Pays Customer" : "Customer Pays Dealer";
            
            column.Item().PaddingTop(20).AlignRight().Text($"Net Settlement: {Math.Abs(total):C} ({_config.BaseCurrency})").FontSize(16).Bold();
            column.Item().AlignRight().Text(directionText).FontSize(12).SemiBold().FontColor(Colors.Grey.Darken2);
        });
    }

    private void ComposeTable(IContainer container)
    {
        container.Table(table =>
        {
            // Define columns
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(25); // #
                columns.RelativeColumn(3); // Description
                columns.RelativeColumn(1); // Gross Wt
                columns.RelativeColumn(1); // Purity
                columns.RelativeColumn(1); // Fine Wt
                columns.RelativeColumn(1); // Spot
                columns.RelativeColumn(1); // Margin
                columns.RelativeColumn(1.5f); // Total
            });

            // Header
            table.Header(header =>
            {
                header.Cell().Text("#").SemiBold();
                header.Cell().Text("Description").SemiBold();
                header.Cell().AlignRight().Text("Gross (g)").SemiBold();
                header.Cell().AlignRight().Text("Purity %").SemiBold();
                header.Cell().AlignRight().Text("Fine (g)").SemiBold();
                header.Cell().AlignRight().Text("Spot/g").SemiBold();
                header.Cell().AlignRight().Text("Margin").SemiBold();
                header.Cell().AlignRight().Text("Total").SemiBold();

                header.Cell().ColumnSpan(8)
                    .PaddingTop(5).BorderBottom(1).BorderColor(Colors.Black);
            });

            // Items
            foreach (var (item, index) in _quote.Items.Select((item, i) => (item, i)))
            {
                table.Cell().Element(CellStyle).Text($"{index + 1}");
                
                string assayStr = item.AssayMethod != "None" ? $" ({item.AssayMethod})" : "";
                table.Cell().Element(CellStyle).Text($"{item.Type.ToString().ToUpper()} - {item.Description}{assayStr}");
                
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.GrossWeightGrams:F2}");
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.PurityPercentage:P1}");
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.FineWeightGrams:F2}");
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.LiveSpotPricePerGram:C2}");
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.DealerMarginValue:P1}");
                table.Cell().Element(CellStyle).AlignRight().Text($"{item.FinalFiatPrice:C2}");

                static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
            }
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Column(column =>
        {
            column.Item().Text("Terms & Conditions").SemiBold();
            column.Item().Text(_config.CustomTermsAndConditions).FontSize(9).FontColor(Colors.Grey.Darken2);
            
            column.Item().PaddingTop(30).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderBottom(1).BorderColor(Colors.Black).Height(20);
                    c.Item().Text("Customer Signature").FontSize(9);
                });
                
                row.ConstantItem(50); // Spacer
                
                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderBottom(1).BorderColor(Colors.Black).Height(20);
                    c.Item().Text("Dealer Signature").FontSize(9);
                });
                
                row.ConstantItem(50); // Spacer
                
                row.RelativeItem().Column(c =>
                {
                    c.Item().BorderBottom(1).BorderColor(Colors.Black).Height(20);
                    c.Item().Text("Date").FontSize(9);
                });
            });
            
            column.Item().PaddingTop(20).AlignCenter().Text(x =>
            {
                x.Span("Page ");
                x.CurrentPageNumber();
                x.Span(" of ");
                x.TotalPages();
            });
        });
    }
}
