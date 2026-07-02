using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint2EmailLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<long>(type: "bigint", nullable: true),
                    variant_id = table.Column<long>(type: "bigint", nullable: true),
                    recipient = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    template = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    status = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)0),
                    error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_log", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_log_order_id",
                table: "email_log",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_log_status_created_at",
                table: "email_log",
                columns: new[] { "status", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_log");
        }
    }
}
