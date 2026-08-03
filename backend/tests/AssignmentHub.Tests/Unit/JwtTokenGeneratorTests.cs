using System.IdentityModel.Tokens.Jwt;
using AssignmentHub.Application.Common;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;
using AssignmentHub.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AssignmentHub.Tests.Unit;

/// <summary>
/// Verifies the real token generator against a real signing key. The claims
/// asserted here are the contract <c>Program.cs</c> validates and
/// <c>AuthController.Me</c> reads, so a rename that breaks authorization fails
/// here first.
/// </summary>
public class JwtTokenGeneratorTests
{
    // Test-only key. 32+ bytes, as HMAC-SHA256 requires.
    private const string TestSecret = "test-only-signing-key-with-enough-bytes";

    private static readonly DateTimeOffset FixedNow =
        new(2026, 8, 3, 12, 0, 0, TimeSpan.Zero);

    private static readonly JwtOptions Options = new()
    {
        Secret = TestSecret,
        Issuer = "AssignmentHub",
        Audience = "AssignmentHubClient",
        AccessTokenMinutes = 60
    };

    private static User AdminUser() => new()
    {
        Id = Guid.Parse("10000000-0000-0000-0000-000000000001"),
        FullName = "System Administrator",
        Email = "admin@assignmenthub.local",
        PasswordHash = "irrelevant",
        Role = UserRole.Admin,
        CreatedAt = FixedNow.UtcDateTime
    };

    private static JwtTokenGenerator CreateSut() =>
        new(new OptionsWrapper<JwtOptions>(Options), new FakeTimeProvider(FixedNow));

    [Fact]
    public void Generate_IncludesTheClaimsAuthorizationDependsOn()
    {
        var user = AdminUser();

        var generated = CreateSut().Generate(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(generated.Token);

        token.Claims.Should().ContainSingle(claim => claim.Type == AppClaimTypes.UserId)
            .Which.Value.Should().Be(user.Id.ToString());
        token.Claims.Should().ContainSingle(claim => claim.Type == AppClaimTypes.Email)
            .Which.Value.Should().Be(user.Email);
        token.Claims.Should().ContainSingle(claim => claim.Type == AppClaimTypes.Name)
            .Which.Value.Should().Be(user.FullName);

        // The claim [Authorize(Roles = ...)] matches on. Stored as the enum *name*,
        // matching nameof(UserRole.Admin) in the attribute.
        token.Claims.Should().ContainSingle(claim => claim.Type == AppClaimTypes.Role)
            .Which.Value.Should().Be(nameof(UserRole.Admin));
    }

    [Fact]
    public void Generate_UsesShortClaimNamesNotWifUris()
    {
        var generated = CreateSut().Generate(AdminUser());
        var token = new JwtSecurityTokenHandler().ReadJwtToken(generated.Token);

        // MapInboundClaims is disabled in Program.cs, so the names issued here are the
        // names read back. A long "schemas.xmlsoap.org/..." claim type would mean the
        // outbound map had rewritten something and the two ends no longer agree.
        token.Claims.Select(claim => claim.Type)
            .Should().NotContain(type => type.StartsWith("http://schemas.", StringComparison.Ordinal));
    }

    [Fact]
    public void Generate_SetsIssuerAudienceAndExpiryFromConfiguration()
    {
        var generated = CreateSut().Generate(AdminUser());
        var token = new JwtSecurityTokenHandler().ReadJwtToken(generated.Token);

        token.Issuer.Should().Be(Options.Issuer);
        token.Audiences.Should().ContainSingle().Which.Should().Be(Options.Audience);

        // Exact, not approximate, because the clock is injected.
        generated.ExpiresAtUtc.Should().Be(FixedNow.UtcDateTime.AddMinutes(Options.AccessTokenMinutes));
        token.ValidTo.Should().Be(generated.ExpiresAtUtc);
    }

    [Fact]
    public void Generate_SignsWithHmacSha256()
    {
        var generated = CreateSut().Generate(AdminUser());
        var token = new JwtSecurityTokenHandler().ReadJwtToken(generated.Token);

        token.Header.Alg.Should().Be(SecurityAlgorithms.HmacSha256);
    }

    [Fact]
    public void Generate_ProducesADistinctTokenIdEachTime()
    {
        var sut = CreateSut();
        var handler = new JwtSecurityTokenHandler();

        // Same user, same frozen clock: without a jti the two tokens would be
        // byte-identical, leaving nothing for a future revocation list to key on.
        var first = handler.ReadJwtToken(sut.Generate(AdminUser()).Token);
        var second = handler.ReadJwtToken(sut.Generate(AdminUser()).Token);

        first.Id.Should().NotBeNullOrEmpty();
        first.Id.Should().NotBe(second.Id);
    }

    /// <summary>Minimal frozen clock, so expiry assertions are exact.</summary>
    private sealed class FakeTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _utcNow;

        public FakeTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;
    }
}
