using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Infrastructure.Persistence;
using AssignmentHub.Infrastructure.Persistence.Seed;
using AssignmentHub.Infrastructure.Repositories;
using AssignmentHub.Infrastructure.Services;
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

        // Injectable clock. Token expiry uses it today; the submission deadline rules
        // will need it to be mockable in tests tomorrow.
        services.AddSingleton(TimeProvider.System);

        // Identity's PBKDF2 hasher, wrapped so Application sees only IPasswordHasher.
        // Both the seeder and the login path resolve this, so there is exactly one
        // hashing policy in the system.
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();

        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAssignmentRepository, AssignmentRepository>();
        services.AddScoped<ITeacherAssignmentRepository, TeacherAssignmentRepository>();
        services.AddScoped<ISubmissionRepository, SubmissionRepository>();

        services.AddScoped<DataSeeder>();

        return services;
    }
}
