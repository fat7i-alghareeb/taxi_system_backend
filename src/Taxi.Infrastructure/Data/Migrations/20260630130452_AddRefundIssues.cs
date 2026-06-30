using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RefundIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PassengerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentRefundId = table.Column<Guid>(type: "uuid", nullable: true),
                    TripCancellationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestType = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    CustomerReason = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Note = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RefundStatusSnapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    RefundAmountSnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    RefundCurrencySnapshot = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    ReviewStatus = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ReviewedByAdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    AdminNotes = table.Column<string>(type: "text", nullable: true),
                    WhatsAppOpened = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundIssues_DomainUsers_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "DomainUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundIssues_PaymentRefunds_PaymentRefundId",
                        column: x => x.PaymentRefundId,
                        principalTable: "PaymentRefunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundIssues_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundIssues_TripCancellations_TripCancellationId",
                        column: x => x.TripCancellationId,
                        principalTable: "TripCancellations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RefundIssues_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RefundIssues_PassengerId",
                table: "RefundIssues",
                column: "PassengerId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundIssues_PaymentId",
                table: "RefundIssues",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundIssues_PaymentRefundId",
                table: "RefundIssues",
                column: "PaymentRefundId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundIssues_ReviewStatus_CreatedAtUtc",
                table: "RefundIssues",
                columns: new[] { "ReviewStatus", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RefundIssues_TripCancellationId",
                table: "RefundIssues",
                column: "TripCancellationId");

            migrationBuilder.CreateIndex(
                name: "IX_RefundIssues_TripId",
                table: "RefundIssues",
                column: "TripId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefundIssues");
        }
    }
}
