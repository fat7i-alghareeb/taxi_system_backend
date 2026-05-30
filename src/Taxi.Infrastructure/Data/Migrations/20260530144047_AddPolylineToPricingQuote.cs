using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPolylineToPricingQuote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EncodedOverviewPolyline",
                table: "PricingQuotes",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RouteSegmentsJson",
                table: "PricingQuotes",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EncodedOverviewPolyline",
                table: "PricingQuotes");

            migrationBuilder.DropColumn(
                name: "RouteSegmentsJson",
                table: "PricingQuotes");
        }
    }
}
