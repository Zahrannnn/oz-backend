using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "exchange",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    order_item_id = table.Column<long>(type: "bigint", nullable: false),
                    old_variant_id = table.Column<long>(type: "bigint", nullable: false),
                    new_variant_id = table.Column<long>(type: "bigint", nullable: false),
                    qty = table.Column<int>(type: "int", nullable: false),
                    price_delta = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_exchange", x => x.id);
                    table.ForeignKey(
                        name: "FK_exchange_order_item_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_item",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_exchange_order_order_id",
                        column: x => x.order_id,
                        principalTable: "order",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_exchange_variant_new_variant_id",
                        column: x => x.new_variant_id,
                        principalTable: "variant",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_exchange_variant_old_variant_id",
                        column: x => x.old_variant_id,
                        principalTable: "variant",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_exchange_new_variant_id",
                table: "exchange",
                column: "new_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_exchange_old_variant_id",
                table: "exchange",
                column: "old_variant_id");

            migrationBuilder.CreateIndex(
                name: "IX_exchange_order_id",
                table: "exchange",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_exchange_order_item_id",
                table: "exchange",
                column: "order_item_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exchange");
        }
    }
}
