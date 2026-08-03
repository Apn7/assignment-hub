using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssignmentHub.Infrastructure.Repositories;

/// <inheritdoc cref="IUserRepository"/>
public sealed class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        // No tracking: the login path only reads. Callers normalise the address, and
        // stored emails are lower-cased, so an exact match is correct here and uses
        // the unique index on Email.
        return _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Email == email, cancellationToken);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }
}
