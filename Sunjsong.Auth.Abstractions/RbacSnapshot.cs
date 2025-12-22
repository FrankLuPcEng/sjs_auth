namespace Sunjsong.Auth.Abstractions;

public sealed record RbacSnapshot
{
    public IReadOnlyList<User> Users { get; init; } = Array.Empty<User>();
    public IReadOnlyList<Role> Roles { get; init; } = Array.Empty<Role>();
    public IReadOnlyList<UserRole> UserRoles { get; init; } = Array.Empty<UserRole>();
    public IReadOnlyList<RolePermission> RolePermissions { get; init; } = Array.Empty<RolePermission>();
}
