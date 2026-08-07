using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oz.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class WipeOrdersForCleanStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                SET NOCOUNT ON;
                SET QUOTED_IDENTIFIER ON;

                UPDATE v
                    SET v.stock = v.stock + oi.qty,
                        v.updated_at = SYSUTCDATETIME()
                FROM variant AS v
                INNER JOIN order_item AS oi ON oi.variant_id = v.id
                INNER JOIN [order] AS o ON o.id = oi.order_id
                WHERE o.state <> 12;

                DELETE FROM email_log;
                DELETE FROM exchange;
                DELETE FROM audit_log WHERE action LIKE 'order.%';
                DELETE FROM order_item;
                DELETE FROM [order];
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
