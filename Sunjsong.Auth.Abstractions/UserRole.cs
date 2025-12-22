namespace Sunjsong.Auth.Abstractions;

public sealed record UserRole
{
    public string UserId { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
}
