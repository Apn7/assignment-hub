using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AssignmentHub.Application.DTOs.Auth;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssignmentHub.Tests.Integration;

/// <summary>
/// Hosts the real API pipeline in-process so tests can make HTTP calls against it.
/// </summary>
/// <remarks>
/// The point of these tests is the one thing a service test cannot reach: the
/// pipeline itself. Routing, JWT validation, <c>[Authorize(Roles = ...)]</c> and the
/// <c>Result</c>-to-status mapping only exist once a request has actually travelled
/// through <c>Program.cs</c>. The final checklist asks that "role-based access is
/// enforced by the backend API", and that is a claim about the edge, not about a
/// service class.
///
/// Only the database is substituted. Every other component — the token generator,
/// the password hasher, the middleware order, the authorization policies — is the
/// production registration, so a token here is signed and validated exactly as it
/// would be against Postgres.
/// </remarks>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    /// <summary>
    /// 58 ASCII characters, comfortably over the 32-byte floor Program.cs enforces.
    /// A fixture value, never a deployed one.
    /// </summary>
    private const string TestJwtSecret =
        "assignment-hub-integration-test-signing-key-0123456789";

    /// <summary>Own store per factory instance, so no test class can see another's writes.</summary>
    private readonly string _databaseName = $"assignment-hub-api-{Guid.NewGuid()}";

    static ApiFactory()
    {
        // Program.cs reads Jwt:Secret and ConnectionStrings:Default off
        // builder.Configuration *before* builder.Build(), and throws on either being
        // absent. ConfigureAppConfiguration only runs during Build, so it lands too
        // late to satisfy those guards. Environment variables are layered in by
        // CreateBuilder itself — which runs inside the entry point, after this static
        // constructor — so they are the one hook early enough to be seen.
        Environment.SetEnvironmentVariable("Jwt__Secret", TestJwtSecret);

        // Never opened. AddInfrastructure refuses to run without a connection string,
        // and the DbContext registration it creates is replaced in ConfigureWebHost
        // before anything resolves it.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Default",
            "Host=in-memory-provider-replaces-this;Database=unused;Username=unused;Password=unused");
    }

    public ApiFactory()
    {
        // Touching Services boots the host, which is what we want: the fixture has to
        // be in place before the first request, and seeding needs the real
        // IPasswordHasher so the seeded passwords verify through the real login path.
        using var scope = Services.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        TestData.Seed(context, passwordHasher);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Deliberately not Development. That branch runs DataSeeder, whose first act is
        // GetPendingMigrationsAsync — a question the in-memory provider cannot answer —
        // and mounts Swagger, which these tests do not need. The else-branch's
        // UseHttpsRedirection finds no HTTPS port under TestServer, logs that once and
        // passes every request through untouched.
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Swap the store, keep the context. Same AppDbContext, same entity
            // configurations, same UTC value converters — only the provider changes, so
            // what these tests exercise is the real model.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    /// <summary>A client carrying no <c>Authorization</c> header.</summary>
    public HttpClient AnonymousClient() => CreateClient();

    /// <summary>
    /// Logs the fixture account in through <c>POST /api/auth/login</c> and returns a
    /// client that presents the resulting bearer token.
    /// </summary>
    /// <remarks>
    /// Going through the real endpoint rather than minting a token directly is the
    /// point: it proves the claims <c>JwtTokenGenerator</c> writes are the claims
    /// <c>TokenValidationParameters</c> reads back, which is precisely the seam where
    /// a role check silently stops matching and every request turns into a 403.
    /// </remarks>
    public async Task<HttpClient> ClientForAsync(string email)
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = TestData.Password });

        response.StatusCode.Should().Be(
            HttpStatusCode.OK,
            $"the fixture account {email} must be able to log in");

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.AccessToken);

        return client;
    }
}
