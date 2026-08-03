using AssignmentHub.Application.DTOs.Auth;
using AssignmentHub.Domain.Entities;

namespace AssignmentHub.Application.Interfaces;

/// <summary>
/// Issues signed access tokens. Behind an interface so <c>AuthService</c> can be
/// unit-tested without signing keys or real cryptography.
/// </summary>
public interface IJwtTokenGenerator
{
    GeneratedToken Generate(User user);
}
