namespace Sunjsong.Auth.Abstractions;

public sealed record UserQuery
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? NameContains { get; init; }
    public RbacPageRequest Page { get; init; } = new();
}

public sealed record RoleQuery
{
    public string? Id { get; init; }
    public string? Name { get; init; }
    public string? NameContains { get; init; }
    public RbacPageRequest Page { get; init; } = new();
}

public sealed record UserRoleQuery
{
    public string? UserId { get; init; }
    public string? RoleId { get; init; }
    public RbacPageRequest Page { get; init; } = new();
}

public sealed record RolePermissionQuery
{
    public string? RoleId { get; init; }
    public string? PermissionKey { get; init; }
    public RbacPageRequest Page { get; init; } = new();
}
