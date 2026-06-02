using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceCounters",
                columns: table => new
                {
                    YearMonth = table.Column<string>(type: "character(6)", fixedLength: true, maxLength: 6, nullable: false),
                    NextSequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceCounters", x => x.YearMonth);
                });

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TripId = table.Column<Guid>(type: "uuid", nullable: false),
                    PassengerId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxRate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PaymentReference = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PaidAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IssuerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IssuerAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IssuerVatNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TripReferenceCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TripCompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DistanceKm = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                    DurationMin = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    VehicleTypeName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PassengerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    StopsJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModifiedUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_DomainUsers_PassengerId",
                        column: x => x.PassengerId,
                        principalTable: "DomainUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Invoices_Trips_TripId",
                        column: x => x.TripId,
                        principalTable: "Trips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceNumber",
                table: "Invoices",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_PassengerId",
                table: "Invoices",
                column: "PassengerId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TripId",
                table: "Invoices",
                column: "TripId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceCounters");

            migrationBuilder.DropTable(
                name: "Invoices");
        }
    }
}
