using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using RbacWpfDemo.PermissionCatalog;
using RbacWpfDemo.Services;
using RbacWpfDemo.ViewModels;
using Sunjsong.Auth.Abstractions;
using Sunjsong.Auth.Core;
using Sunjsong.Auth.Store.Json;

namespace RbacWpfDemo;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        services.AddSunjsongAuthorizationCore();
        services.AddSunjsongAuthorizationJsonStore(options =>
        {
            options.FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rbac.json");
        });
        services.AddSingleton<IPermissionCatalog, DemoPermissionCatalog>();
        services.AddSingleton<IDeviceRepository>(_ => new DeviceRepository(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "devices.json")));
        services.AddSingleton<MainViewModel>();
        services.AddTransient<DeviceCrudViewModel>();

        _serviceProvider = services.BuildServiceProvider();
        AuthorizationServiceLocator.Provider = _serviceProvider;

        var mainWindow = new MainWindow
        {
            DataContext = _serviceProvider.GetRequiredService<MainViewModel>()
        };

        if (mainWindow.DataContext is MainViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
