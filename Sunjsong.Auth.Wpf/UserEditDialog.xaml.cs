using System.Windows;
using Sunjsong.Auth.WpfUI.ViewModels;
using Wpf.Ui.Controls;

namespace Sunjsong.Auth.WpfUI;

public partial class UserEditDialog : FluentWindow
{
    public UserEditDialog(UserEditModel model)
    {
        InitializeComponent();
        DataContext = model;
    }

    public UserEditModel Model => (UserEditModel)DataContext;

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
