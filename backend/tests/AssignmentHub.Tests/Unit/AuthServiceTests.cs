using AssignmentHub.Application.DTOs.Auth;
using AssignmentHub.Application.Interfaces;
using AssignmentHub.Application.Services;
using AssignmentHub.Domain.Entities;
using AssignmentHub.Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssignmentHub.Tests.Unit;

/// <summary>
/// Behaviour of the login path. Pure unit tests: no database, no HTTP, no real
/// cryptography — every collaborator is mocked.
/// </summary>
public class AuthServiceTests
{
    private const string KnownEmail = "teacher1@assignmenthub.local";
    private const string CorrectPassword = "Teacher#1234";
    private const string StoredHash = "stored-hash";

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenGenerator> _tokenGenerator = new();

    private static User TeacherUser() => new()
    {
        Id = Guid.NewGuid(),
        FullName = "Ayesha Rahman",
        Email = KnownEmail,
        PasswordHash = StoredHash,
        Role = UserRole.Teacher,
        CreatedAt = DateTime.UtcNow
    };

    private AuthService CreateSut() => new(
        _users.Object,
        _passwordHasher.Object,
        _tokenGenerator.Object,
        NullLogger<AuthService>.Instance);

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsTokenAndUserWithCorrectRole()
    {
        var user = TeacherUser();
        var expiresAt = new DateTime(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc);

        _users.Setup(repository => repository.GetByEmailAsync(KnownEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(hasher => hasher.Verify(StoredHash, CorrectPassword))
            .Returns(true);
        _tokenGenerator.Setup(generator => generator.Generate(user))
            .Returns(new GeneratedToken("signed.jwt.value", expiresAt));

        var result = await CreateSut().LoginAsync(new LoginRequest
        {
            Email = KnownEmail,
            Password = CorrectPassword
        });

        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("signed.jwt.value");
        result.ExpiresAtUtc.Should().Be(expiresAt);
        result.User.Id.Should().Be(user.Id);
        result.User.Email.Should().Be(KnownEmail);
        result.User.FullName.Should().Be("Ayesha Rahman");
        result.User.Role.Should().Be(nameof(UserRole.Teacher));
    }

    [Fact]
    public async Task LoginAsync_NeverExposesThePasswordHash()
    {
        var user = TeacherUser();

        _users.Setup(repository => repository.GetByEmailAsync(KnownEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(hasher => hasher.Verify(StoredHash, CorrectPassword)).Returns(true);
        _tokenGenerator.Setup(generator => generator.Generate(user))
            .Returns(new GeneratedToken("signed.jwt.value", DateTime.UtcNow));

        var result = await CreateSut().LoginAsync(new LoginRequest
        {
            Email = KnownEmail,
            Password = CorrectPassword
        });

        // UserSummary has no hash property at all; this guards against someone
        // "helpfully" adding one, or swapping the response for the entity.
        result!.User.Should().BeOfType<UserSummary>();
        typeof(UserSummary).GetProperties()
            .Select(property => property.Name)
            .Should().NotContain(nameof(User.PasswordHash));
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ReturnsNull()
    {
        _users.Setup(repository => repository.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasher.Setup(hasher => hasher.Hash(It.IsAny<string>())).Returns("decoy-hash");
        _passwordHasher.Setup(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var result = await CreateSut().LoginAsync(new LoginRequest
        {
            Email = "nobody@assignmenthub.local",
            Password = CorrectPassword
        });

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsNull()
    {
        _users.Setup(repository => repository.GetByEmailAsync(KnownEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TeacherUser());
        _passwordHasher.Setup(hasher => hasher.Verify(StoredHash, "wrong-password")).Returns(false);

        var result = await CreateSut().LoginAsync(new LoginRequest
        {
            Email = KnownEmail,
            Password = "wrong-password"
        });

        result.Should().BeNull();

        // The token generator must not run for a rejected password.
        _tokenGenerator.Verify(generator => generator.Generate(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_UnknownEmailAndWrongPassword_AreIndistinguishable()
    {
        _users.Setup(repository => repository.GetByEmailAsync(KnownEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TeacherUser());
        _users.Setup(repository => repository.GetByEmailAsync("nobody@assignmenthub.local", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasher.Setup(hasher => hasher.Hash(It.IsAny<string>())).Returns("decoy-hash");
        _passwordHasher.Setup(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        var sut = CreateSut();

        var unknownEmailResult = await sut.LoginAsync(new LoginRequest
        {
            Email = "nobody@assignmenthub.local",
            Password = CorrectPassword
        });

        var wrongPasswordResult = await sut.LoginAsync(new LoginRequest
        {
            Email = KnownEmail,
            Password = "wrong-password"
        });

        // Both paths produce the same value, so no caller can tell which failed.
        unknownEmailResult.Should().BeNull();
        wrongPasswordResult.Should().BeNull();
        unknownEmailResult.Should().BeEquivalentTo(wrongPasswordResult);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_StillVerifiesAPassword()
    {
        _users.Setup(repository => repository.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        _passwordHasher.Setup(hasher => hasher.Hash(It.IsAny<string>())).Returns("decoy-hash");
        _passwordHasher.Setup(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>())).Returns(false);

        await CreateSut().LoginAsync(new LoginRequest
        {
            Email = "nobody@assignmenthub.local",
            Password = CorrectPassword
        });

        // Returning early for an unknown address would make the two failures
        // distinguishable by response time, which is user enumeration by stopwatch.
        _passwordHasher.Verify(
            hasher => hasher.Verify(It.IsAny<string>(), CorrectPassword),
            Times.Once);
    }

    [Theory]
    [InlineData("TEACHER1@assignmenthub.local")]
    [InlineData("  teacher1@assignmenthub.local  ")]
    [InlineData("Teacher1@AssignmentHub.local")]
    public async Task LoginAsync_NormalisesEmailBeforeLookup(string suppliedEmail)
    {
        var user = TeacherUser();

        _users.Setup(repository => repository.GetByEmailAsync(KnownEmail, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _passwordHasher.Setup(hasher => hasher.Verify(StoredHash, CorrectPassword)).Returns(true);
        _tokenGenerator.Setup(generator => generator.Generate(user))
            .Returns(new GeneratedToken("signed.jwt.value", DateTime.UtcNow));

        var result = await CreateSut().LoginAsync(new LoginRequest
        {
            Email = suppliedEmail,
            Password = CorrectPassword
        });

        result.Should().NotBeNull("casing and surrounding whitespace must not stop a valid login");
        _users.Verify(
            repository => repository.GetByEmailAsync(KnownEmail, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
