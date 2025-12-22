namespace Sunjsong.Auth.Abstractions;

public interface IRbacStoreWriter
{
    Task CreateUserAsync(User user, CancellationToken ct = default);
    Task UpdateUserAsync(User user, CancellationToken ct = default);
    Task DeleteUserAsync(string userId, CancellationToken ct = default);

    Task CreateRoleAsync(Role role, CancellationToken ct = default);
    Task UpdateRoleAsync(Role role, CancellationToken ct = default);
    Task DeleteRoleAsync(string roleId, CancellationToken ct = default);

    Task AddUserRoleAsync(string userId, string roleId, CancellationToken ct = default);
    Task RemoveUserRoleAsync(string userId, string roleId, CancellationToken ct = default);

    Task AddRolePermissionAsync(string roleId, string permissionKey, CancellationToken ct = default);
    Task RemoveRolePermissionAsync(string roleId, string permissionKey, CancellationToken ct = default);
}
