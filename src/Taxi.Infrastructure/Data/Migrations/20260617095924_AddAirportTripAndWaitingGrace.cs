using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAirportTripAndWaitingGrace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing waiting sessions predate airport trips → standard 10-minute grace.
            migrationBuilder.AddColumn<int>(
                name: "GraceMinutes",
                table: "TripWaitingSessions",
                type: "integer",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<bool>(
                name: "IsAirport",
                table: "Trips",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GraceMinutes",
                table: "TripWaitingSessions");

            migrationBuilder.DropColumn(
                name: "IsAirport",
                table: "Trips");
        }
    }
}
