using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace QuotingEngine.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedBullionCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CatalogItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    GrossWeightGrams = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    PurityPercentage = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DefaultMarginPercentage = table.Column<decimal>(type: "numeric(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferenceId = table.Column<string>(type: "text", nullable: false),
                    CustomerName = table.Column<string>(type: "text", nullable: false),
                    CustomerIdDocument = table.Column<string>(type: "text", nullable: false),
                    CustomerIdNumber = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LockedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GracePeriodHours = table.Column<int>(type: "integer", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedSpotPricePerGram = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    PdfSha256Hash = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemPhoto = table.Column<byte[]>(type: "bytea", nullable: true),
                    StoredSpotPriceG = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    DefaultPremiumPct = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ManagerOverrideSpreadDollar = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    GrossWeightGrams = table.Column<decimal>(type: "numeric(18,6)", nullable: false),
                    PurityPercentage = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    LiveSpotPricePerGram = table.Column<decimal>(type: "numeric", nullable: false),
                    DealerMarginPercentage = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LineItems_Quotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "Quotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "CatalogItems",
                columns: new[] { "Id", "DefaultMarginPercentage", "GrossWeightGrams", "Name", "PurityPercentage", "Type" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 0.03m, 33.930m, "1 oz South African Krugerrand", 0.9167m, 1 },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 0.04m, 33.931m, "1 oz US Mint Gold Eagle", 0.9167m, 1 },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 0.03m, 31.1035m, "1 oz Canadian Maple Leaf", 0.9999m, 1 },
                    { new Guid("44444444-4444-4444-4444-444444444444"), -0.15m, 0m, "Generic Scrap 9k", 0.375m, 0 },
                    { new Guid("55555555-5555-5555-5555-555555555555"), -0.10m, 0m, "Generic Scrap 14k", 0.585m, 0 },
                    { new Guid("66666666-6666-6666-6666-666666666666"), -0.08m, 0m, "Generic Scrap 18k", 0.750m, 0 },
                    { new Guid("77777777-7777-7777-7777-777777777777"), -0.05m, 0m, "Generic Scrap 22k", 0.916m, 0 },
                    { new Guid("88888888-8888-8888-8888-888888888888"), -0.05m, 0m, "Generic Scrap 24k", 0.999m, 0 },
                    { new Guid("99999999-9999-9999-9999-999999999999"), 0.025m, 31.1035m, "1 oz Australian Kangaroo (Perth Mint)", 0.9999m, 1 },
                    { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), 0.02m, 31.1035m, "1 oz ABC Bullion Cast Bar", 0.9999m, 1 },
                    { new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), 0.03m, 31.1035m, "1 oz PAMP Suisse Fortuna Bar", 0.9999m, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_LineItems_QuoteId",
                table: "LineItems",
                column: "QuoteId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CatalogItems");

            migrationBuilder.DropTable(
                name: "LineItems");

            migrationBuilder.DropTable(
                name: "Quotes");
        }
    }
}
