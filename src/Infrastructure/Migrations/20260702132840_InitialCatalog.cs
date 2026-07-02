using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Oz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItemTypes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Schools",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GradeStages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SchoolId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeStages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GradeStages_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "ItemTypes",
                columns: new[] { "Id", "CreatedAt", "IsActive", "Name", "NameAr", "Slug", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4016), true, "T-Shirt", "تي شيرت", "t-shirt", new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4392) },
                    { 2L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4758), true, "Polo", "بولو", "polo", new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4759) },
                    { 3L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4760), true, "Shirt", "قميص", "shirt", new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4760) },
                    { 4L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4762), true, "Trousers", "بنطلون", "trousers", new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4762) },
                    { 5L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4763), true, "Shorts", "شورت", "shorts", new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4764) },
                    { 6L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4765), true, "Skirt", "تنورة", "skirt", new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4765) },
                    { 7L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4766), true, "Pinafore", "مريلة", "pinafore", new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4767) },
                    { 8L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4768), true, "Sweater", "سترة", "sweater", new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4768) },
                    { 9L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4769), true, "Tracksuit", "بدلة رياضية", "tracksuit", new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4770) },
                    { 10L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4771), true, "Socks", "جوارب", "socks", new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(4771) }
                });

            migrationBuilder.InsertData(
                table: "Schools",
                columns: new[] { "Id", "CreatedAt", "IsActive", "Name", "NameAr", "Slug", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 7, 2, 13, 28, 39, 751, DateTimeKind.Utc).AddTicks(690), true, "Cairo Language School", "مدرسة القاهرة للغات", "cairo-language-school", new DateTime(2026, 7, 2, 13, 28, 39, 751, DateTimeKind.Utc).AddTicks(1071) },
                    { 2L, new DateTime(2026, 7, 2, 13, 28, 39, 751, DateTimeKind.Utc).AddTicks(1432), true, "Alexandria Experimental", "مدرسة الإسكندرية التجريبية", "alexandria-experimental", new DateTime(2026, 7, 2, 13, 28, 39, 751, DateTimeKind.Utc).AddTicks(1432) },
                    { 3L, new DateTime(2026, 7, 2, 13, 28, 39, 751, DateTimeKind.Utc).AddTicks(1434), true, "Giza Arabic School", "مدرسة الجيزة العربية", "giza-arabic-school", new DateTime(2026, 7, 2, 13, 28, 39, 751, DateTimeKind.Utc).AddTicks(1434) }
                });

            migrationBuilder.InsertData(
                table: "GradeStages",
                columns: new[] { "Id", "CreatedAt", "IsActive", "Name", "NameAr", "SchoolId", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(699), true, "KG1", "كي جي 1", 1L, 1, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1103) },
                    { 2L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1397), true, "KG2", "كي جي 2", 1L, 2, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1397) },
                    { 3L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1398), true, "KG3", "كي جي 3", 1L, 3, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1399) },
                    { 4L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1453), true, "FS1", "صف اول", 1L, 4, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1453) },
                    { 5L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1454), true, "FS2", "صف ثاني", 1L, 5, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1455) },
                    { 6L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1456), true, "FS3", "صف ثالث", 1L, 6, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1456) },
                    { 7L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1458), true, "FS4", "صف رابع", 1L, 7, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1458) },
                    { 8L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1459), true, "FS5", "صف خامس", 1L, 8, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1460) },
                    { 9L, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1461), true, "FS6", "صف سادس", 1L, 9, new DateTime(2026, 7, 2, 13, 28, 39, 752, DateTimeKind.Utc).AddTicks(1461) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_GradeStages_SchoolId",
                table: "GradeStages",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_GradeStages_SchoolId_Name",
                table: "GradeStages",
                columns: new[] { "SchoolId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemTypes_Name",
                table: "ItemTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemTypes_Slug",
                table: "ItemTypes",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schools_Slug",
                table: "Schools",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GradeStages");

            migrationBuilder.DropTable(
                name: "ItemTypes");

            migrationBuilder.DropTable(
                name: "Schools");
        }
    }
}
