using AssignmentHub.Infrastructure.Persistence;

namespace AssignmentHub.Tests.Unit;

/// <summary>
/// Placeholder suite that proves the harness works: xUnit discovers tests,
/// FluentAssertions is available, and the project reference graph reaches every
/// layer under test. Real business-rule, authorization and submission-workflow
/// tests replace this once the domain model exists.
/// </summary>
public class SolutionSmokeTests
{
    [Fact]
    public void TestHarness_IsWiredUp()
    {
        const int answer = 1 + 1;

        answer.Should().Be(2);
    }

    [Fact]
    public void ReferenceGraph_ReachesApiAndInfrastructure()
    {
        // Compiles only if Tests -> Api and Tests -> Infrastructure both resolve,
        // which is what later WebApplicationFactory and EF Core tests depend on.
        typeof(Program).Assembly.GetName().Name.Should().Be("AssignmentHub.Api");
        typeof(AppDbContext).Assembly.GetName().Name.Should().Be("AssignmentHub.Infrastructure");
    }
}
