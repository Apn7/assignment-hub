using AssignmentHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentHub.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        // Email is the login identifier, so uniqueness is a correctness
        // requirement rather than a lookup optimisation.
        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.HasOne(u => u.ClassRoom)
            .WithMany(c => c.Students)
            .HasForeignKey(u => u.ClassRoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
