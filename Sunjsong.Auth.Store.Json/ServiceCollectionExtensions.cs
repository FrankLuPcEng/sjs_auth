using Microsoft.Extensions.DependencyInjection;
using Sunjsong.Auth.Abstractions;

namespace Sunjsong.Auth.Store.Json;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSunjsongAuthorizationJsonStore(
        this IServiceCollection services,
        Action<JsonRbacStoreOptions> setup)
    {
        var options = new JsonRbacStoreOptions();
        setup(options);
        services.AddSingleton(options);
        services.AddSingleton<IRbacStore, JsonRbacStore>();
        return services;
    }
}
