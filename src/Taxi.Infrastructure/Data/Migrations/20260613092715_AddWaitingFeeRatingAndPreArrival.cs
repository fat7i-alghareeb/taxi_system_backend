using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWaitingFeeRatingAndPreArrival : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BillableMinutes",
                table: "TripWaitingSessions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RatePerMinute",
                table: "TripWaitingSessions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "PassengerRating",
                table: "Trips",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PreArrivalNotifiedAtUtc",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RatingComment",
                table: "Trips",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BillableMinutes",
                table: "TripWaitingSessions");

            migrationBuilder.DropColumn(
                name: "RatePerMinute",
                table: "TripWaitingSessions");

            migrationBuilder.DropColumn(
                name: "PassengerRating",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "PreArrivalNotifiedAtUtc",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "RatingComment",
                table: "Trips");
        }
    }
}
