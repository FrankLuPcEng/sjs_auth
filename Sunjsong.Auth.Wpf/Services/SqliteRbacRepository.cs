using System.IO;
using System.Text;
using Microsoft.Data.Sqlite;
using Sunjsong.Auth.Abstractions;
using Sunjsong.Auth.Store.Sqlite;

namespace Sunjsong.Auth.WpfUI.Services;

public sealed class SqliteRbacRepository : IRbacRepository
{
    private readonly string _connectionString;

    public SqliteRbacRepository(SqliteRbacStoreOptions options)
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

    public async Task<User> CreateUserAsync(UserUpsertInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        var id = string.IsNullOrWhiteSpace(input.Id) ? Guid.NewGuid().ToString("N") : input.Id;
        var name = input.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("User name is required.", nameof(input));
        }

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "INSERT INTO Users (Id, Name) VALUES ($id, $name);",
            ct,
            ("$id", id),
            ("$name", name));

        return new User { Id = id, Name = name };
    }

    public async Task<User> UpdateUserAsync(UserUpsertInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.Id))
        {
            throw new ArgumentException("User id is required.", nameof(input));
        }

        var name = input.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("User name is required.", nameof(input));
        }

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "UPDATE Users SET Name = $name WHERE Id = $id;",
            ct,
            ("$id", input.Id),
            ("$name", name));

        return new User { Id = input.Id, Name = name };
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

    public async Task<RbacPageResult<User>> QueryUsersAsync(UserQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (where, parameters) = BuildUserFilter(query);
        var paging = BuildPaging(query.Page);
        var sql = new StringBuilder("SELECT Id, Name FROM Users ");
        sql.Append(where);
        sql.Append(" ORDER BY Name COLLATE NOCASE ASC ");
        sql.Append(" LIMIT $limit OFFSET $offset;");

        await using var connection = await OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        command.Parameters.AddWithValue("$limit", paging.Limit);
        command.Parameters.AddWithValue("$offset", paging.Offset);

        var items = new List<User>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            items.Add(new User
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1)
            });
        }

        var total = await CountAsync(connection, "Users", where, parameters, ct);

        return new RbacPageResult<User>
        {
            Items = items,
            TotalCount = total,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize
        };
    }

    public async Task<Role> CreateRoleAsync(RoleUpsertInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var id = string.IsNullOrWhiteSpace(input.Id) ? Guid.NewGuid().ToString("N") : input.Id;
        var name = input.Name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name is required.", nameof(input));
        }

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "INSERT INTO Roles (Id, Name) VALUES ($id, $name);",
            ct,
            ("$id", id),
            ("$name", name));

        return new Role { Id = id, Name = name };
    }

    public async Task<Role> UpdateRoleAsync(RoleUpsertInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.Id))
        {
            throw new ArgumentException("Role id is required.", nameof(input));
        }

        var name = input.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name is required.", nameof(input));
        }

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "UPDATE Roles SET Name = $name WHERE Id = $id;",
            ct,
            ("$id", input.Id),
            ("$name", name));

        return new Role { Id = input.Id, Name = name };
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

    public async Task<RbacPageResult<Role>> QueryRolesAsync(RoleQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (where, parameters) = BuildRoleFilter(query);
        var paging = BuildPaging(query.Page);
        var sql = new StringBuilder("SELECT Id, Name FROM Roles ");
        sql.Append(where);
        sql.Append(" ORDER BY Name COLLATE NOCASE ASC ");
        sql.Append(" LIMIT $limit OFFSET $offset;");

        await using var connection = await OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        command.Parameters.AddWithValue("$limit", paging.Limit);
        command.Parameters.AddWithValue("$offset", paging.Offset);

        var items = new List<Role>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            items.Add(new Role
            {
                Id = reader.GetString(0),
                Name = reader.GetString(1)
            });
        }

        var total = await CountAsync(connection, "Roles", where, parameters, ct);

        return new RbacPageResult<Role>
        {
            Items = items,
            TotalCount = total,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize
        };
    }

    public async Task<UserRole> CreateUserRoleAsync(UserRoleUpsertInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.UserId) || string.IsNullOrWhiteSpace(input.RoleId))
        {
            throw new ArgumentException("UserId and RoleId are required.", nameof(input));
        }

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "INSERT OR IGNORE INTO UserRoles (UserId, RoleId) VALUES ($userId, $roleId);",
            ct,
            ("$userId", input.UserId),
            ("$roleId", input.RoleId));

        return new UserRole { UserId = input.UserId, RoleId = input.RoleId };
    }

    public async Task<UserRole> UpdateUserRoleAsync(UserRoleKey key, UserRoleUpsertInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(input);

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "DELETE FROM UserRoles WHERE UserId = $oldUserId AND RoleId = $oldRoleId;",
            ct,
            ("$oldUserId", key.UserId),
            ("$oldRoleId", key.RoleId));

        await ExecuteNonQueryAsync(
            connection,
            "INSERT OR IGNORE INTO UserRoles (UserId, RoleId) VALUES ($userId, $roleId);",
            ct,
            ("$userId", input.UserId),
            ("$roleId", input.RoleId));

        return new UserRole { UserId = input.UserId, RoleId = input.RoleId };
    }

    public async Task DeleteUserRoleAsync(UserRoleKey key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "DELETE FROM UserRoles WHERE UserId = $userId AND RoleId = $roleId;",
            ct,
            ("$userId", key.UserId),
            ("$roleId", key.RoleId));
    }

    public async Task<RbacPageResult<UserRole>> QueryUserRolesAsync(UserRoleQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (where, parameters) = BuildUserRoleFilter(query);
        var paging = BuildPaging(query.Page);

        var sql = new StringBuilder("SELECT UserId, RoleId FROM UserRoles ");
        sql.Append(where);
        sql.Append(" ORDER BY UserId, RoleId ");
        sql.Append(" LIMIT $limit OFFSET $offset;");

        await using var connection = await OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        command.Parameters.AddWithValue("$limit", paging.Limit);
        command.Parameters.AddWithValue("$offset", paging.Offset);

        var items = new List<UserRole>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            items.Add(new UserRole
            {
                UserId = reader.GetString(0),
                RoleId = reader.GetString(1)
            });
        }

        var total = await CountAsync(connection, "UserRoles", where, parameters, ct);

        return new RbacPageResult<UserRole>
        {
            Items = items,
            TotalCount = total,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize
        };
    }

    public async Task<RolePermission> CreateRolePermissionAsync(RolePermissionUpsertInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (string.IsNullOrWhiteSpace(input.RoleId) || string.IsNullOrWhiteSpace(input.PermissionKey))
        {
            throw new ArgumentException("RoleId and PermissionKey are required.", nameof(input));
        }

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "INSERT OR IGNORE INTO RolePermissions (RoleId, PermissionKey) VALUES ($roleId, $permissionKey);",
            ct,
            ("$roleId", input.RoleId),
            ("$permissionKey", input.PermissionKey));

        return new RolePermission { RoleId = input.RoleId, PermissionKey = input.PermissionKey };
    }

    public async Task<RolePermission> UpdateRolePermissionAsync(RolePermissionKey key, RolePermissionUpsertInput input, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(input);

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "DELETE FROM RolePermissions WHERE RoleId = $oldRoleId AND PermissionKey = $oldPermissionKey;",
            ct,
            ("$oldRoleId", key.RoleId),
            ("$oldPermissionKey", key.PermissionKey));

        await ExecuteNonQueryAsync(
            connection,
            "INSERT OR IGNORE INTO RolePermissions (RoleId, PermissionKey) VALUES ($roleId, $permissionKey);",
            ct,
            ("$roleId", input.RoleId),
            ("$permissionKey", input.PermissionKey));

        return new RolePermission { RoleId = input.RoleId, PermissionKey = input.PermissionKey };
    }

    public async Task DeleteRolePermissionAsync(RolePermissionKey key, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(key);

        await using var connection = await OpenConnectionAsync(ct);
        await ExecuteNonQueryAsync(
            connection,
            "DELETE FROM RolePermissions WHERE RoleId = $roleId AND PermissionKey = $permissionKey;",
            ct,
            ("$roleId", key.RoleId),
            ("$permissionKey", key.PermissionKey));
    }

    public async Task<RbacPageResult<RolePermission>> QueryRolePermissionsAsync(
        RolePermissionQuery query,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var (where, parameters) = BuildRolePermissionFilter(query);
        var paging = BuildPaging(query.Page);

        var sql = new StringBuilder("SELECT RoleId, PermissionKey FROM RolePermissions ");
        sql.Append(where);
        sql.Append(" ORDER BY RoleId, PermissionKey ");
        sql.Append(" LIMIT $limit OFFSET $offset;");

        await using var connection = await OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = sql.ToString();
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        command.Parameters.AddWithValue("$limit", paging.Limit);
        command.Parameters.AddWithValue("$offset", paging.Offset);

        var items = new List<RolePermission>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            items.Add(new RolePermission
            {
                RoleId = reader.GetString(0),
                PermissionKey = reader.GetString(1)
            });
        }

        var total = await CountAsync(connection, "RolePermissions", where, parameters, ct);

        return new RbacPageResult<RolePermission>
        {
            Items = items,
            TotalCount = total,
            PageNumber = paging.PageNumber,
            PageSize = paging.PageSize
        };
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
        command.CommandText = "SELECT Id, Name FROM Users ORDER BY Name COLLATE NOCASE ASC;";

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
        command.CommandText = "SELECT Id, Name FROM Roles ORDER BY Name COLLATE NOCASE ASC;";

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

    private static (string Where, List<SqliteParameter> Parameters) BuildUserFilter(UserQuery query)
    {
        var builder = new StringBuilder("WHERE 1=1 ");
        var parameters = new List<SqliteParameter>();

        if (!string.IsNullOrWhiteSpace(query.Id))
        {
            builder.Append(" AND Id = $id ");
            parameters.Add(new SqliteParameter("$id", query.Id));
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            builder.Append(" AND Name = $name ");
            parameters.Add(new SqliteParameter("$name", query.Name));
        }

        if (!string.IsNullOrWhiteSpace(query.NameContains))
        {
            builder.Append(" AND Name LIKE $nameContains ");
            parameters.Add(new SqliteParameter("$nameContains", $"%{query.NameContains}%"));
        }

        return (builder.ToString(), parameters);
    }

    private static (string Where, List<SqliteParameter> Parameters) BuildRoleFilter(RoleQuery query)
    {
        var builder = new StringBuilder("WHERE 1=1 ");
        var parameters = new List<SqliteParameter>();

        if (!string.IsNullOrWhiteSpace(query.Id))
        {
            builder.Append(" AND Id = $id ");
            parameters.Add(new SqliteParameter("$id", query.Id));
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            builder.Append(" AND Name = $name ");
            parameters.Add(new SqliteParameter("$name", query.Name));
        }

        if (!string.IsNullOrWhiteSpace(query.NameContains))
        {
            builder.Append(" AND Name LIKE $nameContains ");
            parameters.Add(new SqliteParameter("$nameContains", $"%{query.NameContains}%"));
        }

        return (builder.ToString(), parameters);
    }

    private static (string Where, List<SqliteParameter> Parameters) BuildUserRoleFilter(UserRoleQuery query)
    {
        var builder = new StringBuilder("WHERE 1=1 ");
        var parameters = new List<SqliteParameter>();

        if (!string.IsNullOrWhiteSpace(query.UserId))
        {
            builder.Append(" AND UserId = $userId ");
            parameters.Add(new SqliteParameter("$userId", query.UserId));
        }

        if (!string.IsNullOrWhiteSpace(query.RoleId))
        {
            builder.Append(" AND RoleId = $roleId ");
            parameters.Add(new SqliteParameter("$roleId", query.RoleId));
        }

        return (builder.ToString(), parameters);
    }

    private static (string Where, List<SqliteParameter> Parameters) BuildRolePermissionFilter(RolePermissionQuery query)
    {
        var builder = new StringBuilder("WHERE 1=1 ");
        var parameters = new List<SqliteParameter>();

        if (!string.IsNullOrWhiteSpace(query.RoleId))
        {
            builder.Append(" AND RoleId = $roleId ");
            parameters.Add(new SqliteParameter("$roleId", query.RoleId));
        }

        if (!string.IsNullOrWhiteSpace(query.PermissionKey))
        {
            builder.Append(" AND PermissionKey = $permissionKey ");
            parameters.Add(new SqliteParameter("$permissionKey", query.PermissionKey));
        }

        return (builder.ToString(), parameters);
    }

    private static async Task<int> CountAsync(
        SqliteConnection connection,
        string table,
        string whereClause,
        IEnumerable<SqliteParameter> parameters,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table} {whereClause};";

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        var result = await command.ExecuteScalarAsync(ct);
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    private static Paging BuildPaging(RbacPageRequest pageRequest)
    {
        var size = pageRequest?.PageSize > 0 ? pageRequest.PageSize : 50;
        var number = pageRequest?.PageNumber > 0 ? pageRequest.PageNumber : 1;
        var offset = (number - 1) * size;
        return new Paging(number, size, offset);
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

    private readonly record struct Paging(int PageNumber, int PageSize, int Offset)
    {
        public int Limit => PageSize;
    }
}
