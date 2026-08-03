using AssignmentHub.Domain.Entities;
using AssignmentHub.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;

namespace AssignmentHub.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Assignment Hub schema.
/// </summary>
/// <remarks>
/// Entity shape lives in <c>Persistence/Configurations</c> as
/// <see cref="IEntityTypeConfiguration{TEntity}"/> classes, discovered
/// automatically, so the Domain entities stay free of persistence annotations.
/// </remarks>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<ClassRoom> ClassRooms => Set<ClassRoom>();

    public DbSet<Subject> Subjects => Set<Subject>();

    public DbSet<TeacherAssignment> TeacherAssignments => Set<TeacherAssignment>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<Submission> Submissions => Set<Submission>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Applies to DateTime and DateTime? alike, so every temporal column is
        // timestamptz and every value is UTC without per-property opt-in.
        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>()
            .HaveColumnType("timestamptz");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
