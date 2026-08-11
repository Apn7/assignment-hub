using AssignmentHub.Application.Interfaces;
using AssignmentHub.Application.Services;
using AssignmentHub.Domain.Entities;
using Moq;

namespace AssignmentHub.Tests.Unit;

public class TeacherAssignmentServiceTests
{
    private static readonly Guid Teacher1 = new("20000000-0000-0000-0000-000000000001");
    private static readonly Guid Class9A = new("40000000-0000-0000-0000-000000000001");
    private static readonly Guid Physics = new("50000000-0000-0000-0000-000000000001");

    [Fact]
    public async Task ListMineAsync_ReturnsOnlyCallersPairs()
    {
        var repositoryMock = new Mock<ITeacherAssignmentRepository>();
        var pairs = new List<TeacherAssignment>
        {
            new()
            {
                Id = Guid.NewGuid(),
                TeacherId = Teacher1,
                ClassRoomId = Class9A,
                ClassRoom = new ClassRoom { Id = Class9A, Name = "Class 9 – A" },
                SubjectId = Physics,
                Subject = new Subject { Id = Physics, Name = "Physics" }
            }
        };

        repositoryMock
            .Setup(repo => repo.ListForTeacherAsync(Teacher1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pairs);

        var service = new TeacherAssignmentService(repositoryMock.Object);

        var result = await service.ListMineAsync(Teacher1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value![0].ClassRoomId.Should().Be(Class9A);
        result.Value![0].ClassRoomName.Should().Be("Class 9 – A");
        result.Value![0].SubjectId.Should().Be(Physics);
        result.Value![0].SubjectName.Should().Be("Physics");
    }
}
