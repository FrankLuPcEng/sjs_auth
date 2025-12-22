using Microsoft.Extensions.DependencyInjection;
using Sunjsong.Auth.Abstractions;

namespace RbacWpfDemo.Services;

public static class AuthorizationServiceLocator
{
    public static IServiceProvider? Provider { get; set; }

    public static IAuthorizationService AuthorizationService
    {
        get
        {
            if (Provider is null)
            {
                throw new InvalidOperationException("AuthorizationServiceLocator.Provider has not been initialized.");
            }

            return Provider.GetRequiredService<IAuthorizationService>();
        }
    }
}
