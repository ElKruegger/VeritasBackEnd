using LuxuryStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuxuryStore.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Sku).IsRequired().HasMaxLength(50);
        builder.Property(v => v.VariantLabel).IsRequired().HasMaxLength(50);
        builder.Property(v => v.PriceOverride).HasPrecision(10, 2);

        // ImageUrls (List<string>) is left unconfigured on purpose — the
        // Npgsql EF Core provider maps it to a native Postgres array column
        // (text[]) by convention, no value converter needed.

        builder.HasIndex(v => v.Sku).IsUnique();

        // Cascade here (unlike Product -> Brand/Category): a variant has no
        // meaning without its product, so deleting the product should take
        // its variants with it.
        builder.HasOne(v => v.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
