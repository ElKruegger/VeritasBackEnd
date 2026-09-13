using LuxuryStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuxuryStore.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(rt => rt.Id);

        builder.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(128); // SHA-256 hex string is 64 chars; padded for headroom
        builder.HasIndex(rt => rt.TokenHash).IsUnique();

        // IsActive is a get-only computed property (RevokedAt/ExpiresAt) —
        // EF Core's default conventions never map a property without a
        // setter, so no explicit Ignore() is needed here.

        // Cascade: deleting a user should invalidate every session/refresh
        // token they hold, not leave orphaned rows behind.
        builder.HasOne(rt => rt.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
