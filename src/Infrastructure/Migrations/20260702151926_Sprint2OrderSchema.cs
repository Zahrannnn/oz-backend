using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint2OrderSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "order",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    tracking_token_hash = table.Column<byte[]>(type: "binary(32)", nullable: false),
                    state = table.Column<byte>(type: "tinyint", nullable: false),
                    channel = table.Column<byte>(type: "tinyint", nullable: false),
                    customer_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    customer_phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    customer_email = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    address_city = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    address_line = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    delivery_fee = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    total = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    bosta_tracking_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    state_changed_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    cancelled_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    delivered_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true),
                    picked_up_at = table.Column<DateTime>(type: "datetime2(3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order", x => x.id);
                    table.CheckConstraint("CK_order_channel", "[channel] IN (1, 2)");
                    table.CheckConstraint("CK_order_pickup_address", "[channel] = 2 AND [address_line] IS NULL OR [channel] = 1");
                    table.CheckConstraint("CK_order_state", "[state] IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)");
                });

            migrationBuilder.CreateTable(
                name: "order_item",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<long>(type: "bigint", nullable: false),
                    variant_id = table.Column<long>(type: "bigint", nullable: false),
                    qty = table.Column<int>(type: "int", nullable: false),
                    unit_price_snapshot = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    line_total_snapshot = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_item", x => x.id);
                    table.CheckConstraint("CK_order_item_qty", "[qty] > 0");
                    table.ForeignKey(
                        name: "FK_order_item_order_order_id",
                        column: x => x.order_id,
                        principalTable: "order",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_order_item_variant_variant_id",
                        column: x => x.variant_id,
                        principalTable: "variant",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_order_created_at",
                table: "order",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_order_customer_phone",
                table: "order",
                column: "customer_phone");

            migrationBuilder.CreateIndex(
                name: "IX_order_state",
                table: "order",
                column: "state");

            migrationBuilder.CreateIndex(
                name: "IX_order_state_state_changed_at",
                table: "order",
                columns: new[] { "state", "state_changed_at" },
                filter: "[state] IN (1, 2, 8)");

            migrationBuilder.CreateIndex(
                name: "IX_order_tracking_token_hash",
                table: "order",
                column: "tracking_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_item_order_id",
                table: "order_item",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_item_variant_id",
                table: "order_item",
                column: "variant_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_item");

            migrationBuilder.DropTable(
                name: "order");
        }
    }
}
