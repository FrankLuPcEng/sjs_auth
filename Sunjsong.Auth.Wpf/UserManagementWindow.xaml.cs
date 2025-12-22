using System.Windows;
using Sunjsong.Auth.WpfUI.ViewModels;
using Wpf.Ui.Controls;

namespace Sunjsong.Auth.WpfUI;

public partial class UserManagementWindow : FluentWindow
{
    private readonly UserManagementViewModel _viewModel;

    public UserManagementWindow(UserManagementViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.InitializeAsync();
    }
}
