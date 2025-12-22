namespace Sunjsong.Auth.Abstractions;

public interface IRbacStore
{
    Task<RbacSnapshot> LoadAsync(CancellationToken ct = default);
}
