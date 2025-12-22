namespace Sunjsong.Auth.Abstractions;

public sealed record PermissionDefinition
{
    public string Key { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
