namespace Sunjsong.Auth.Abstractions;

public sealed record UserUpsertInput
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed record RoleUpsertInput
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed record UserRoleUpsertInput
{
    public string UserId { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
}

public sealed record RolePermissionUpsertInput
{
    public string RoleId { get; init; } = string.Empty;
    public string PermissionKey { get; init; } = string.Empty;
}

public sealed record UserRoleKey
{
    public string UserId { get; init; } = string.Empty;
    public string RoleId { get; init; } = string.Empty;
}

public sealed record RolePermissionKey
{
    public string RoleId { get; init; } = string.Empty;
    public string PermissionKey { get; init; } = string.Empty;
}
