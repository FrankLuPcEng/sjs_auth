# sjs_auth

A lightweight RBAC (role-based access control) toolkit with pluggable stores.

## Projects

- **Sunjsong.Auth.Abstractions**: shared contracts and models.
- **Sunjsong.Auth.Core**: authorization engine and default user context.
- **Sunjsong.Auth.Store.Json**: JSON-backed RBAC store.
- **Sunjsong.Auth.Store.Sqlite**: SQLite-backed RBAC store with CRUD support.
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

#### SQLite registration

```csharp
using Microsoft.Extensions.DependencyInjection;
using Sunjsong.Auth.Abstractions;
using Sunjsong.Auth.Core;
using Sunjsong.Auth.Store.Sqlite;

var services = new ServiceCollection();

services.AddSingleton<IPermissionCatalog, DemoPermissionCatalog>();
services.AddSunjsongAuthorizationCore();
services.AddSunjsongAuthorizationSqliteStore(options =>
{
    options.DatabasePath = Path.Combine(AppContext.BaseDirectory, "rbac.db");
    // Or configure a full connection string:
    // options.ConnectionString = "Data Source=rbac.db;";
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

### 5) SQLite CRUD example

```csharp
using Sunjsong.Auth.Abstractions;

var writer = provider.GetRequiredService<IRbacStoreWriter>();

await writer.CreateUserAsync(new User { Id = "user-1", Name = "Alice" });
await writer.CreateRoleAsync(new Role { Id = "role-1", Name = "Report admins" });
await writer.AddUserRoleAsync("user-1", "role-1");
await writer.AddRolePermissionAsync("role-1", "reports.edit");

await writer.RemoveRolePermissionAsync("role-1", "reports.edit");
await writer.RemoveUserRoleAsync("user-1", "role-1");
```

## Testing

```bash
dotnet test
```
