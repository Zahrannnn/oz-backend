using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oz.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropDeadSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_key");

            migrationBuilder.DropIndex(
                name: "IX_email_log_order_id",
                table: "email_log");

            migrationBuilder.DropColumn(
                name: "order_id",
                table: "email_log");

            migrationBuilder.DropColumn(
                name: "variant_id",
                table: "email_log");

            migrationBuilder.DropColumn(
                name: "password_salt",
                table: "admin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "order_id",
                table: "email_log",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "variant_id",
                table: "email_log",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "password_salt",
                table: "admin",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "idempotency_key",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    expires_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false),
                    key = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    request_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    response_body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    response_status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_key", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_log_order_id",
                table: "email_log",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_key_key",
                table: "idempotency_key",
                column: "key",
                unique: true);
        }
    }
}
