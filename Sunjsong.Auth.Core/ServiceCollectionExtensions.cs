using Microsoft.Extensions.DependencyInjection;
using Sunjsong.Auth.Abstractions;

namespace Sunjsong.Auth.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSunjsongAuthorizationCore(this IServiceCollection services)
    {
        services.AddSingleton<IUserContext, DefaultUserContext>();
        services.AddSingleton<IAuthorizationService, AuthorizationService>();
        return services;
    }

    public static IServiceCollection AddSunjsongAuthorizationManagement(this IServiceCollection services)
    {
        services.AddSingleton<IRbacManagementService, RbacManagementService>();
        return services;
    }
}
