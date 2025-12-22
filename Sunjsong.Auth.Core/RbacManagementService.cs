using Sunjsong.Auth.Abstractions;

namespace Sunjsong.Auth.Core;

public sealed class RbacManagementService : IRbacManagementService
{
    private readonly IRbacStoreWriter _storeWriter;

    public RbacManagementService(IRbacStoreWriter storeWriter)
    {
        _storeWriter = storeWriter;
    }

    public Task<User> CreateUserAsync(UserUpsertInput input, CancellationToken ct = default)
    {
        return _storeWriter.CreateUserAsync(input, ct);
    }

    public Task<User> UpdateUserAsync(UserUpsertInput input, CancellationToken ct = default)
    {
        return _storeWriter.UpdateUserAsync(input, ct);
    }

    public Task DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        return _storeWriter.DeleteUserAsync(userId, ct);
    }

    public Task<RbacPageResult<User>> QueryUsersAsync(UserQuery query, CancellationToken ct = default)
    {
        return _storeWriter.QueryUsersAsync(query, ct);
    }

    public Task<Role> CreateRoleAsync(RoleUpsertInput input, CancellationToken ct = default)
    {
        return _storeWriter.CreateRoleAsync(input, ct);
    }

    public Task<Role> UpdateRoleAsync(RoleUpsertInput input, CancellationToken ct = default)
    {
        return _storeWriter.UpdateRoleAsync(input, ct);
    }

    public Task DeleteRoleAsync(string roleId, CancellationToken ct = default)
    {
        return _storeWriter.DeleteRoleAsync(roleId, ct);
    }

    public Task<RbacPageResult<Role>> QueryRolesAsync(RoleQuery query, CancellationToken ct = default)
    {
        return _storeWriter.QueryRolesAsync(query, ct);
    }

    public Task<UserRole> CreateUserRoleAsync(UserRoleUpsertInput input, CancellationToken ct = default)
    {
        return _storeWriter.CreateUserRoleAsync(input, ct);
    }

    public Task<UserRole> UpdateUserRoleAsync(UserRoleKey key, UserRoleUpsertInput input, CancellationToken ct = default)
    {
        return _storeWriter.UpdateUserRoleAsync(key, input, ct);
    }

    public Task DeleteUserRoleAsync(UserRoleKey key, CancellationToken ct = default)
    {
        return _storeWriter.DeleteUserRoleAsync(key, ct);
    }

    public Task<RbacPageResult<UserRole>> QueryUserRolesAsync(UserRoleQuery query, CancellationToken ct = default)
    {
        return _storeWriter.QueryUserRolesAsync(query, ct);
    }

    public Task<RolePermission> CreateRolePermissionAsync(RolePermissionUpsertInput input, CancellationToken ct = default)
    {
        return _storeWriter.CreateRolePermissionAsync(input, ct);
    }

    public Task<RolePermission> UpdateRolePermissionAsync(RolePermissionKey key, RolePermissionUpsertInput input, CancellationToken ct = default)
    {
        return _storeWriter.UpdateRolePermissionAsync(key, input, ct);
    }

    public Task DeleteRolePermissionAsync(RolePermissionKey key, CancellationToken ct = default)
    {
        return _storeWriter.DeleteRolePermissionAsync(key, ct);
    }

    public Task<RbacPageResult<RolePermission>> QueryRolePermissionsAsync(RolePermissionQuery query, CancellationToken ct = default)
    {
        return _storeWriter.QueryRolePermissionsAsync(query, ct);
    }
}
