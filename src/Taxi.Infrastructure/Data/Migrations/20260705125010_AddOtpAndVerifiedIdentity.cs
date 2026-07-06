using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpAndVerifiedIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DomainUsers_Phone",
                table: "DomainUsers");

            migrationBuilder.AddColumn<string>(
                name: "GoogleId",
                table: "DomainUsers",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "DomainUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPhoneVerified",
                table: "DomainUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProfileResetAtUtc",
                table: "DomainUsers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OtpCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Channel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Purpose = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Recipient = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CodeHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                    MaxAttempts = table.Column<int>(type: "integer", nullable: false),
                    ResendCount = table.Column<int>(type: "integer", nullable: false),
                    LastSentAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ProviderMessageId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpCodes", x => x.Id);
                });

            // Backfill: every pre-existing account was created via Firebase-verified phone,
            // so mark those phones verified (keeps them working + uniquely enforced by the
            // partial index below). Emails were never verified previously → left unverified.
            migrationBuilder.Sql("UPDATE \"DomainUsers\" SET \"IsPhoneVerified\" = TRUE;");

            // Report (do NOT auto-pick a winner): flag pre-existing duplicate emails to the
            // server log for manual review. These stay unverified so the partial email unique
            // index never conflicts; see docs/AUTH_EXTERNAL_SERVICES_SETUP.md for remediation.
            migrationBuilder.Sql(@"
DO $$
DECLARE r RECORD;
BEGIN
  FOR r IN
    SELECT lower(""Email"") AS email, COUNT(*) AS cnt
    FROM ""DomainUsers""
    WHERE ""Email"" IS NOT NULL AND ""DeletedAtUtc"" IS NULL
    GROUP BY lower(""Email"")
    HAVING COUNT(*) > 1
  LOOP
    RAISE NOTICE 'AUTH MIGRATION: duplicate email needs manual review: % (% accounts)', r.email, r.cnt;
  END LOOP;
END $$;");

            migrationBuilder.CreateIndex(
                name: "IX_DomainUsers_Email",
                table: "DomainUsers",
                column: "Email",
                unique: true,
                filter: "\"IsEmailVerified\" AND \"Email\" IS NOT NULL AND \"DeletedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DomainUsers_GoogleId",
                table: "DomainUsers",
                column: "GoogleId",
                unique: true,
                filter: "\"GoogleId\" IS NOT NULL AND \"DeletedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DomainUsers_Phone",
                table: "DomainUsers",
                column: "Phone",
                unique: true,
                filter: "\"IsPhoneVerified\" AND \"DeletedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OtpCodes_Recipient_Channel_Purpose_CreatedAtUtc",
                table: "OtpCodes",
                columns: new[] { "Recipient", "Channel", "Purpose", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtpCodes");

            migrationBuilder.DropIndex(
                name: "IX_DomainUsers_Email",
                table: "DomainUsers");

            migrationBuilder.DropIndex(
                name: "IX_DomainUsers_GoogleId",
                table: "DomainUsers");

            migrationBuilder.DropIndex(
                name: "IX_DomainUsers_Phone",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "GoogleId",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "IsPhoneVerified",
                table: "DomainUsers");

            migrationBuilder.DropColumn(
                name: "ProfileResetAtUtc",
                table: "DomainUsers");

            migrationBuilder.CreateIndex(
                name: "IX_DomainUsers_Phone",
                table: "DomainUsers",
                column: "Phone",
                unique: true);
        }
    }
}
