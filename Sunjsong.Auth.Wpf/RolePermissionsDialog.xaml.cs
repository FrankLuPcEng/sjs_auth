using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Sunjsong.Auth.WpfUI.ViewModels;
using Wpf.Ui.Controls;

namespace Sunjsong.Auth.WpfUI;

public partial class RolePermissionsDialog : FluentWindow
{
    public RolePermissionsDialog(RolePermissionDialogModel model)
    {
        InitializeComponent();
        DataContext = model;
    }

    public RolePermissionDialogModel Model => (RolePermissionDialogModel)DataContext;

    public IReadOnlyCollection<string> SelectedKeys => CollectSelected(Model.Nodes).ToArray();

    private static IEnumerable<string> CollectSelected(IEnumerable<PermissionTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (!string.IsNullOrWhiteSpace(node.Key) && node.IsSelected)
            {
                yield return node.Key;
            }

            foreach (var childKey in CollectSelected(node.Children))
            {
                yield return childKey;
            }
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
