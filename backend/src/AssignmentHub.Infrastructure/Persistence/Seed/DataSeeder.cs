using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AssignmentHub.Infrastructure.Persistence.Seed;

/// <summary>
/// Populates a fresh database with a workable demo dataset: one account per role,
/// two classes, three subjects, and assignments covering every status the UI has
/// to render.
/// </summary>
/// <remarks>
/// Idempotent and Development-only. Invoked from <c>Program.cs</c>, not from
/// <c>OnModelCreating</c>, because passwords have to be hashed at runtime and EF
/// Core's <c>HasData</c> cannot express that. Hashing goes through the same
/// <see cref="IPasswordHasher"/> the login path uses, so seeded credentials are
/// guaranteed verifiable.
/// </remarks>
public sealed class DataSeeder
{
    // Demo passwords. Development fixtures only; documented in docs/database.md.
    // These are the credentials the evaluator logs in with, so they are
    // deliberately committed. They never reach a non-Development database.
    private const string AdminPassword = "Admin#1234";
    private const string TeacherPassword = "Teacher#1234";
    private const string StudentPassword = "Student#1234";

    // Deterministic ids so a re-seeded database always looks the same, which keeps
    // docs, manual API calls and frontend fixtures stable.
    private static readonly Guid AdminId = new("10000000-0000-0000-0000-000000000001");
    private static readonly Guid Teacher1Id = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid Teacher2Id = new("20000000-0000-0000-0000-000000000002");
    private static readonly Guid Student1Id = new("30000000-0000-0000-0000-000000000001");
    private static readonly Guid Student2Id = new("30000000-0000-0000-0000-000000000002");
    private static readonly Guid Student3Id = new("30000000-0000-0000-0000-000000000003");
    private static readonly Guid Student4Id = new("30000000-0000-0000-0000-000000000004");
    private static readonly Guid Class9AId = new("40000000-0000-0000-0000-000000000001");
    private static readonly Guid Class10AId = new("40000000-0000-0000-0000-000000000002");
    private static readonly Guid PhysicsId = new("50000000-0000-0000-0000-000000000001");
    private static readonly Guid MathematicsId = new("50000000-0000-0000-0000-000000000002");
    private static readonly Guid EnglishId = new("50000000-0000-0000-0000-000000000003");
    private static readonly Guid DraftAssignmentId = new("60000000-0000-0000-0000-000000000001");
    private static readonly Guid OpenAssignmentId = new("60000000-0000-0000-0000-000000000002");
    private static readonly Guid ClosedAssignmentId = new("60000000-0000-0000-0000-000000000003");
    private static readonly Guid UngradedSubmissionId = new("70000000-0000-0000-0000-000000000001");
    private static readonly Guid GradedSubmissionId = new("70000000-0000-0000-0000-000000000002");

    private readonly AppDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(
        AppDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<DataSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var pending = (await _context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        if (pending.Count > 0)
        {
            _logger.LogWarning(
                "Seed skipped: {Count} migration(s) not applied. Run `dotnet ef database update` first.",
                pending.Count);
            return;
        }

        // The idempotency guard: presence of any user means this database has
        // already been seeded, so a restart must not duplicate anything.
        if (await _context.Users.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Seed skipped: the database already contains users.");
            return;
        }

        var now = DateTime.UtcNow;

        User CreateUser(Guid id, string fullName, string email, UserRole role, string password, Guid? classRoomId = null)
        {
            var user = new User
            {
                Id = id,
                FullName = fullName,
                Email = email,
                Role = role,
                ClassRoomId = classRoomId,
                CreatedAt = now
            };

            user.PasswordHash = _passwordHasher.Hash(password);
            return user;
        }

        var classRooms = new[]
        {
            new ClassRoom { Id = Class9AId, Name = "Class 9 – A" },
            new ClassRoom { Id = Class10AId, Name = "Class 10 – A" }
        };

        var subjects = new[]
        {
            new Subject { Id = PhysicsId, Name = "Physics" },
            new Subject { Id = MathematicsId, Name = "Mathematics" },
            new Subject { Id = EnglishId, Name = "English" }
        };

        var users = new[]
        {
            CreateUser(AdminId, "System Administrator", "admin@assignmenthub.local", UserRole.Admin, AdminPassword),
            CreateUser(Teacher1Id, "Ayesha Rahman", "teacher1@assignmenthub.local", UserRole.Teacher, TeacherPassword),
            CreateUser(Teacher2Id, "Imran Hossain", "teacher2@assignmenthub.local", UserRole.Teacher, TeacherPassword),
            CreateUser(Student1Id, "Nabila Akter", "student1@assignmenthub.local", UserRole.Student, StudentPassword, Class9AId),
            CreateUser(Student2Id, "Tanvir Ahmed", "student2@assignmenthub.local", UserRole.Student, StudentPassword, Class9AId),
            CreateUser(Student3Id, "Farhan Kabir", "student3@assignmenthub.local", UserRole.Student, StudentPassword, Class10AId),
            CreateUser(Student4Id, "Sadia Islam", "student4@assignmenthub.local", UserRole.Student, StudentPassword, Class10AId)
        };

        var teacherAssignments = new[]
        {
            new TeacherAssignment { Id = new("80000000-0000-0000-0000-000000000001"), TeacherId = Teacher1Id, ClassRoomId = Class9AId, SubjectId = PhysicsId },
            new TeacherAssignment { Id = new("80000000-0000-0000-0000-000000000002"), TeacherId = Teacher1Id, ClassRoomId = Class9AId, SubjectId = MathematicsId },
            new TeacherAssignment { Id = new("80000000-0000-0000-0000-000000000003"), TeacherId = Teacher2Id, ClassRoomId = Class10AId, SubjectId = EnglishId },
            new TeacherAssignment { Id = new("80000000-0000-0000-0000-000000000004"), TeacherId = Teacher2Id, ClassRoomId = Class9AId, SubjectId = EnglishId }
        };

        var assignments = new[]
        {
            // Not visible to students: exercises the Draft filter.
            new Assignment
            {
                Id = DraftAssignmentId,
                Title = "Newton's Laws of Motion – Worksheet 1",
                Description = "Work through problems 1-10 from chapter 4. Show every step of your reasoning.",
                ClassRoomId = Class9AId,
                SubjectId = PhysicsId,
                CreatedByTeacherId = Teacher1Id,
                Deadline = now.AddDays(14),
                MaxMarks = 20,
                Status = AssignmentStatus.Draft,
                CreatedAt = now,
                UpdatedAt = now
            },
            // Open: students may still submit and edit.
            new Assignment
            {
                Id = OpenAssignmentId,
                Title = "Kinematics Problem Set",
                Description = "Solve the five kinematics problems attached in class. State assumptions where the question is ambiguous.",
                ClassRoomId = Class9AId,
                SubjectId = PhysicsId,
                CreatedByTeacherId = Teacher1Id,
                Deadline = now.AddDays(7),
                MaxMarks = 50,
                Status = AssignmentStatus.Published,
                CreatedAt = now.AddDays(-5),
                UpdatedAt = now.AddDays(-5)
            },
            // Deadline passed: exercises the "no edits after deadline" rule.
            new Assignment
            {
                Id = ClosedAssignmentId,
                Title = "Quadratic Equations Revision",
                Description = "Complete the revision sheet on solving quadratic equations by factorisation and by formula.",
                ClassRoomId = Class9AId,
                SubjectId = MathematicsId,
                CreatedByTeacherId = Teacher1Id,
                Deadline = now.AddDays(-3),
                MaxMarks = 30,
                Status = AssignmentStatus.Published,
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-20)
            }
        };

        var submissions = new[]
        {
            new Submission
            {
                Id = UngradedSubmissionId,
                AssignmentId = OpenAssignmentId,
                StudentId = Student1Id,
                AnswerText = "Q1: v = u + at gives 24 m/s. Q2: s = ut + ½at² gives 90 m. Remaining answers attached in my notebook.",
                SubmittedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2),
                Status = SubmissionStatus.Submitted
            },
            new Submission
            {
                Id = GradedSubmissionId,
                AssignmentId = OpenAssignmentId,
                StudentId = Student2Id,
                AnswerText = "Q1: 24 m/s. Q2: 90 m. Q3: 4.5 s. Q4: 12.5 m/s². Q5: assumed air resistance is negligible.",
                SubmittedAt = now.AddDays(-3),
                UpdatedAt = now.AddDays(-3),
                Status = SubmissionStatus.Reviewed,
                Marks = 42,
                Feedback = "Strong work overall. Q4 mixes up the sign convention for deceleration - review that section.",
                ReviewedAt = now.AddDays(-1)
            }
        };

        _context.ClassRooms.AddRange(classRooms);
        _context.Subjects.AddRange(subjects);
        _context.Users.AddRange(users);
        _context.TeacherAssignments.AddRange(teacherAssignments);
        _context.Assignments.AddRange(assignments);
        _context.Submissions.AddRange(submissions);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Seed complete: {Users} users, {ClassRooms} classes, {Subjects} subjects, {TeacherAssignments} teacher assignments, {Assignments} assignments, {Submissions} submissions.",
            users.Length,
            classRooms.Length,
            subjects.Length,
            teacherAssignments.Length,
            assignments.Length,
            submissions.Length);
    }
}
