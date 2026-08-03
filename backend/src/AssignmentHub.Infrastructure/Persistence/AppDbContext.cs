using Microsoft.EntityFrameworkCore;

namespace AssignmentHub.Infrastructure.Persistence;

/// <summary>
/// EF Core context for the Assignment Hub schema.
/// </summary>
/// <remarks>
/// Intentionally empty for now: no entities have been modelled yet. The context
/// exists so the connection string, DI registration and migration tooling can be
/// verified before the domain model lands. Entity configurations go in
/// <c>Persistence/Configurations</c> and are discovered automatically.
/// </remarks>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
