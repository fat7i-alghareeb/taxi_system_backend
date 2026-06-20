using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdminOnlyScheduledTripLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreArrivalNotifiedAtUtc",
                table: "Trips");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Trips",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptedAtUtc",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AcceptedByAdminId",
                table: "Trips",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptedReminder15SentAtUtc",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptedReminder30SentAtUtc",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UnacceptedOverdueSentAtUtc",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UnacceptedReminder15SentAtUtc",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UnacceptedReminder30SentAtUtc",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UnacceptedReminder60SentAtUtc",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Trips" AS t
                SET "AcceptedByAdminId" = d."UserId",
                    "AcceptedAtUtc" = COALESCE(t."AssignedAtUtc", t."LastModifiedUtc", t."CreatedAtUtc"),
                    "DriverId" = NULL
                FROM "Drivers" AS d
                INNER JOIN "AdminProfiles" AS a ON a."Id" = d."UserId"
                WHERE t."DriverId" = d."Id"
                  AND t."Status" IN ('DriverAssigned', 'DriverEnRoute', 'DriverArrived', 'InProgress');

                UPDATE "Trips"
                SET "Status" = 'AwaitingAdminAcceptance'
                WHERE "Status" IN ('Scheduled', 'PendingDriver');

                UPDATE "Trips"
                SET "Status" = CASE
                    WHEN "AcceptedByAdminId" IS NOT NULL THEN 'Accepted'
                    ELSE 'AwaitingAdminAcceptance'
                END,
                "DriverId" = CASE
                    WHEN "AcceptedByAdminId" IS NOT NULL THEN NULL
                    ELSE "DriverId"
                END
                WHERE "Status" = 'DriverAssigned';

                UPDATE "Trips" SET "Status" = 'EnRoute' WHERE "Status" = 'DriverEnRoute';
                UPDATE "Trips" SET "Status" = 'Arrived' WHERE "Status" = 'DriverArrived';
                """);

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Content = table.Column<string>(type: "jsonb", nullable: false),
                    ProcessedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trips_AcceptedByAdminId",
                table: "Trips",
                column: "AcceptedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Trips_Status_ScheduledAtUtc",
                table: "Trips",
                columns: new[] { "Status", "ScheduledAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedAtUtc_OccurredAtUtc",
                table: "OutboxMessages",
                columns: new[] { "ProcessedAtUtc", "OccurredAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Trips_AdminProfiles_AcceptedByAdminId",
                table: "Trips",
                column: "AcceptedByAdminId",
                principalTable: "AdminProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Trips_AdminProfiles_AcceptedByAdminId",
                table: "Trips");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_Trips_AcceptedByAdminId",
                table: "Trips");

            migrationBuilder.DropIndex(
                name: "IX_Trips_Status_ScheduledAtUtc",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "AcceptedAtUtc",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "AcceptedByAdminId",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "AcceptedReminder15SentAtUtc",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "AcceptedReminder30SentAtUtc",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "UnacceptedOverdueSentAtUtc",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "UnacceptedReminder15SentAtUtc",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "UnacceptedReminder30SentAtUtc",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "UnacceptedReminder60SentAtUtc",
                table: "Trips");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PreArrivalNotifiedAtUtc",
                table: "Trips",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Trips" SET "Status" = 'PendingDriver' WHERE "Status" = 'AwaitingAdminAcceptance';
                UPDATE "Trips" SET "Status" = 'DriverAssigned' WHERE "Status" = 'Accepted';
                UPDATE "Trips" SET "Status" = 'DriverEnRoute' WHERE "Status" = 'EnRoute';
                UPDATE "Trips" SET "Status" = 'DriverArrived' WHERE "Status" = 'Arrived';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Trips",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);
        }
    }
}
