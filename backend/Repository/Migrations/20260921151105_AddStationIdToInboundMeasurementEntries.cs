using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddStationIdToInboundMeasurementEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StationId",
                table: "InboundMeasurementEntries",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_InboundMeasurementEntries_StationId",
                table: "InboundMeasurementEntries",
                column: "StationId");

            migrationBuilder.AddForeignKey(
                name: "FK_InboundMeasurementEntries_Stations_StationId",
                table: "InboundMeasurementEntries",
                column: "StationId",
                principalTable: "Stations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InboundMeasurementEntries_Stations_StationId",
                table: "InboundMeasurementEntries");

            migrationBuilder.DropIndex(
                name: "IX_InboundMeasurementEntries_StationId",
                table: "InboundMeasurementEntries");

            migrationBuilder.DropColumn(
                name: "StationId",
                table: "InboundMeasurementEntries");
        }
    }
}
