using Sunjsong.Auth.Abstractions;

namespace Sunjsong.Auth.WpfUI.PermissionCatalog;

public sealed class EmptyPermissionCatalog : IPermissionCatalog
{
    public IReadOnlyCollection<PermissionDefinition> GetAll()
    {
        return Array.Empty<PermissionDefinition>();
    }
}
