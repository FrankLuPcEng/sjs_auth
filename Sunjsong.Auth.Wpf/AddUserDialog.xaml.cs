using System.Windows;
using Sunjsong.Auth.WpfUI.ViewModels;
using Wpf.Ui.Controls;
using PasswordBox = System.Windows.Controls.PasswordBox;

namespace Sunjsong.Auth.WpfUI;

public partial class AddUserDialog : FluentWindow
{
    public AddUserDialog(AddUserDialogModel model)
    {
        InitializeComponent();
        DataContext = model;
    }

    public AddUserDialogModel Model => (AddUserDialogModel)DataContext;

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AddUserDialogModel model && sender is PasswordBox box)
        {
            model.Password = box.Password;
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
