using Microsoft.Data.Sqlite;
using Sunjsong.Auth.Abstractions;

namespace Sunjsong.Auth.Store.Sqlite;

public sealed class SqliteRbacStore : IRbacStore
{
    private readonly string _connectionString;

    public SqliteRbacStore(SqliteRbacStoreOptions options)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _connectionString = ResolveConnectionString(options);
    }

    public async Task<RbacSnapshot> LoadAsync(CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);

        var users = await ReadUsersAsync(connection, ct);
        var roles = await ReadRolesAsync(connection, ct);
        var userRoles = await ReadUserRolesAsync(connection, ct);
        var rolePermissions = await ReadRolePermissionsAsync(connection, ct);

        return new RbacSnapshot
        {
            Users = users,
            Roles = roles,
            UserRoles = userRoles,
            RolePermissions = rolePermissions
        };
    }

    public async Task CreateUserAsync(User user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "INSERT INTO Users (Id, Name) VALUES ($id, $name);",
            ct,
            ("$id", user.Id),
            ("$name", user.Name));
    }

    public async Task UpdateUserAsync(User user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "UPDATE Users SET Name = $name WHERE Id = $id;",
            ct,
            ("$id", user.Id),
            ("$name", user.Name));
    }

    public async Task DeleteUserAsync(string userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "DELETE FROM Users WHERE Id = $id;",
            ct,
            ("$id", userId));
    }

    public async Task CreateRoleAsync(Role role, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "INSERT INTO Roles (Id, Name) VALUES ($id, $name);",
            ct,
            ("$id", role.Id),
            ("$name", role.Name));
    }

    public async Task UpdateRoleAsync(Role role, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(role);

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "UPDATE Roles SET Name = $name WHERE Id = $id;",
            ct,
            ("$id", role.Id),
            ("$name", role.Name));
    }

    public async Task DeleteRoleAsync(string roleId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("Role id is required.", nameof(roleId));
        }

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "DELETE FROM Roles WHERE Id = $id;",
            ct,
            ("$id", roleId));
    }

    public async Task AddUserRoleAsync(string userId, string roleId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("Role id is required.", nameof(roleId));
        }

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "INSERT INTO UserRoles (UserId, RoleId) VALUES ($userId, $roleId);",
            ct,
            ("$userId", userId),
            ("$roleId", roleId));
    }

    public async Task RemoveUserRoleAsync(string userId, string roleId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User id is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("Role id is required.", nameof(roleId));
        }

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "DELETE FROM UserRoles WHERE UserId = $userId AND RoleId = $roleId;",
            ct,
            ("$userId", userId),
            ("$roleId", roleId));
    }

    public async Task AddRolePermissionAsync(string roleId, string permissionKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("Role id is required.", nameof(roleId));
        }

        if (string.IsNullOrWhiteSpace(permissionKey))
        {
            throw new ArgumentException("Permission key is required.", nameof(permissionKey));
        }

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "INSERT INTO RolePermissions (RoleId, PermissionKey) VALUES ($roleId, $permissionKey);",
            ct,
            ("$roleId", roleId),
            ("$permissionKey", permissionKey));
    }

    public async Task RemoveRolePermissionAsync(string roleId, string permissionKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(roleId))
        {
            throw new ArgumentException("Role id is required.", nameof(roleId));
        }

        if (string.IsNullOrWhiteSpace(permissionKey))
        {
            throw new ArgumentException("Permission key is required.", nameof(permissionKey));
        }

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "DELETE FROM RolePermissions WHERE RoleId = $roleId AND PermissionKey = $permissionKey;",
            ct,
            ("$roleId", roleId),
            ("$permissionKey", permissionKey));
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken ct)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await EnsureSchemaAsync(connection, ct);
        return connection;
    }

    private static async Task EnsureSchemaAsync(SqliteConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            PRAGMA foreign_keys = ON;

            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS Roles (
                Id TEXT NOT NULL PRIMARY KEY,
                Name TEXT NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS UserRoles (
                UserId TEXT NOT NULL,
                RoleId TEXT NOT NULL,
                PRIMARY KEY (UserId, RoleId),
                FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
                FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS RolePermissions (
                RoleId TEXT NOT NULL,
                PermissionKey TEXT NOT NULL,
                PRIMARY KEY (RoleId, PermissionKey),
                FOREIGN KEY (RoleId) REFERENCES Roles(Id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS IX_UserRoles_UserId ON UserRoles(UserId);
            CREATE INDEX IF NOT EXISTS IX_UserRoles_RoleId ON UserRoles(RoleId);
            CREATE INDEX IF NOT EXISTS IX_RolePermissions_RoleId ON RolePermissions(RoleId);
            CREATE INDEX IF NOT EXISTS IX_RolePermissions_PermissionKey ON RolePermissions(PermissionKey);
            """;

        await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task<IReadOnlyList<User>> ReadUsersAsync(SqliteConnection connection, CancellationToken ct)
    {
        var results = new List<User>();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name FROM Users;";

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new User
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1)
            });
        }

        return results;
    }

    private static async Task<IReadOnlyList<Role>> ReadRolesAsync(SqliteConnection connection, CancellationToken ct)
    {
        var results = new List<Role>();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name FROM Roles;";

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new Role
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1)
            });
        }

        return results;
    }

    private static async Task<IReadOnlyList<UserRole>> ReadUserRolesAsync(SqliteConnection connection, CancellationToken ct)
    {
        var results = new List<UserRole>();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT UserId, RoleId FROM UserRoles;";

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new UserRole
            {
                UserId = reader.GetString(0),
                RoleId = reader.GetString(1)
            });
        }

        return results;
    }

    private static async Task<IReadOnlyList<RolePermission>> ReadRolePermissionsAsync(
        SqliteConnection connection,
        CancellationToken ct)
    {
        var results = new List<RolePermission>();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT RoleId, PermissionKey FROM RolePermissions;";

        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(new RolePermission
            {
                RoleId = reader.GetString(0),
                PermissionKey = reader.GetString(1)
            });
        }

        return results;
    }

    private static async Task ExecuteNonQueryAsync(
        SqliteConnection connection,
        string sql,
        CancellationToken ct,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        await command.ExecuteNonQueryAsync(ct);
    }

    private static string ResolveConnectionString(SqliteRbacStoreOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return options.ConnectionString;
        }

        if (!string.IsNullOrWhiteSpace(options.DatabasePath))
        {
            var directory = Path.GetDirectoryName(options.DatabasePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var builder = new SqliteConnectionStringBuilder
            {
                DataSource = options.DatabasePath
            };

            return builder.ToString();
        }

        throw new InvalidOperationException("SqliteRbacStoreOptions.ConnectionString or DatabasePath must be configured.");
    }
}
