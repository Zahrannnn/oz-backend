using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oz.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTrackingToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "tracking_token",
                table: "order",
                type: "varchar(64)",
                maxLength: 64,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "tracking_token",
                table: "order");
        }
    }
}
