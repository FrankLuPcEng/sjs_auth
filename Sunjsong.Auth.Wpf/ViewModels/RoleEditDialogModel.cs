using System.Collections.ObjectModel;

namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed class RoleEditDialogModel
{
    public string RoleId { get; init; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public ObservableCollection<PermissionTreeNode> Nodes { get; init; } = new();
}
