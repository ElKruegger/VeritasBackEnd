using LuxuryStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuxuryStore.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(150);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(180);
        builder.Property(p => p.Description).HasMaxLength(4000);
        builder.Property(p => p.BasePrice).HasPrecision(10, 2);

        builder.HasIndex(p => p.Slug).IsUnique();

        // Restrict, not Cascade: deleting a Brand/Category should never
        // silently wipe out every product that references it — the caller
        // has to reassign or explicitly delete those products first.
        builder.HasOne(p => p.Brand)
            .WithMany(b => b.Products)
            .HasForeignKey(p => p.BrandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
