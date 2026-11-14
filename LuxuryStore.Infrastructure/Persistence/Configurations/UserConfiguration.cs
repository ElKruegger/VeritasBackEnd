using LuxuryStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LuxuryStore.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);

        // Stored as text ("Customer"/"Admin"), not as the enum's int value —
        // readable in Supabase Studio and safe against enum reordering (see
        // UserRole). The migration that added this column backfills existing
        // rows with 'Customer'.
        builder.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Emails are normalized to lowercase before being persisted (see
        // AuthService/UserRepository), so a plain unique index is enough —
        // no need for a separate NormalizedEmail column like full ASP.NET
        // Core Identity uses.
        builder.HasIndex(u => u.Email).IsUnique();

        // Existing rows start unlocked with no failed attempts.
        builder.Property(u => u.FailedLoginCount).IsRequired().HasDefaultValue(0);
        builder.Property(u => u.LockoutEndsAt);
    }
}
