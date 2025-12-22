namespace Sunjsong.Auth.Abstractions;

public sealed record RolePermission
{
    public string RoleId { get; init; } = string.Empty;
    public string PermissionKey { get; init; } = string.Empty;
}
