using LuxuryStore.Domain.Entities;
using LuxuryStore.Infrastructure.Persistence;
using LuxuryStore.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace LuxuryStore.UnitTests.Infrastructure;

/// <summary>
/// Checks the SQL that EF Core + Npgsql generate, without a database:
/// ToQueryString only translates, it never opens a connection. Catches
/// translation-level mistakes; what the rows look like still needs the
/// planned integration tests.
/// </summary>
public class ProductRepositoryQueryTests
{
    private static string SearchSql(string search)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql("Host=unused;Database=unused").Options;
        using var db = new AppDbContext(options);

        return ProductRepository
            .ApplySlice(db.Set<Product>().AsNoTracking(), categoryId: null, brandId: null, search)
            .ToQueryString();
    }

    [Fact]
    public void Search_declares_backslash_as_the_escape_character_so_wildcards_stay_literal()
    {
        var sql = SearchSql("100%");

        // Without an explicit escape character Npgsql emits ESCAPE '' — escaping
        // off — and a search for "%" would look for a literal backslash instead.
        sql.ShouldContain(@"ESCAPE '\'");
        sql.ShouldNotContain("ESCAPE ''");
    }

    [Fact]
    public void Search_matches_the_product_name_or_the_brand_name()
    {
        var sql = SearchSql("zegna");

        sql.ShouldContain(@"p.""Name"" ILIKE");
        sql.ShouldContain(@"b.""Name"" ILIKE");
    }
}
