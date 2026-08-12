using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;
using AssignmentHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AssignmentHub.Infrastructure.Repositories;

/// <inheritdoc cref="IUserRepository"/>
public sealed class UserRepository : IUserRepository
{
    /// <summary>
    /// PostgreSQL's SQLSTATE for unique_violation. The only relevant unique index
    /// on Users is (Email), so a 23505 raised while inserting a user can only mean
    /// an email collision.
    /// </summary>
    private const string UniqueViolation = "23505";

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

    public async Task<bool> TryAddAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _context.Entry(user).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<IReadOnlyList<User>> ListAsync(
        UserRole? roleFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .AsNoTracking()
            .Include(u => u.ClassRoom);

        IQueryable<User> filtered = roleFilter.HasValue
            ? query.Where(u => u.Role == roleFilter.Value)
            : query;

        return await filtered
            .OrderBy(u => u.FullName)
            .ToListAsync(cancellationToken);
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: UniqueViolation };
}

