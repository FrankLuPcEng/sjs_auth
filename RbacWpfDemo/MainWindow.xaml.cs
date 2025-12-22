using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RbacWpfDemo.Services;
using RbacWpfDemo.ViewModels;

namespace RbacWpfDemo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenCrudDemo_Click(object sender, RoutedEventArgs e)
    {
        if (AuthorizationServiceLocator.Provider is null)
        {
            MessageBox.Show("Service provider 尚未初始化。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var viewModel = AuthorizationServiceLocator.Provider.GetRequiredService<DeviceCrudViewModel>();
        var window = new DeviceCrudWindow
        {
            Owner = this,
            DataContext = viewModel
        };

        window.Show();
    }
}
