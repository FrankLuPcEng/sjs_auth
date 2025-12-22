namespace Sunjsong.Auth.Abstractions;

public sealed record Role
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
