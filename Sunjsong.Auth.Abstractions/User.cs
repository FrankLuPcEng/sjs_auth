namespace Sunjsong.Auth.Abstractions;

public sealed record User
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
