namespace Sunjsong.Auth.Abstractions;

public interface IPermissionCatalog
{
    IReadOnlyCollection<PermissionDefinition> GetAll();
}
