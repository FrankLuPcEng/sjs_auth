using Sunjsong.Auth.Abstractions;
using Sunjsong.Auth.Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xunit;

namespace Sunjsong.Auth.Tests;

public sealed class AuthorizationServiceTests
{
    [Fact]
    public async Task Can_ReturnsTrue_WhenPermissionIsAssignedAndKnown()
    {
        var snapshot = new RbacSnapshot
        {
            UserRoles = new[]
            {
                new UserRole { UserId = "user-1", RoleId = "role-1" }
            },
            RolePermissions = new[]
            {
                new RolePermission { RoleId = "role-1", PermissionKey = "perm.read" }
            }
        };
        var store = new StubStore(snapshot);
        var userContext = new DefaultUserContext { CurrentUserId = "user-1" };
        var catalog = new StubPermissionCatalog(new[]
        {
            new PermissionDefinition { Key = "perm.read", Name = "Read" }
        });

        var service = new AuthorizationService(store, userContext, catalog);
        await service.RefreshAsync();

        Assert.True(service.Can("perm.read"));
        Assert.False(service.Can("perm.unknown"));
    }

    [Fact]
    public async Task Demand_Throws_WhenUserLacksPermission()
    {
        var store = new StubStore(new RbacSnapshot());
        var userContext = new DefaultUserContext { CurrentUserId = "user-1" };
        var catalog = new StubPermissionCatalog(new[]
        {
            new PermissionDefinition { Key = "perm.write", Name = "Write" }
        });

        var service = new AuthorizationService(store, userContext, catalog);
        await service.RefreshAsync();

        Assert.Throws<System.UnauthorizedAccessException>(() => service.Demand("perm.write"));
    }

    private sealed class StubStore : IRbacStoreReader
    {
        private readonly RbacSnapshot _snapshot;

        public StubStore(RbacSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public Task<RbacSnapshot> LoadAsync(CancellationToken ct = default)
        {
            return Task.FromResult(_snapshot);
        }
    }

    private sealed class StubPermissionCatalog : IPermissionCatalog
    {
        private readonly IReadOnlyCollection<PermissionDefinition> _definitions;

        public StubPermissionCatalog(IReadOnlyCollection<PermissionDefinition> definitions)
        {
            _definitions = definitions;
        }

        public IReadOnlyCollection<PermissionDefinition> GetAll()
        {
            return _definitions;
        }
    }
}
