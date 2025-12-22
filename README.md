# sjs_auth

A lightweight RBAC (role-based access control) toolkit with pluggable stores.

## Projects

- **Sunjsong.Auth.Abstractions**: shared contracts and models.
- **Sunjsong.Auth.Core**: authorization engine and default user context.
- **Sunjsong.Auth.Store.Json**: JSON-backed RBAC store.
- **Sunjsong.Auth.Store.Sqlite**: SQLite-backed RBAC store with CRUD support.
- **RbacWpfDemo**: sample WPF demo app.
- **Sunjsong.Auth.Wpf**: standalone WPF user/role manager (Wpf.Ui, SQLite).

## Quick start

1) 安裝 .NET 8 SDK，clone 後在倉庫根目錄執行：
   ```bash
   dotnet restore
   dotnet build
   ```
2) 運行 WPF 管理工具（預設使用 SQLite 檔案）：  
   ```bash
   dotnet run --project Sunjsong.Auth.Wpf
   ```
3) 在自己專案中引用核心套件（示例用 SQLite）：  
   ```csharp
   services.AddSunjsongAuthorizationCore();
   services.AddSunjsongAuthorizationSqliteStore(opts => opts.DatabasePath = "rbac.db");
   services.AddSingleton<IPermissionCatalog, MyPermissionCatalog>();
   ```
   設定 `IUserContext.CurrentUserId` 後呼叫 `authorizationService.RefreshAsync()` 並使用 `Can/Demand` 進行權限判斷。

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

### 4) Manage RBAC data (writer)

```csharp
using Sunjsong.Auth.Abstractions;
using Sunjsong.Auth.Core;

services.AddSunjsongAuthorizationManagement();

var manager = provider.GetRequiredService<IRbacManagementService>();

await manager.CreateUserAsync(new UserUpsertInput { Id = "user-2", Name = "Bob" });
await manager.CreateRoleAsync(new RoleUpsertInput { Id = "role-2", Name = "Report viewers" });
await manager.CreateUserRoleAsync(new UserRoleUpsertInput { UserId = "user-2", RoleId = "role-2" });
await manager.CreateRolePermissionAsync(new RolePermissionUpsertInput
{
    RoleId = "role-2",
    PermissionKey = "reports.view"
});

var users = await manager.QueryUsersAsync(new UserQuery
{
    NameContains = "Bob",
    Page = new RbacPageRequest { PageNumber = 1, PageSize = 20 }
});
```

> Note: management APIs require an `IRbacStoreWriter`/`IRbacRepository` implementation to be registered in DI.

### 5) JSON store format

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
