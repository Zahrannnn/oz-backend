using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UnifySchoolTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE school SET [type] = CASE [type]
                    WHEN 1 THEN 3
                    WHEN 2 THEN 2
                    WHEN 3 THEN 1
                    WHEN 4 THEN 6
                    WHEN 5 THEN 6
                    WHEN 6 THEN 6
                    ELSE [type]
                END;
                """);

            migrationBuilder.UpdateData(
                table: "school",
                keyColumn: "id",
                keyValue: 1L,
                column: "type",
                value: (byte)4);

            migrationBuilder.UpdateData(
                table: "school",
                keyColumn: "id",
                keyValue: 3L,
                column: "type",
                value: (byte)3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE school SET [type] = CASE [type]
                    WHEN 3 THEN 1
                    WHEN 2 THEN 2
                    WHEN 1 THEN 3
                    WHEN 6 THEN 4
                    ELSE [type]
                END;
                """);

            migrationBuilder.UpdateData(
                table: "school",
                keyColumn: "id",
                keyValue: 1L,
                column: "type",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "school",
                keyColumn: "id",
                keyValue: 3L,
                column: "type",
                value: (byte)1);
        }
    }
}
