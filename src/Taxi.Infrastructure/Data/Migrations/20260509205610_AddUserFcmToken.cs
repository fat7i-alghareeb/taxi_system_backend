using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFcmToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtpSessions");

            migrationBuilder.AddColumn<string>(
                name: "FcmToken",
                table: "DomainUsers",
                type: "character varying(4096)",
                maxLength: 4096,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FcmToken",
                table: "DomainUsers");

            migrationBuilder.CreateTable(
                name: "OtpSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    Consumed = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpSessions", x => x.Id);
                });
        }
    }
}
