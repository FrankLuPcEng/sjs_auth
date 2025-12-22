using Sunjsong.Auth.Abstractions;

namespace Sunjsong.Auth.WpfUI.PermissionCatalog;

public sealed class DefaultPermissionCatalog : IPermissionCatalog
{
    public IReadOnlyCollection<PermissionDefinition> GetAll()
    {
        return new[]
        {
            new PermissionDefinition { Key = "User.Read", Name = "檢視使用者" },
            new PermissionDefinition { Key = "User.Write", Name = "編輯使用者" },
            new PermissionDefinition { Key = "Role.Read", Name = "檢視角色" },
            new PermissionDefinition { Key = "Role.Write", Name = "編輯角色" },
            new PermissionDefinition { Key = "RolePermission.Write", Name = "管理角色權限" }
        };
    }
}
