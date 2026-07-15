using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oz.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderNumber : Migration
    {
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "order_number",
            table: "order",
            type: "varchar(20)",
            maxLength: 20,
            nullable: false,
            defaultValue: "");

        migrationBuilder.Sql("""
            UPDATE [order]
            SET order_number = 'OZ-' + UPPER(SUBSTRING(CONVERT(varchar(36), NEWID()), 1, 8))
            WHERE order_number = ''
        """);

        migrationBuilder.CreateIndex(
            name: "IX_order_order_number",
            table: "order",
            column: "order_number",
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_order_order_number",
            table: "order");

        migrationBuilder.DropColumn(
            name: "order_number",
            table: "order");
    }
    }
}
