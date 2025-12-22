using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Sunjsong.Auth.WpfUI.ViewModels;
using Wpf.Ui.Controls;

namespace Sunjsong.Auth.WpfUI;

public partial class RoleEditDialog : FluentWindow
{
    public RoleEditDialog(RoleEditDialogModel model)
    {
        InitializeComponent();
        DataContext = model;
    }

    public RoleEditDialogModel Model => (RoleEditDialogModel)DataContext;

    public IReadOnlyCollection<string> SelectedKeys => CollectSelected(Model.Nodes).ToArray();

    private static IEnumerable<string> CollectSelected(IEnumerable<PermissionTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (!string.IsNullOrWhiteSpace(node.Key) && node.IsSelected)
            {
                yield return node.Key;
            }

            foreach (var child in CollectSelected(node.Children))
            {
                yield return child;
            }
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
