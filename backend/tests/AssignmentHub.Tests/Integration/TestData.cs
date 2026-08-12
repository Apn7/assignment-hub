using AssignmentHub.Application.Interfaces;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;
using AssignmentHub.Infrastructure.Persistence;

namespace AssignmentHub.Tests.Integration;

/// <summary>
/// The fixture every integration test reads from: two classes, two subjects, two
/// teachers with disjoint entitlements, two students in different classes, and one
/// assignment per state worth asserting on.
/// </summary>
/// <remarks>
/// Deliberately separate from <c>DataSeeder</c>'s demo dataset. These tests assert on
/// exact ids and counts, so they must not break the next time the demo data is
/// retuned for a screenshot — and the demo seeder must stay free to change without
/// dragging the test suite behind it.
///
/// The shape is chosen so that every cross-boundary question has a witness: whatever
/// teacher1 owns, teacher2 does not, and whatever class 9-A can see, class 10-A
/// cannot.
/// </remarks>
internal static class TestData
{
    /// <summary>One password for every fixture account. Hashed through the real hasher at seed time.</summary>
    public const string Password = "Fixture#2026";

    public const string AdminEmail = "admin@integration.local";
    public const string Teacher1Email = "teacher1@integration.local";
    public const string Teacher2Email = "teacher2@integration.local";
    public const string Student1Email = "student1@integration.local";
    public const string Student2Email = "student2@integration.local";

    public static readonly Guid AdminId = new("11000000-0000-0000-0000-000000000001");

    /// <summary>Entitled to Physics for class 9-A, and nothing else.</summary>
    public static readonly Guid Teacher1Id = new("22000000-0000-0000-0000-000000000001");

    /// <summary>Entitled to English for class 10-A, and nothing else.</summary>
    public static readonly Guid Teacher2Id = new("22000000-0000-0000-0000-000000000002");

    /// <summary>In class 9-A.</summary>
    public static readonly Guid Student1Id = new("33000000-0000-0000-0000-000000000001");

    /// <summary>In class 10-A.</summary>
    public static readonly Guid Student2Id = new("33000000-0000-0000-0000-000000000002");

    public static readonly Guid Class9AId = new("44000000-0000-0000-0000-000000000001");
    public static readonly Guid Class10AId = new("44000000-0000-0000-0000-000000000002");

    public static readonly Guid PhysicsId = new("55000000-0000-0000-0000-000000000001");
    public static readonly Guid EnglishId = new("55000000-0000-0000-0000-000000000002");

    /// <summary>Teacher1, class 9-A, published, deadline in the future.</summary>
    public static readonly Guid OpenFor9AId = new("66000000-0000-0000-0000-000000000001");

    /// <summary>Teacher1, class 9-A, still a draft. No student may see this.</summary>
    public static readonly Guid DraftFor9AId = new("66000000-0000-0000-0000-000000000002");

    /// <summary>Teacher1, class 9-A, published, deadline already gone.</summary>
    public static readonly Guid ClosedFor9AId = new("66000000-0000-0000-0000-000000000003");

    /// <summary>Teacher2, class 10-A, published. Invisible to class 9-A.</summary>
    public static readonly Guid OpenFor10AId = new("66000000-0000-0000-0000-000000000004");

    /// <summary>Student1's answer on <see cref="OpenFor9AId"/>, so it sits on teacher1's assignment.</summary>
    public static readonly Guid Student1SubmissionId = new("77000000-0000-0000-0000-000000000001");

    /// <summary>Student2's answer on <see cref="OpenFor10AId"/>, so it sits on teacher2's assignment.</summary>
    public static readonly Guid Student2SubmissionId = new("77000000-0000-0000-0000-000000000002");

    /// <summary>Marks on <see cref="OpenFor9AId"/>, so a grade-boundary test has a real ceiling.</summary>
    public const int OpenFor9AMaxMarks = 50;

    public static void Seed(AppDbContext context, IPasswordHasher passwordHasher)
    {
        var now = DateTime.UtcNow;

        context.ClassRooms.AddRange(
            new ClassRoom { Id = Class9AId, Name = "Class 9 - A" },
            new ClassRoom { Id = Class10AId, Name = "Class 10 - A" });

        context.Subjects.AddRange(
            new Subject { Id = PhysicsId, Name = "Physics" },
            new Subject { Id = EnglishId, Name = "English" });

        User User(Guid id, string fullName, string email, UserRole role, Guid? classRoomId = null) => new()
        {
            Id = id,
            FullName = fullName,
            Email = email,
            // The real hasher, so these passwords verify through the real login path.
            PasswordHash = passwordHasher.Hash(Password),
            Role = role,
            ClassRoomId = classRoomId,
            CreatedAt = now.AddYears(-1)
        };

        context.Users.AddRange(
            User(AdminId, "Integration Admin", AdminEmail, UserRole.Admin),
            User(Teacher1Id, "Physics Teacher", Teacher1Email, UserRole.Teacher),
            User(Teacher2Id, "English Teacher", Teacher2Email, UserRole.Teacher),
            User(Student1Id, "Nine A Student", Student1Email, UserRole.Student, Class9AId),
            User(Student2Id, "Ten A Student", Student2Email, UserRole.Student, Class10AId));

        // Disjoint on purpose: neither teacher holds any pair the other holds, so a
        // leak across entitlements cannot hide behind an overlap.
        context.TeacherAssignments.AddRange(
            new TeacherAssignment
            {
                Id = new("88000000-0000-0000-0000-000000000001"),
                TeacherId = Teacher1Id,
                ClassRoomId = Class9AId,
                SubjectId = PhysicsId
            },
            new TeacherAssignment
            {
                Id = new("88000000-0000-0000-0000-000000000002"),
                TeacherId = Teacher2Id,
                ClassRoomId = Class10AId,
                SubjectId = EnglishId
            });

        Assignment Assignment(
            Guid id,
            string title,
            Guid teacherId,
            Guid classRoomId,
            Guid subjectId,
            AssignmentStatus status,
            DateTime deadline,
            int maxMarks) => new()
        {
            Id = id,
            Title = title,
            Description = "Work through the questions set in class.",
            ClassRoomId = classRoomId,
            SubjectId = subjectId,
            CreatedByTeacherId = teacherId,
            Deadline = deadline,
            MaxMarks = maxMarks,
            Status = status,
            CreatedAt = now.AddDays(-10),
            UpdatedAt = now.AddDays(-10)
        };

        context.Assignments.AddRange(
            Assignment(
                OpenFor9AId, "Kinematics Problem Set", Teacher1Id, Class9AId, PhysicsId,
                AssignmentStatus.Published, now.AddDays(7), OpenFor9AMaxMarks),
            Assignment(
                DraftFor9AId, "Unpublished Worksheet", Teacher1Id, Class9AId, PhysicsId,
                AssignmentStatus.Draft, now.AddDays(14), 20),
            Assignment(
                ClosedFor9AId, "Overdue Revision Sheet", Teacher1Id, Class9AId, PhysicsId,
                AssignmentStatus.Published, now.AddDays(-3), 30),
            Assignment(
                OpenFor10AId, "Comprehension Exercise", Teacher2Id, Class10AId, EnglishId,
                AssignmentStatus.Published, now.AddDays(7), 40));

        context.Submissions.AddRange(
            new Submission
            {
                Id = Student1SubmissionId,
                AssignmentId = OpenFor9AId,
                StudentId = Student1Id,
                AnswerText = "Q1: 24 m/s. Q2: 90 m.",
                SubmittedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2),
                Status = SubmissionStatus.Submitted
            },
            new Submission
            {
                Id = Student2SubmissionId,
                AssignmentId = OpenFor10AId,
                StudentId = Student2Id,
                AnswerText = "The narrator is unreliable because the timeline contradicts itself.",
                SubmittedAt = now.AddDays(-2),
                UpdatedAt = now.AddDays(-2),
                Status = SubmissionStatus.Submitted
            });

        context.SaveChanges();
        context.ChangeTracker.Clear();
    }
}
