using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AssignmentHub.Infrastructure.Repositories;

/// <inheritdoc cref="ISubjectRepository"/>
public sealed class SubjectRepository : ISubjectRepository
{
    private readonly AppDbContext _context;

    public SubjectRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Subject?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Subjects
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return _context.Subjects.AnyAsync(
            s => s.Name.ToLower() == name.ToLower(),
            cancellationToken);
    }

    public async Task AddAsync(Subject subject, CancellationToken cancellationToken = default)
    {
        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subject>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Subjects
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }
}
