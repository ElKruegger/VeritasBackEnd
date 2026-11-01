using LuxuryStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LuxuryStore.Infrastructure.Migrations
{
    /// <summary>
    /// Enables Postgres Row Level Security on every table in the public
    /// schema (Supabase Security Advisor: "RLS Disabled in Public").
    ///
    /// Why: Supabase exposes the public schema through its auto-generated
    /// Data API (PostgREST). Anyone holding the project URL + the anon key
    /// (which is designed to be public) could read and write these tables
    /// directly, bypassing this API, its validation and its auth entirely.
    ///
    /// RLS is enabled with NO policies on purpose. For the anon/authenticated
    /// roles PostgREST uses, "RLS on + zero policies" means zero rows visible
    /// and every write rejected. This API is unaffected: it connects as the
    /// postgres role, which owns these tables (EF created them), and a table
    /// owner bypasses RLS unless FORCE ROW LEVEL SECURITY is set — it isn't.
    ///
    /// Plain SQL only (no Supabase-specific roles referenced), so this
    /// migration also applies cleanly to a vanilla Postgres, e.g. a local
    /// Docker database or a CI service container.
    ///
    /// Hand-written (no model change, so no Designer/BuildTargetModel and
    /// no snapshot change are needed) — the two attributes below are what
    /// EF uses to discover a migration; normally they live in the
    /// generated .Designer.cs partial.
    ///
    /// RULE FOR EVERY FUTURE MIGRATION THAT CREATES A TABLE (CartItems,
    /// Orders, OrderItems, Payments, ...): add the same
    /// ALTER TABLE ... ENABLE ROW LEVEL SECURITY line in its Up(), or the
    /// Advisor will flag it again and the new table will be exposed.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261008230000_EnableRowLevelSecurity")]
    public class EnableRowLevelSecurity : Migration
    {
        private static readonly string[] Tables =
        [
            "Brands",
            "Categories",
            "Products",
            "ProductVariants",
            "Users",
            "Addresses",
            "RefreshTokens",
            "__EFMigrationsHistory"
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
                migrationBuilder.Sql($"ALTER TABLE public.\"{table}\" ENABLE ROW LEVEL SECURITY;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var table in Tables)
                migrationBuilder.Sql($"ALTER TABLE public.\"{table}\" DISABLE ROW LEVEL SECURITY;");
        }
    }
}
