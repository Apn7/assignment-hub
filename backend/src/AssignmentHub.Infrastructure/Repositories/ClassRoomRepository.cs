using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssignmentHub.Infrastructure.Repositories;

/// <inheritdoc cref="IClassRoomRepository"/>
public sealed class ClassRoomRepository : IClassRoomRepository
{
    private readonly AppDbContext _context;

    public ClassRoomRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<ClassRoom?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.ClassRooms
            .AsNoTracking()
            .SingleOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        // Case-insensitive comparison so "Class 9 – A" and "class 9 – a" are
        // treated as duplicates. EF translates this to ILIKE on PostgreSQL.
        return _context.ClassRooms.AnyAsync(
            c => c.Name.ToLower() == name.ToLower(),
            cancellationToken);
    }

    public async Task AddAsync(ClassRoom classRoom, CancellationToken cancellationToken = default)
    {
        _context.ClassRooms.Add(classRoom);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClassRoom>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ClassRooms
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }
}
