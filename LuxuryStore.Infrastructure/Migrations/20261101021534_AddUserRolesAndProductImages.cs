using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuxuryStore.Infrastructure.Migrations
{
    /// <summary>
    /// Adds Users.Role (UserRole stored as text) for the AdminOnly policy, and
    /// replaces ProductVariants.ImageUrls (text[]) with a ProductImages table
    /// (alt text, ordering, product- or variant-level images). Scaffolded by EF
    /// Core, then edited by hand: Role backfill value, RLS on the new table,
    /// and the data copy from the old column (steps marked below).
    /// </summary>
    public partial class AddUserRolesAndProductImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Role: existing accounts become Customer. The generated
            //    defaultValue was "" — not a valid UserRole name, so EF would
            //    throw on the first read of any pre-existing user.
            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "Users",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Customer");

            // 2) ProductImages table (replaces ProductVariants.ImageUrls).
            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    AltText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_ProductVariants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "ProductVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_SortOrder",
                table: "ProductImages",
                columns: new[] { "ProductId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_VariantId",
                table: "ProductImages",
                column: "VariantId");

            // Supabase rule (see 20261008230000_EnableRowLevelSecurity):
            // every new table gets RLS, or it is readable/writable by anyone
            // with the anon key through the auto-generated Data API.
            migrationBuilder.Sql("ALTER TABLE public.\"ProductImages\" ENABLE ROW LEVEL SECURITY;");

            // 3) Carry over any URLs stored in the old text[] column BEFORE
            //    dropping it. They were per-variant, so they stay
            //    variant-specific (VariantId set); array position becomes
            //    SortOrder. gen_random_uuid() is built into Postgres 13+, no
            //    extension needed. A no-op on a database without images.
            migrationBuilder.Sql(@"
INSERT INTO ""ProductImages"" (""Id"", ""Path"", ""AltText"", ""SortOrder"", ""ProductId"", ""VariantId"", ""CreatedAt"")
SELECT gen_random_uuid(),
       btrim(u.url),
       left(p.""Name"" || ' — ' || v.""VariantLabel"", 200),
       (u.ord - 1)::int,
       v.""ProductId"",
       v.""Id"",
       now()
FROM ""ProductVariants"" v
JOIN ""Products"" p ON p.""Id"" = v.""ProductId""
CROSS JOIN LATERAL unnest(v.""ImageUrls"") WITH ORDINALITY AS u(url, ord)
WHERE u.url IS NOT NULL AND btrim(u.url) <> '';");

            migrationBuilder.DropColumn(
                name: "ImageUrls",
                table: "ProductVariants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore the text[] column (empty by default, so the NOT NULL
            // add succeeds on a populated table) and copy variant-specific
            // images back into it. Product-level images (VariantId NULL) had
            // no home in the old model and are lost on downgrade.
            migrationBuilder.AddColumn<List<string>>(
                name: "ImageUrls",
                table: "ProductVariants",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]");

            migrationBuilder.Sql(@"
UPDATE ""ProductVariants"" v
SET ""ImageUrls"" = sub.urls
FROM (
    SELECT ""VariantId"", array_agg(""Path"" ORDER BY ""SortOrder"") AS urls
    FROM ""ProductImages""
    WHERE ""VariantId"" IS NOT NULL
    GROUP BY ""VariantId""
) sub
WHERE v.""Id"" = sub.""VariantId"";");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }
    }
}
