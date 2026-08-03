using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AssignmentHub.Application;

/// <summary>
/// Composition root for the Application layer. Keeping registration here means
/// the Api project never has to know which concrete services exist.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Picks up every AbstractValidator<T> in this assembly as it is added.
        services.AddValidatorsFromAssemblyContaining(typeof(DependencyInjection));

        // Application services (assignment, submission, grading, ...) are
        // registered here as they are introduced.

        return services;
    }
}
