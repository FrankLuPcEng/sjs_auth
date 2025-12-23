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

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.Acrylic, true);

        var services = new ServiceCollection();
        services.AddSunjsongAuthWpf(options =>
        {
            options.DatabasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App.db");
            options.PermissionCatalog = new DefaultPermissionCatalog();
        });

        _provider = services.BuildServiceProvider();

        // Seed ROOT/ADMIN before login
        var repo = _provider.GetRequiredService<Sunjsong.Auth.Abstractions.IRbacRepository>();
        var catalog = _provider.GetRequiredService<Sunjsong.Auth.Abstractions.IPermissionCatalog>();
        var accounts = _provider.GetRequiredService<Sunjsong.Auth.WpfUI.Services.ILocalAccountService>();
        SystemAccountSeeder.SeedAsync(repo, catalog, accounts).GetAwaiter().GetResult();

        var login = _provider.GetRequiredService<LoginDialog>();
        var loginOk = login.ShowDialog() == true && login.Result is not null;

        if (loginOk & login.Result is not null)
        {
            var res = login.Result;
            var options = _provider.GetRequiredService<Options.UserManagementOptions>();
            options.CurrentUserName = res.UserName;
            options.CurrentRoleName = res.RoleNames.FirstOrDefault();
        }


        var window = _provider.GetRequiredService<UserManagementWindow>();
        //MainWindow = window;
        window.ShowDialog();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _provider?.Dispose();
        base.OnExit(e);
    }
}
