using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyKeyTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "idempotency_key",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    key = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    request_hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    response_status = table.Column<int>(type: "int", nullable: false),
                    response_body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    expires_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_key", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_idempotency_key_key",
                table: "idempotency_key",
                column: "key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_key");
        }
    }
}
