using Microsoft.Extensions.DependencyInjection;
using Sunjsong.Auth.WpfUI.PermissionCatalog;
using Sunjsong.Auth.WpfUI.Services;
using System.IO;
using System.Windows;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Sunjsong.Auth.WpfUI;

public partial class App : Application
{
    private ServiceProvider? _provider;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Acrylic, true);

        var services = new ServiceCollection();
        services.AddSunjsongAuthWpf(options =>
        {
            options.DatabasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App.db");
            options.PermissionCatalog = new DefaultPermissionCatalog();


        });

        _provider = services.BuildServiceProvider();
        var window = _provider.GetRequiredService<UserManagementWindow>();
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _provider?.Dispose();
        base.OnExit(e);
    }
}
