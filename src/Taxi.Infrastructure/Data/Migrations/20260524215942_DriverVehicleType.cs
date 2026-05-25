using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Taxi.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DriverVehicleType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_Vehicles_ActiveVehicleId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_ActiveVehicleId",
                table: "Drivers");

            migrationBuilder.AddColumn<Guid>(
                name: "VehicleTypeId",
                table: "Drivers",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Drivers" d
                SET "VehicleTypeId" = v."VehicleTypeId"
                FROM "Vehicles" v
                WHERE d."ActiveVehicleId" = v."Id";
                """);

            migrationBuilder.DropColumn(
                name: "ActiveVehicleId",
                table: "Drivers");

            migrationBuilder.CreateIndex(
                table: "Drivers",
                name: "IX_Drivers_VehicleTypeId",
                column: "VehicleTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_VehicleTypes_VehicleTypeId",
                table: "Drivers",
                column: "VehicleTypeId",
                principalTable: "VehicleTypes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drivers_VehicleTypes_VehicleTypeId",
                table: "Drivers");

            migrationBuilder.DropIndex(
                name: "IX_Drivers_VehicleTypeId",
                table: "Drivers");

            migrationBuilder.AddColumn<Guid>(
                name: "ActiveVehicleId",
                table: "Drivers",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Drivers" d
                SET "ActiveVehicleId" = v."Id"
                FROM "Vehicles" v
                WHERE d."VehicleTypeId" = v."VehicleTypeId"
                  AND d."Id" = v."DriverId";
                """);

            migrationBuilder.DropColumn(
                name: "VehicleTypeId",
                table: "Drivers");

            migrationBuilder.CreateIndex(
                table: "Drivers",
                name: "IX_Drivers_ActiveVehicleId",
                column: "ActiveVehicleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drivers_Vehicles_ActiveVehicleId",
                table: "Drivers",
                column: "ActiveVehicleId",
                principalTable: "Vehicles",
                principalColumn: "Id");
        }
    }
}
