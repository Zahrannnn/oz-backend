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
            // Remap legacy values before seed updates:
            // 1 Arabic→3, 2 Experimental→2, 3 AzharEldelta→1 Governmental,
            // 4 ElHoda→6 Private, 5 ElTegara→6 Private, 6 Custom→6 Private
            migrationBuilder.Sql("""
                UPDATE school SET [type] = [type] + 100;
                UPDATE school SET [type] = CASE [type]
                    WHEN 101 THEN 3
                    WHEN 102 THEN 2
                    WHEN 103 THEN 1
                    WHEN 104 THEN 6
                    WHEN 105 THEN 6
                    WHEN 106 THEN 6
                    ELSE [type]
                END
                WHERE [type] BETWEEN 101 AND 106;
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
                UPDATE school SET [type] = [type] + 100;
                UPDATE school SET [type] = CASE [type]
                    WHEN 101 THEN 3
                    WHEN 102 THEN 2
                    WHEN 103 THEN 1
                    WHEN 104 THEN 2
                    WHEN 105 THEN 2
                    WHEN 106 THEN 4
                    ELSE [type]
                END
                WHERE [type] BETWEEN 101 AND 106;
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
