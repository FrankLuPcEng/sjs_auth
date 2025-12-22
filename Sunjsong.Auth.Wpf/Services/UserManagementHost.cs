using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Sunjsong.Auth.WpfUI.Options;
using Sunjsong.Auth.WpfUI.ViewModels;

namespace Sunjsong.Auth.WpfUI.Services;

public static class UserManagementHost
{
    public static Window ShowDialog(Action<UserManagementOptions>? configure = null, Window? owner = null)
    {
        var services = new ServiceCollection();
        services.AddSunjsongAuthWpf(configure);

        var provider = services.BuildServiceProvider();
        var window = provider.GetRequiredService<UserManagementWindow>();

        if (owner is not null)
        {
            window.Owner = owner;
        }

        window.Closed += (_, _) => provider.Dispose();
        window.ShowDialog();
        return window;
    }

    public static Window CreateWindow(IServiceProvider provider)
    {
        return provider.GetRequiredService<UserManagementWindow>();
    }
}
