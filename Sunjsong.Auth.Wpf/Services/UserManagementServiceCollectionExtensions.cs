using Microsoft.Extensions.DependencyInjection;
using Sunjsong.Auth.Abstractions;
using Sunjsong.Auth.Core;
using Sunjsong.Auth.Store.Sqlite;
using Sunjsong.Auth.WpfUI.Options;
using Sunjsong.Auth.WpfUI.PermissionCatalog;
using Sunjsong.Auth.WpfUI.Services;
using Sunjsong.Auth.WpfUI.ViewModels;

namespace Sunjsong.Auth.WpfUI.Services;

public static class UserManagementServiceCollectionExtensions
{
    public static IServiceCollection AddSunjsongAuthWpf(this IServiceCollection services, Action<UserManagementOptions>? configure)
    {
        var options = new UserManagementOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton(sp => new SqliteRbacStoreOptions
        {
            ConnectionString = options.ConnectionString,
            DatabasePath = options.DatabasePath
        });
        services.AddSingleton<IPermissionCatalog>(_ => options.PermissionCatalog ?? new DefaultPermissionCatalog());

        services.AddSingleton<SqliteRbacRepository>();
        services.AddSingleton<IRbacRepository>(sp => sp.GetRequiredService<SqliteRbacRepository>());
        services.AddSingleton<IRbacStoreReader>(sp => sp.GetRequiredService<SqliteRbacRepository>());
        services.AddSingleton<IRbacStoreWriter>(sp => sp.GetRequiredService<SqliteRbacRepository>());
        services.AddSingleton<IRbacManagementService, RbacManagementService>();

        services.AddSingleton<ILocalAccountService, LocalAccountService>();

        services.AddSingleton<UserManagementViewModel>();
        services.AddTransient<AccountManagementViewModel>();
        services.AddSingleton<UserManagementWindow>();
        services.AddTransient<AccountManagementWindow>();

        return services;
    }
}
