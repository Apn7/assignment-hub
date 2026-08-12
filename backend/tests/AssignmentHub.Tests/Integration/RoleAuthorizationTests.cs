using System.Net;
using System.Net.Http.Json;
using System.Text;
using AssignmentHub.Application.DTOs.Auth;

namespace AssignmentHub.Tests.Integration;

/// <summary>
/// Every protected endpoint, against every role that must not reach it.
/// </summary>
/// <remarks>
/// This is the final checklist's "role-based access is enforced by the backend API",
/// asserted as a matrix rather than as a handful of examples. A per-action
/// <c>[Authorize(Roles = ...)]</c> attribute is easy to forget on a new endpoint and
/// impossible to notice missing, because the frontend never asks for the route it is
/// not supposed to have. Enumerating the surface here means a route added without a
/// gate fails a test instead of shipping.
///
/// Both halves matter and they are different failures. A missing attribute shows up
/// as anonymous reaching the route (401 expected, 200 or 500 seen). A wrong role name
/// shows up in the second theory: authenticated but wrong-role callers should be
/// refused, and a role-claim mismatch would let them through — or, in the mirror-image
/// bug, refuse the correct role too, which the workflow tests would then catch.
/// </remarks>
public sealed class RoleAuthorizationTests : IClassFixture<ApiFactory>
{
    private const string Admin = nameof(Admin);
    private const string Teacher = nameof(Teacher);
    private const string Student = nameof(Student);

    private static readonly string[] AllRoles = [Admin, Teacher, Student];

    /// <summary>
    /// Stands in for any resource id. Authorization runs before routing has to find
    /// anything and before a body is bound, so the id never needs to exist — which is
    /// the property being relied on: a refusal must not depend on what is stored.
    /// </summary>
    private const string AnyId = "00000000-0000-0000-0000-0000000000ff";

    private readonly ApiFactory _factory;

    public RoleAuthorizationTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private sealed record Endpoint(string Method, string Path, string[] AllowedRoles);

    /// <summary>
    /// The whole authenticated surface. Anonymous endpoints (health, login) are
    /// asserted separately at the bottom.
    /// </summary>
    private static readonly Endpoint[] Endpoints =
    [
        // Any authenticated caller.
        new("GET", "/api/auth/me", AllRoles),

        // Teacher: authoring assignments.
        new("POST", "/api/assignments", [Teacher]),
        new("PUT", $"/api/assignments/{AnyId}", [Teacher]),
        new("POST", $"/api/assignments/{AnyId}/publish", [Teacher]),
        new("DELETE", $"/api/assignments/{AnyId}", [Teacher]),
        new("GET", "/api/assignments/mine", [Teacher]),

        // Teacher: marking.
        new("GET", $"/api/assignments/{AnyId}/submissions", [Teacher]),
        new("GET", $"/api/submissions/{AnyId}", [Teacher]),
        new("POST", $"/api/submissions/{AnyId}/grade", [Teacher]),
        new("POST", $"/api/submissions/{AnyId}/status", [Teacher]),
        new("GET", "/api/teacher-assignments/mine", [Teacher]),

        // Student: reading their class's work and answering it.
        new("GET", "/api/assignments", [Student]),
        new("GET", $"/api/assignments/{AnyId}", [Student]),
        new("POST", $"/api/assignments/{AnyId}/submissions", [Student]),
        new("PUT", $"/api/assignments/{AnyId}/submissions/mine", [Student]),
        new("GET", $"/api/assignments/{AnyId}/submissions/mine", [Student]),

        // Admin: user, class, subject and entitlement management, plus the audit views.
        new("GET", "/api/admin/assignments", [Admin]),
        new("GET", "/api/admin/submissions", [Admin]),
        new("POST", "/api/admin/users", [Admin]),
        new("GET", "/api/admin/users", [Admin]),
        new("POST", "/api/admin/classrooms", [Admin]),
        new("GET", "/api/admin/classrooms", [Admin]),
        new("POST", "/api/admin/subjects", [Admin]),
        new("GET", "/api/admin/subjects", [Admin]),
        new("POST", "/api/admin/teacher-assignments", [Admin]),
        new("GET", "/api/admin/teacher-assignments", [Admin])
    ];

    public static IEnumerable<object[]> ProtectedEndpoints() =>
        Endpoints.Select(endpoint => new object[] { endpoint.Method, endpoint.Path });

    public static IEnumerable<object[]> WrongRoleCombinations() =>
        from endpoint in Endpoints
        from role in AllRoles.Except(endpoint.AllowedRoles)
        select new object[] { endpoint.Method, endpoint.Path, role };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task EveryProtectedEndpoint_RejectsAnonymousCallers(string method, string path)
    {
        var client = _factory.AnonymousClient();

        var response = await client.SendAsync(Request(method, path));

        response.StatusCode.Should().Be(
            HttpStatusCode.Unauthorized,
            $"{method} {path} carries no token and must never be reachable without one");
    }

    [Theory]
    [MemberData(nameof(WrongRoleCombinations))]
    public async Task EveryProtectedEndpoint_RejectsRolesItDoesNotServe(
        string method,
        string path,
        string role)
    {
        var client = await _factory.ClientForAsync(EmailFor(role));

        var response = await client.SendAsync(Request(method, path));

        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            $"{method} {path} is not part of the {role} surface");
    }

    [Fact]
    public async Task Health_IsReachableAnonymously()
    {
        var response = await _factory.AnonymousClient().GetAsync("/api/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_IsReachableAnonymously_AndIssuesAUsableToken()
    {
        var response = await _factory.AnonymousClient().PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestData.Teacher1Email, password = TestData.Password });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

        login!.AccessToken.Should().NotBeNullOrWhiteSpace();
        login.User.Role.Should().Be(nameof(Teacher));
        login.ExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Login_RejectsAWrongPassword()
    {
        var response = await _factory.AnonymousClient().PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestData.Teacher1Email, password = "not-the-password" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData(Admin)]
    [InlineData(Teacher)]
    [InlineData(Student)]
    public async Task Me_ReturnsTheRoleTheTokenWasIssuedFor(string role)
    {
        var client = await _factory.ClientForAsync(EmailFor(role));

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await response.Content.ReadFromJsonAsync<UserSummary>();

        // Proves the round trip end to end: the role claim JwtTokenGenerator wrote is
        // the one the validated principal hands back, which is the same claim
        // [Authorize(Roles = ...)] matches on.
        me!.Role.Should().Be(role);
        me.Email.Should().Be(EmailFor(role));
    }

    [Fact]
    public async Task AMalformedToken_IsRefused()
    {
        var client = _factory.AnonymousClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer not-a-jwt");

        var response = await client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string EmailFor(string role) => role switch
    {
        Admin => TestData.AdminEmail,
        Teacher => TestData.Teacher1Email,
        Student => TestData.Student1Email,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown role.")
    };

    /// <summary>
    /// Writes-shaped verbs get an empty JSON body so the request is well-formed. It is
    /// never read: authorization short-circuits before model binding, which is exactly
    /// why a 403 here proves the gate and not the validator.
    /// </summary>
    private static HttpRequestMessage Request(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);

        if (method is "POST" or "PUT")
        {
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        }

        return request;
    }
}
