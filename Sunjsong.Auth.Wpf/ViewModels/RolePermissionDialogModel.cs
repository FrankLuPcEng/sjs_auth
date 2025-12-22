using System.Collections.ObjectModel;

namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed class RolePermissionDialogModel
{
    public ObservableCollection<PermissionTreeNode> Nodes { get; init; } = new();
}
