namespace Sunjsong.Auth.Abstractions;

public interface IRbacManagementService
{
    Task<User> CreateUserAsync(UserUpsertInput input, CancellationToken ct = default);
    Task<User> UpdateUserAsync(UserUpsertInput input, CancellationToken ct = default);
    Task DeleteUserAsync(string userId, CancellationToken ct = default);
    Task<RbacPageResult<User>> QueryUsersAsync(UserQuery query, CancellationToken ct = default);

    Task<Role> CreateRoleAsync(RoleUpsertInput input, CancellationToken ct = default);
    Task<Role> UpdateRoleAsync(RoleUpsertInput input, CancellationToken ct = default);
    Task DeleteRoleAsync(string roleId, CancellationToken ct = default);
    Task<RbacPageResult<Role>> QueryRolesAsync(RoleQuery query, CancellationToken ct = default);

    Task<UserRole> CreateUserRoleAsync(UserRoleUpsertInput input, CancellationToken ct = default);
    Task<UserRole> UpdateUserRoleAsync(UserRoleKey key, UserRoleUpsertInput input, CancellationToken ct = default);
    Task DeleteUserRoleAsync(UserRoleKey key, CancellationToken ct = default);
    Task<RbacPageResult<UserRole>> QueryUserRolesAsync(UserRoleQuery query, CancellationToken ct = default);

    Task<RolePermission> CreateRolePermissionAsync(RolePermissionUpsertInput input, CancellationToken ct = default);
    Task<RolePermission> UpdateRolePermissionAsync(RolePermissionKey key, RolePermissionUpsertInput input, CancellationToken ct = default);
    Task DeleteRolePermissionAsync(RolePermissionKey key, CancellationToken ct = default);
    Task<RbacPageResult<RolePermission>> QueryRolePermissionsAsync(RolePermissionQuery query, CancellationToken ct = default);
}
