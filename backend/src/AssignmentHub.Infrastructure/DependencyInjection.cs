using AssignmentHub.Domain.Entities;
using AssignmentHub.Infrastructure.Persistence;
using AssignmentHub.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentHub.Infrastructure;

/// <summary>
/// Composition root for the Infrastructure layer: database access and any other
/// implementation of an Application-layer interface.
/// </summary>
public static class DependencyInjection
{
    /// <summary>Name of the connection string this layer expects.</summary>
    public const string ConnectionStringName = "Default";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{ConnectionStringName}' is not configured. " +
                "Set ConnectionStrings__Default in the environment (see .env.example) " +
                "or add it to appsettings.Development.json.");
        }

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        // Identity.Core's PBKDF2 hasher. Registered for the seeder now and reused
        // by login/registration later, so there is only ever one hashing policy.
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        services.AddScoped<DataSeeder>();

        // Repositories and other Application-interface implementations are
        // registered here as they are introduced.

        return services;
    }
}
