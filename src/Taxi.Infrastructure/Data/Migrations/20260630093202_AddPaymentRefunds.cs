using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentRefunds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentRefunds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: true),
                    TripCancellationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerIncidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    TripCompensationClaimId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedByAdminId = table.Column<Guid>(type: "uuid", nullable: true),
                    PassengerId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    RefundPercent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true),
                    IsFullRefund = table.Column<bool>(type: "boolean", nullable: false),
                    OriginalPaymentAmountSnapshot = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    StripeRefundId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripePaymentIntentId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    StripeChargeId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    FailureCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SafeCustomerFailureMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequiresAdminAction = table.Column<bool>(type: "boolean", nullable: false),
                    CanRetry = table.Column<bool>(type: "boolean", nullable: false),
                    RetryBlockedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastStripeEventId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    AdminNote = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRefunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_AdminProfiles_RequestedByAdminId",
                        column: x => x.RequestedByAdminId,
                        principalTable: "AdminProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_CustomerIncidents_CustomerIncidentId",
                        column: x => x.CustomerIncidentId,
                        principalTable: "CustomerIncidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_DomainUsers_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "DomainUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_TripCancellations_TripCancellationId",
                        column: x => x.TripCancellationId,
                        principalTable: "TripCancellations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_TripCompensationClaims_TripCompensationClaim~",
                        column: x => x.TripCompensationClaimId,
                        principalTable: "TripCompensationClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PaymentRefunds_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_CustomerIncidentId",
                table: "PaymentRefunds",
                column: "CustomerIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_IdempotencyKey",
                table: "PaymentRefunds",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_PassengerId",
                table: "PaymentRefunds",
                column: "PassengerId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_PaymentId",
                table: "PaymentRefunds",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_RequestedByAdminId",
                table: "PaymentRefunds",
                column: "RequestedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_RequiresAdminAction",
                table: "PaymentRefunds",
                column: "RequiresAdminAction");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_Status",
                table: "PaymentRefunds",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_StripeRefundId",
                table: "PaymentRefunds",
                column: "StripeRefundId",
                unique: true,
                filter: "\"StripeRefundId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_TripCancellationId",
                table: "PaymentRefunds",
                column: "TripCancellationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_TripCompensationClaimId",
                table: "PaymentRefunds",
                column: "TripCompensationClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRefunds_TripId",
                table: "PaymentRefunds",
                column: "TripId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentRefunds");
        }
    }
}
