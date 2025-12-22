using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Sunjsong.Auth.Abstractions;
using Sunjsong.Auth.Store.Json;
using Xunit;

namespace Sunjsong.Auth.Tests;

public sealed class JsonRbacStoreTests
{
    [Fact]
    public async Task LoadAsync_CreatesDefaultSnapshot_WhenFileIsMissing()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var filePath = Path.Combine(tempDirectory, "rbac.json");
        var store = new JsonRbacStore(new JsonRbacStoreOptions { FilePath = filePath });

        var snapshot = await store.LoadAsync();

        Assert.True(File.Exists(filePath));
        Assert.Empty(snapshot.Users);
        Assert.Empty(snapshot.Roles);
        Assert.Empty(snapshot.UserRoles);
        Assert.Empty(snapshot.RolePermissions);
    }

    [Fact]
    public async Task LoadAsync_ReadsExistingSnapshot()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        var filePath = Path.Combine(tempDirectory, "rbac.json");
        var expected = new RbacSnapshot
        {
            Users = new[] { new User { Id = "user-1", Name = "User" } },
            Roles = new[] { new Role { Id = "role-1", Name = "Role" } },
            UserRoles = new[] { new UserRole { UserId = "user-1", RoleId = "role-1" } },
            RolePermissions = new[] { new RolePermission { RoleId = "role-1", PermissionKey = "perm.read" } }
        };
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(expected));
        var store = new JsonRbacStore(new JsonRbacStoreOptions { FilePath = filePath });

        var snapshot = await store.LoadAsync();

        Assert.Single(snapshot.Users);
        Assert.Equal("user-1", snapshot.Users[0].Id);
        Assert.Single(snapshot.Roles);
        Assert.Single(snapshot.UserRoles);
        Assert.Single(snapshot.RolePermissions);
    }
}
