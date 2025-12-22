using System.Windows;
using Sunjsong.Auth.WpfUI.ViewModels;
using Wpf.Ui.Controls;

namespace Sunjsong.Auth.WpfUI;

public partial class AccountManagementWindow : FluentWindow
{
    public AccountManagementWindow(AccountManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (DataContext is AccountManagementViewModel vm)
        {
            await vm.InitializeAsync();
        }
    }

    private void NewPasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is AccountManagementViewModel vm && vm.Selected is not null && sender is System.Windows.Controls.PasswordBox pb)
        {
            vm.Selected.NewPassword = pb.Password;
        }
    }
}
