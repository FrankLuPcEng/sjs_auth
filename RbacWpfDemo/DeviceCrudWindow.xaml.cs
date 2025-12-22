using System.Windows;
using RbacWpfDemo.ViewModels;

namespace RbacWpfDemo;

public partial class DeviceCrudWindow : Window
{
    public DeviceCrudWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DeviceCrudViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }
}
