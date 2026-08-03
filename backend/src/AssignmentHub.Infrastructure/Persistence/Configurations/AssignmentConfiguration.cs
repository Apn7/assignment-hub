using AssignmentHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentHub.Infrastructure.Persistence.Configurations;

public sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(200);

        // Free-form and potentially long: no length cap.
        builder.Property(a => a.Description)
            .IsRequired()
            .HasColumnType("text");

        builder.Property(a => a.Deadline)
            .IsRequired();

        builder.Property(a => a.MaxMarks)
            .IsRequired();

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .IsRequired();

        // Students list assignments by class, filtered to Published.
        builder.HasIndex(a => new { a.ClassRoomId, a.Status });

        builder.HasOne(a => a.ClassRoom)
            .WithMany()
            .HasForeignKey(a => a.ClassRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Subject)
            .WithMany()
            .HasForeignKey(a => a.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CreatedByTeacher)
            .WithMany()
            .HasForeignKey(a => a.CreatedByTeacherId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
