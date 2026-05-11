using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefreshTokenExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresOnUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"RefreshTokens\" SET \"ExpiresOnUtc\" = \"CreatedAtUtc\" + INTERVAL '30 days' WHERE \"ExpiresOnUtc\" IS NULL;");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "ExpiresOnUtc",
                table: "RefreshTokens",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_Token",
                table: "RefreshTokens",
                columns: new[] { "UserId", "Token" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_Token",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "ExpiresOnUtc",
                table: "RefreshTokens");
        }
    }
}
