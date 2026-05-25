using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyUserNameToSingleColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"DomainUsers\" ALTER COLUMN \"Name\" TYPE character varying(150) USING COALESCE(\"Name\"->>'En', 'System User');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"DomainUsers\" ALTER COLUMN \"Name\" TYPE jsonb USING jsonb_build_object('En', \"Name\", 'Ar', \"Name\", 'Nl', \"Name\");");
        }
    }
}
