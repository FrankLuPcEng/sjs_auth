using Sunjsong.Auth.Abstractions;

namespace RbacWpfDemo.PermissionCatalog;

public sealed class DemoPermissionCatalog : IPermissionCatalog
{
    private static readonly IReadOnlyCollection<PermissionDefinition> Permissions = new List<PermissionDefinition>
    {
        new PermissionDefinition
        {
            Key = "Device.Read",
            Name = "Read device",
            Description = "Open device details."
        },
        new PermissionDefinition
        {
            Key = "Device.Edit",
            Name = "Edit device",
            Description = "Edit device details."
        },
        new PermissionDefinition
        {
            Key = "Report.View",
            Name = "View report",
            Description = "View report content."
        }
    };

    public IReadOnlyCollection<PermissionDefinition> GetAll() => Permissions;
}
