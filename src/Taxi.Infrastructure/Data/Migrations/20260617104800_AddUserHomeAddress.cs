using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserHomeAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HomeAddressLabel",
                table: "DomainUsers",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HomeAddressLatitude",
                table: "DomainUsers",
                type: "numeric(18,10)",
                precision: 18,
                scale: 10,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HomeAddressLongitude",
                table: "DomainUsers",
                type: "numeric(18,10)",
                precision: 18,
                scale: 10,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HomeAddressLabel",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "HomeAddressLatitude",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "HomeAddressLongitude",
                table: "DomainUsers");
        }
    }
}
