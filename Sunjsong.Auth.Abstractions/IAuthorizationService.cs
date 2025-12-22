namespace Sunjsong.Auth.Abstractions;

public interface IAuthorizationService
{
    bool Can(string permissionKey);

    void Demand(string permissionKey);

    Task RefreshAsync(CancellationToken ct = default);

    event EventHandler? AuthorizationChanged;
}
