using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTripPickupDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PickupAddress",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "PickupCoordinate_Latitude",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "PickupCoordinate_Longitude",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "PickupHouseNumber",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "PickupStreetName",
                table: "Trips");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PickupAddress",
                table: "Trips",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PickupCoordinate_Latitude",
                table: "Trips",
                type: "numeric(18,10)",
                precision: 18,
                scale: 10,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PickupCoordinate_Longitude",
                table: "Trips",
                type: "numeric(18,10)",
                precision: 18,
                scale: 10,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "PickupHouseNumber",
                table: "Trips",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PickupStreetName",
                table: "Trips",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
