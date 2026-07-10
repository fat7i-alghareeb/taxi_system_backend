using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNoDriverDetection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NoDriverDecisionRequired",
                table: "Trips",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NoDriverPromptDueAtUtc",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Status_NoDriverPromptDueAtUtc",
                table: "Trips",
                columns: new[] { "Status", "NoDriverPromptDueAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Trips_Status_NoDriverPromptDueAtUtc",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "NoDriverDecisionRequired",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "NoDriverPromptDueAtUtc",
                table: "Trips");
        }
    }
}
