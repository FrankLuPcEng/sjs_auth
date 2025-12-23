using Sunjsong.Auth.Abstractions;
using Sunjsong.Auth.WpfUI.ViewModels;

namespace Sunjsong.Auth.WpfUI.Services;

internal static class SystemAccountSeeder
{
    private const string RootUserId = "root";
    private const string RootRoleId = "root-role";
    private const string RootName = "ROOT";
    private const string RootDefaultPassword = "Root@123!";

    private const string AdminUserId = "admin";
    private const string AdminRoleId = "admin-role";
    private const string AdminName = "ADMIN";
    private const string AdminDefaultPassword = "Admin@123!";

    public static async Task SeedAsync(IRbacRepository repository, IPermissionCatalog catalog, ILocalAccountService accounts)
    {
        var snapshot = await repository.LoadAsync();
        var users = snapshot.Users.ToDictionary(u => u.Id, StringComparer.OrdinalIgnoreCase);
        var roles = snapshot.Roles.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
        var userRoles = snapshot.UserRoles.ToHashSet(new UserRoleComparer());
        var permKeys = catalog.GetAll()?.Select(p => p.Key).Where(k => !string.IsNullOrWhiteSpace(k)).ToList() ?? new List<string>();

        // Root user/role: all permissions
        await EnsureUserAsync(repository, RootUserId, RootName);
        await EnsureRoleAsync(repository, RootRoleId, RootName);
        await EnsureUserRoleAsync(repository, RootUserId, RootRoleId, userRoles);
        await EnsureRolePermissionsAsync(repository, RootRoleId, permKeys, snapshot.RolePermissions);
        await EnsureAccountAsync(accounts, RootUserId, RootName, RootName, RootDefaultPassword, true);

        // Admin user/role: all non-root permissions
        await EnsureUserAsync(repository, AdminUserId, AdminName);
        await EnsureRoleAsync(repository, AdminRoleId, AdminName);
        await EnsureUserRoleAsync(repository, AdminUserId, AdminRoleId, userRoles);

        var adminPerms = permKeys.Where(k => !IsRootPermission(k)).ToList();
        await EnsureRolePermissionsAsync(repository, AdminRoleId, adminPerms, snapshot.RolePermissions);
        await EnsureAccountAsync(accounts, AdminUserId, AdminName, AdminName, AdminDefaultPassword, true);

        // Guest: no login / no permissions (intentionally skipped)
    }

    private static async Task EnsureUserAsync(IRbacRepository repo, string id, string name)
    {
        try
        {
            await repo.CreateUserAsync(new UserUpsertInput { Id = id, Name = name });
        }
        catch
        {
            await repo.UpdateUserAsync(new UserUpsertInput { Id = id, Name = name });
        }
    }

    private static async Task EnsureRoleAsync(IRbacRepository repo, string id, string name)
    {
        try
        {
            await repo.CreateRoleAsync(new RoleUpsertInput { Id = id, Name = name });
        }
        catch
        {
            await repo.UpdateRoleAsync(new RoleUpsertInput { Id = id, Name = name });
        }
    }

    private static async Task EnsureUserRoleAsync(IRbacRepository repo, string userId, string roleId, HashSet<UserRole> existing)
    {
        if (!existing.Contains(new UserRole { UserId = userId, RoleId = roleId }))
        {
            await repo.CreateUserRoleAsync(new UserRoleUpsertInput { UserId = userId, RoleId = roleId });
        }
    }

    private static async Task EnsureRolePermissionsAsync(IRbacRepository repo, string roleId, IEnumerable<string> desired, IReadOnlyList<RolePermission> existing)
    {
        var existingKeys = existing.Where(rp => rp.RoleId == roleId).Select(rp => rp.PermissionKey).ToHashSet(StringComparer.Ordinal);
        var toAdd = desired.Where(k => !existingKeys.Contains(k)).ToList();

        foreach (var key in toAdd)
        {
            await repo.CreateRolePermissionAsync(new RolePermissionUpsertInput { RoleId = roleId, PermissionKey = key });
        }
    }

    private static async Task EnsureAccountAsync(ILocalAccountService accounts, string userId, string userName, string displayName, string defaultPassword, bool isEnabled)
    {
        var existing = await accounts.GetByUserIdAsync(userId);
        if (existing is null)
        {
            await accounts.UpsertAsync(userId, userName, displayName, defaultPassword, isEnabled);
        }
        else if (!existing.IsEnabled || !string.Equals(existing.UserName, userName, StringComparison.OrdinalIgnoreCase))
        {
            await accounts.UpsertAsync(userId, userName, displayName, null, isEnabled);
        }
    }

    private static bool IsRootPermission(string? key) => !string.IsNullOrWhiteSpace(key) && key.StartsWith("Root.", StringComparison.OrdinalIgnoreCase);

    private sealed class UserRoleComparer : IEqualityComparer<UserRole>
    {
        public bool Equals(UserRole? x, UserRole? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return string.Equals(x.UserId, y.UserId, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.RoleId, y.RoleId, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(UserRole obj)
        {
            return HashCode.Combine(obj.UserId?.ToLowerInvariant(), obj.RoleId?.ToLowerInvariant());
        }
    }
}
