using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint4BostaTrackingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "cod_failed_at",
                table: "order",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "handed_to_courier_at",
                table: "order",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "in_transit_at",
                table: "order",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "returned_at",
                table: "order",
                type: "datetime2(3)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_bosta_tracking_id",
                table: "order",
                column: "bosta_tracking_id",
                unique: true,
                filter: "[bosta_tracking_id] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_order_bosta_tracking_id",
                table: "order");

            migrationBuilder.DropColumn(
                name: "cod_failed_at",
                table: "order");

            migrationBuilder.DropColumn(
                name: "handed_to_courier_at",
                table: "order");

            migrationBuilder.DropColumn(
                name: "in_transit_at",
                table: "order");

            migrationBuilder.DropColumn(
                name: "returned_at",
                table: "order");
        }
    }
}
