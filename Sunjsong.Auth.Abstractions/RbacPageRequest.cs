namespace Sunjsong.Auth.Abstractions;

public sealed record RbacPageRequest
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
