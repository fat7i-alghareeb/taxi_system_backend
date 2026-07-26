using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundIssueSingleOpenPerTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: this runs against a fresh database. If it is ever applied to one that already
            // contains data, CREATE UNIQUE INDEX will abort on the duplicate Open/InReview rows the
            // missing guard allowed, and the deploy will fail. In that case a dedupe step has to
            // run first — dismiss all but the OLDEST open issue per (TripId, PassengerId), keeping
            // the oldest because it carries the customer's original submission date.
            migrationBuilder.CreateIndex(
                name: "IX_RefundIssues_TripId_PassengerId_Open",
                table: "RefundIssues",
                columns: new[] { "TripId", "PassengerId" },
                unique: true,
                filter: "\"ReviewStatus\" IN ('Open', 'InReview')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefundIssues_TripId_PassengerId_Open",
                table: "RefundIssues");
        }
    }
}
