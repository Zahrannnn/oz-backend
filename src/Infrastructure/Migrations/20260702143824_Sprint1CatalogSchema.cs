using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Oz.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Sprint1CatalogSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GradeStages_Schools_SchoolId",
                table: "GradeStages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Schools",
                table: "Schools");

            migrationBuilder.DropIndex(
                name: "IX_Schools_Slug",
                table: "Schools");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ItemTypes",
                table: "ItemTypes");

            migrationBuilder.DropIndex(
                name: "IX_ItemTypes_Slug",
                table: "ItemTypes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_GradeStages",
                table: "GradeStages");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ItemTypes");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "ItemTypes");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "ItemTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "ItemTypes");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "GradeStages");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "GradeStages");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "GradeStages");

            migrationBuilder.RenameTable(
                name: "Schools",
                newName: "school");

            migrationBuilder.RenameTable(
                name: "ItemTypes",
                newName: "item_type");

            migrationBuilder.RenameTable(
                name: "GradeStages",
                newName: "grade_stage");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "school",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "school",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "school",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "school",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "item_type",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "item_type",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "item_type",
                newName: "created_at");

            migrationBuilder.RenameIndex(
                name: "IX_ItemTypes_Name",
                table: "item_type",
                newName: "IX_item_type_name");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "grade_stage",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "grade_stage",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SchoolId",
                table: "grade_stage",
                newName: "school_id");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "grade_stage",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "SortOrder",
                table: "grade_stage",
                newName: "display_order");

            migrationBuilder.RenameIndex(
                name: "IX_GradeStages_SchoolId_Name",
                table: "grade_stage",
                newName: "IX_grade_stage_school_id_name");

            migrationBuilder.RenameIndex(
                name: "IX_GradeStages_SchoolId",
                table: "grade_stage",
                newName: "IX_grade_stage_school_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "school",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "school",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<bool>(
                name: "is_archived",
                table: "school",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte>(
                name: "type",
                table: "school",
                type: "tinyint",
                nullable: true);

            migrationBuilder.Sql("UPDATE [school] SET [type] = CAST(1 AS tinyint) WHERE [type] IS NULL");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "item_type",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                table: "grade_stage",
                type: "datetime2(3)",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_school",
                table: "school",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_item_type",
                table: "item_type",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_grade_stage",
                table: "grade_stage",
                column: "id");

            migrationBuilder.CreateTable(
                name: "product",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    school_id = table.Column<long>(type: "bigint", nullable: false),
                    grade_stage_id = table.Column<long>(type: "bigint", nullable: false),
                    item_type_id = table.Column<long>(type: "bigint", nullable: false),
                    gender = table.Column<byte>(type: "tinyint", nullable: false),
                    color = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    is_in_set = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    is_archived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product", x => x.id);
                    table.CheckConstraint("CK_product_gender", "[gender] IN (1, 2, 3)");
                    table.ForeignKey(
                        name: "FK_product_grade_stage_grade_stage_id",
                        column: x => x.grade_stage_id,
                        principalTable: "grade_stage",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_product_item_type_item_type_id",
                        column: x => x.item_type_id,
                        principalTable: "item_type",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_product_school_school_id",
                        column: x => x.school_id,
                        principalTable: "school",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "product_image",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    sort_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_image", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_image_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "variant",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    product_id = table.Column<long>(type: "bigint", nullable: false),
                    size_label = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    price_incl_vat = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    stock = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    reserved = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    low_stock_threshold = table.Column<int>(type: "int", nullable: false, defaultValue: 5),
                    is_archived = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    updated_at = table.Column<DateTime>(type: "datetime2(3)", nullable: false, defaultValueSql: "SYSUTCDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_variant", x => x.id);
                    table.CheckConstraint("CK_variant_stock_nonneg", "[stock] >= 0");
                    table.CheckConstraint("CK_variant_threshold_nonneg", "[low_stock_threshold] >= 0");
                    table.ForeignKey(
                        name: "FK_variant_product_product_id",
                        column: x => x.product_id,
                        principalTable: "product",
                        principalColumn: "id");
                });

            migrationBuilder.UpdateData(
                table: "school",
                keyColumn: "id",
                keyValue: 1L,
                column: "type",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "school",
                keyColumn: "id",
                keyValue: 2L,
                column: "type",
                value: (byte)2);

            migrationBuilder.UpdateData(
                table: "school",
                keyColumn: "id",
                keyValue: 3L,
                column: "type",
                value: (byte)1);

            migrationBuilder.AlterColumn<byte>(
                name: "type",
                table: "school",
                type: "tinyint",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_school_name",
                table: "school",
                column: "name",
                unique: true,
                filter: "[is_archived] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_school_type",
                table: "school",
                column: "type");

            migrationBuilder.AddCheckConstraint(
                name: "CK_school_type",
                table: "school",
                sql: "[type] BETWEEN 1 AND 6");

            migrationBuilder.CreateIndex(
                name: "IX_product_grade_stage_id",
                table: "product",
                column: "grade_stage_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_item_type_id",
                table: "product",
                column: "item_type_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_school_id_grade_stage_id",
                table: "product",
                columns: new[] { "school_id", "grade_stage_id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_school_id_grade_stage_id_gender",
                table: "product",
                columns: new[] { "school_id", "grade_stage_id", "gender" },
                filter: "[is_in_set] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_product_school_id_grade_stage_id_item_type_id_gender",
                table: "product",
                columns: new[] { "school_id", "grade_stage_id", "item_type_id", "gender" },
                unique: true,
                filter: "[is_archived] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_product_image_product_id_sort_order",
                table: "product_image",
                columns: new[] { "product_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_variant_product_id",
                table: "variant",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_variant_product_id_size_label",
                table: "variant",
                columns: new[] { "product_id", "size_label" },
                unique: true,
                filter: "[is_archived] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_variant_stock_low_stock_threshold",
                table: "variant",
                columns: new[] { "stock", "low_stock_threshold" },
                filter: "[is_archived] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_grade_stage_school_school_id",
                table: "grade_stage",
                column: "school_id",
                principalTable: "school",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_grade_stage_school_school_id",
                table: "grade_stage");

            migrationBuilder.DropTable(
                name: "product_image");

            migrationBuilder.DropTable(
                name: "variant");

            migrationBuilder.DropTable(
                name: "product");

            migrationBuilder.DropPrimaryKey(
                name: "PK_school",
                table: "school");

            migrationBuilder.DropIndex(
                name: "IX_school_name",
                table: "school");

            migrationBuilder.DropIndex(
                name: "IX_school_type",
                table: "school");

            migrationBuilder.DropCheckConstraint(
                name: "CK_school_type",
                table: "school");

            migrationBuilder.DropPrimaryKey(
                name: "PK_item_type",
                table: "item_type");

            migrationBuilder.DropPrimaryKey(
                name: "PK_grade_stage",
                table: "grade_stage");

            migrationBuilder.DropColumn(
                name: "is_archived",
                table: "school");

            migrationBuilder.DropColumn(
                name: "type",
                table: "school");

            migrationBuilder.RenameTable(
                name: "school",
                newName: "Schools");

            migrationBuilder.RenameTable(
                name: "item_type",
                newName: "ItemTypes");

            migrationBuilder.RenameTable(
                name: "grade_stage",
                newName: "GradeStages");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Schools",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Schools",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                table: "Schools",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "Schools",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "ItemTypes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "ItemTypes",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "ItemTypes",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_item_type_name",
                table: "ItemTypes",
                newName: "IX_ItemTypes_Name");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "GradeStages",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "GradeStages",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "school_id",
                table: "GradeStages",
                newName: "SchoolId");

            migrationBuilder.RenameColumn(
                name: "created_at",
                table: "GradeStages",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "display_order",
                table: "GradeStages",
                newName: "SortOrder");

            migrationBuilder.RenameIndex(
                name: "IX_grade_stage_school_id_name",
                table: "GradeStages",
                newName: "IX_GradeStages_SchoolId_Name");

            migrationBuilder.RenameIndex(
                name: "IX_grade_stage_school_id",
                table: "GradeStages",
                newName: "IX_GradeStages_SchoolId");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                table: "Schools",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "Schools",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Schools",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "Schools",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Schools",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "ItemTypes",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ItemTypes",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "ItemTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "ItemTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "ItemTypes",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAt",
                table: "GradeStages",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2(3)",
                oldDefaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "GradeStages",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "GradeStages",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "GradeStages",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Schools",
                table: "Schools",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ItemTypes",
                table: "ItemTypes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GradeStages",
                table: "GradeStages",
                column: "Id");

            migrationBuilder.UpdateData(
                table: "GradeStages",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "IsActive", "NameAr", "UpdatedAt" },
                values: new object[] { true, "كي جي 1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "GradeStages",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "IsActive", "NameAr", "UpdatedAt" },
                values: new object[] { true, "كي جي 2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "GradeStages",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "IsActive", "NameAr", "UpdatedAt" },
                values: new object[] { true, "كي جي 3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "GradeStages",
                keyColumn: "Id",
                keyValue: 4L,
                columns: new[] { "IsActive", "NameAr", "UpdatedAt" },
                values: new object[] { true, "صف اول", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "GradeStages",
                keyColumn: "Id",
                keyValue: 5L,
                columns: new[] { "IsActive", "NameAr", "UpdatedAt" },
                values: new object[] { true, "صف ثاني", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "GradeStages",
                keyColumn: "Id",
                keyValue: 6L,
                columns: new[] { "IsActive", "NameAr", "UpdatedAt" },
                values: new object[] { true, "صف ثالث", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "GradeStages",
                keyColumn: "Id",
                keyValue: 7L,
                columns: new[] { "IsActive", "NameAr", "UpdatedAt" },
                values: new object[] { true, "صف رابع", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "GradeStages",
                keyColumn: "Id",
                keyValue: 8L,
                columns: new[] { "IsActive", "NameAr", "UpdatedAt" },
                values: new object[] { true, "صف خامس", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "GradeStages",
                keyColumn: "Id",
                keyValue: 9L,
                columns: new[] { "IsActive", "NameAr", "UpdatedAt" },
                values: new object[] { true, "صف سادس", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "ItemTypes",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "IsActive", "NameAr", "Slug", "UpdatedAt" },
                values: new object[] { true, "تي شيرت", "t-shirt", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "ItemTypes",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "IsActive", "NameAr", "Slug", "UpdatedAt" },
                values: new object[] { true, "بولو", "polo", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "ItemTypes",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "IsActive", "NameAr", "Slug", "UpdatedAt" },
                values: new object[] { true, "قميص", "shirt", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "ItemTypes",
                keyColumn: "Id",
                keyValue: 4L,
                columns: new[] { "IsActive", "NameAr", "Slug", "UpdatedAt" },
                values: new object[] { true, "بنطلون", "trousers", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "ItemTypes",
                keyColumn: "Id",
                keyValue: 5L,
                columns: new[] { "IsActive", "NameAr", "Slug", "UpdatedAt" },
                values: new object[] { true, "شورت", "shorts", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "ItemTypes",
                keyColumn: "Id",
                keyValue: 6L,
                columns: new[] { "IsActive", "NameAr", "Slug", "UpdatedAt" },
                values: new object[] { true, "تنورة", "skirt", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "ItemTypes",
                keyColumn: "Id",
                keyValue: 7L,
                columns: new[] { "IsActive", "NameAr", "Slug", "UpdatedAt" },
                values: new object[] { true, "مريلة", "pinafore", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "ItemTypes",
                keyColumn: "Id",
                keyValue: 8L,
                columns: new[] { "IsActive", "NameAr", "Slug", "UpdatedAt" },
                values: new object[] { true, "سترة", "sweater", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "ItemTypes",
                keyColumn: "Id",
                keyValue: 9L,
                columns: new[] { "IsActive", "NameAr", "Slug", "UpdatedAt" },
                values: new object[] { true, "بدلة رياضية", "tracksuit", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "ItemTypes",
                keyColumn: "Id",
                keyValue: 10L,
                columns: new[] { "IsActive", "NameAr", "Slug", "UpdatedAt" },
                values: new object[] { true, "جوارب", "socks", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Schools",
                keyColumn: "Id",
                keyValue: 1L,
                columns: new[] { "IsActive", "NameAr", "Slug" },
                values: new object[] { true, "مدرسة القاهرة للغات", "cairo-language-school" });

            migrationBuilder.UpdateData(
                table: "Schools",
                keyColumn: "Id",
                keyValue: 2L,
                columns: new[] { "IsActive", "NameAr", "Slug" },
                values: new object[] { true, "مدرسة الإسكندرية التجريبية", "alexandria-experimental" });

            migrationBuilder.UpdateData(
                table: "Schools",
                keyColumn: "Id",
                keyValue: 3L,
                columns: new[] { "IsActive", "NameAr", "Slug" },
                values: new object[] { true, "مدرسة الجيزة العربية", "giza-arabic-school" });

            migrationBuilder.CreateIndex(
                name: "IX_Schools_Slug",
                table: "Schools",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItemTypes_Slug",
                table: "ItemTypes",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_GradeStages_Schools_SchoolId",
                table: "GradeStages",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id");
        }
    }
}
