using AssignmentHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentHub.Infrastructure.Persistence.Configurations;

public sealed class TeacherAssignmentConfiguration : IEntityTypeConfiguration<TeacherAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherAssignment> builder)
    {
        builder.ToTable("TeacherAssignments");

        builder.HasKey(ta => ta.Id);

        // One row per teacher/class/subject combination.
        builder.HasIndex(ta => new { ta.TeacherId, ta.ClassRoomId, ta.SubjectId })
            .IsUnique();

        // WithMany() with no navigation on purpose: User carries no inverse
        // collection, so the relationship stays unambiguous alongside the other
        // User foreign keys elsewhere in the model.
        builder.HasOne(ta => ta.Teacher)
            .WithMany()
            .HasForeignKey(ta => ta.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ta => ta.ClassRoom)
            .WithMany()
            .HasForeignKey(ta => ta.ClassRoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ta => ta.Subject)
            .WithMany()
            .HasForeignKey(ta => ta.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
