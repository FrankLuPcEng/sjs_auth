using System.Windows;
using Sunjsong.Auth.WpfUI.ViewModels;
using Wpf.Ui.Controls;
using PasswordBox = System.Windows.Controls.PasswordBox;

namespace Sunjsong.Auth.WpfUI;

public partial class ChangePasswordDialog : FluentWindow
{
    public ChangePasswordDialog(ChangePasswordModel model)
    {
        InitializeComponent();
        DataContext = model;
    }

    public ChangePasswordModel Model => (ChangePasswordModel)DataContext;

    private void OnOldPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChangePasswordModel model && sender is PasswordBox box)
        {
            model.OldPassword = box.Password;
        }
    }

    private void OnNewPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ChangePasswordModel model && sender is PasswordBox box)
        {
            model.NewPassword = box.Password;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
