using Sunjsong.Auth.Abstractions;

namespace Sunjsong.Auth.Core;

public sealed class AuthorizationService : IAuthorizationService
{
    private readonly IRbacStoreReader _store;
    private readonly IUserContext _userContext;
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly HashSet<string> _catalogKeys;
    private readonly object _sync = new();
    private RbacSnapshot _snapshot = new();
    private HashSet<string> _permissions = new(StringComparer.Ordinal);

    public AuthorizationService(IRbacStoreReader store, IUserContext userContext, IPermissionCatalog permissionCatalog)
    {
        _store = store;
        _userContext = userContext;
        _permissionCatalog = permissionCatalog;
        _catalogKeys = _permissionCatalog.GetAll()
            .Select(definition => definition.Key)
            .ToHashSet(StringComparer.Ordinal);

        _userContext.UserChanged += (_, _) =>
        {
            RecalculatePermissions();
            AuthorizationChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    public event EventHandler? AuthorizationChanged;

    public bool Can(string permissionKey)
    {
        if (!IsPermissionKnown(permissionKey))
        {
            // Unknown permission keys are treated as denied. Hook in logging here if needed.
            return false;
        }

        lock (_sync)
        {
            return _permissions.Contains(permissionKey);
        }
    }

    public void Demand(string permissionKey)
    {
        if (!Can(permissionKey))
        {
            var userId = _userContext.CurrentUserId;
            throw new UnauthorizedAccessException(
                $"User '{userId}' does not have permission '{permissionKey}'.");
        }
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        _snapshot = await _store.LoadAsync(ct).ConfigureAwait(false);
        RecalculatePermissions();
        AuthorizationChanged?.Invoke(this, EventArgs.Empty);
    }

    private bool IsPermissionKnown(string permissionKey)
    {
        return _catalogKeys.Contains(permissionKey);
    }

    private void RecalculatePermissions()
    {
        var currentUserId = _userContext.CurrentUserId;
        var roleIds = _snapshot.UserRoles
            .Where(link => string.Equals(link.UserId, currentUserId, StringComparison.Ordinal))
            .Select(link => link.RoleId)
            .ToHashSet(StringComparer.Ordinal);

        var permissions = _snapshot.RolePermissions
            .Where(link => roleIds.Contains(link.RoleId))
            .Select(link => link.PermissionKey)
            .ToHashSet(StringComparer.Ordinal);

        lock (_sync)
        {
            _permissions = permissions;
        }
    }
}
