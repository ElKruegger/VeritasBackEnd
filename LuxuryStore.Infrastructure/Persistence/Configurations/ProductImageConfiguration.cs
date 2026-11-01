using LuxuryStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuxuryStore.Infrastructure.Persistence.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Path).IsRequired().HasMaxLength(500);
        builder.Property(i => i.AltText).IsRequired().HasMaxLength(200);

        // Every read of a gallery is "images of product X, in display order"
        // — this composite index serves exactly that query (and the
        // max-SortOrder lookup when appending a new image).
        builder.HasIndex(i => new { i.ProductId, i.SortOrder });

        // Cascade on both: an image has no meaning without its product, and a
        // variant-specific image has no meaning without its variant.
        // (Two cascade paths into one table is fine on Postgres; SQL Server
        // would reject it, which only matters if the provider ever changes.)
        builder.HasOne(i => i.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Variant)
            .WithMany(v => v.Images)
            .HasForeignKey(i => i.VariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
