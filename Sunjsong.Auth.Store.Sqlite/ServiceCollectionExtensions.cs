using Microsoft.Extensions.DependencyInjection;
using Sunjsong.Auth.Abstractions;

namespace Sunjsong.Auth.Store.Sqlite;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSunjsongAuthorizationSqliteStore(
        this IServiceCollection services,
        Action<SqliteRbacStoreOptions> setup)
    {
        var options = new SqliteRbacStoreOptions();
        setup(options);

        services.AddSingleton(options);
        services.AddSingleton<SqliteRbacStore>();
        services.AddSingleton<IRbacStore>(sp => sp.GetRequiredService<SqliteRbacStore>());
        services.AddSingleton<IRbacStoreWriter>(sp => sp.GetRequiredService<SqliteRbacStore>());

        return services;
    }
}
