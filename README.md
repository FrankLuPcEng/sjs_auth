# sjs_auth

A lightweight RBAC (role-based access control) toolkit with pluggable stores.

## Projects

- **Sunjsong.Auth.Abstractions**: shared contracts and models.
- **Sunjsong.Auth.Core**: authorization engine and default user context.
- **Sunjsong.Auth.Store.Json**: JSON-backed RBAC store.
- **RbacWpfDemo**: sample WPF demo app.

## Usage

### 1) Register services

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sunjsong.Auth.Abstractions;
using Sunjsong.Auth.Core;
using Sunjsong.Auth.Store.Json;

var services = new ServiceCollection();

services.AddSingleton<IPermissionCatalog, DemoPermissionCatalog>();
services.AddSunjsongAuthorizationCore();
services.AddSunjsongAuthorizationJsonStore(options =>
{
    options.FilePath = Path.Combine(AppContext.BaseDirectory, "rbac.json");
});

var provider = services.BuildServiceProvider();
```

### 2) Define permissions

```csharp
using Sunjsong.Auth.Abstractions;

public sealed class DemoPermissionCatalog : IPermissionCatalog
{
    public IReadOnlyCollection<PermissionDefinition> GetAll()
    {
        return new[]
        {
            new PermissionDefinition { Key = "reports.view", Name = "View reports" },
            new PermissionDefinition { Key = "reports.edit", Name = "Edit reports" }
        };
    }
}
```

### 3) Load RBAC data and check permissions

```csharp
using Sunjsong.Auth.Abstractions;

var userContext = provider.GetRequiredService<IUserContext>();
var authorizationService = provider.GetRequiredService<IAuthorizationService>();

userContext.CurrentUserId = "user-1";
await authorizationService.RefreshAsync();

if (authorizationService.Can("reports.view"))
{
    // Allowed
}

authorizationService.Demand("reports.edit"); // Throws if not allowed
```

### 4) JSON store format

The JSON store file is created automatically when missing.

```json
{
  "users": [
    { "id": "user-1", "name": "Alice" }
  ],
  "roles": [
    { "id": "role-1", "name": "Report admins" }
  ],
  "userRoles": [
    { "userId": "user-1", "roleId": "role-1" }
  ],
  "rolePermissions": [
    { "roleId": "role-1", "permissionKey": "reports.edit" }
  ]
}
```

## Testing

```bash
dotnet test
```
