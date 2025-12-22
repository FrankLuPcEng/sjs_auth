using System.Windows;
using Sunjsong.Auth.WpfUI.ViewModels;
using Wpf.Ui.Controls;

namespace Sunjsong.Auth.WpfUI;

public partial class AddRoleDialog : FluentWindow
{
    public AddRoleDialog(AddRoleDialogModel model)
    {
        InitializeComponent();
        DataContext = model;
    }

    public AddRoleDialogModel Model => (AddRoleDialogModel)DataContext;

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
