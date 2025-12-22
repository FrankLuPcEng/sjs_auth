using System.Data;
using System.IO;
using Microsoft.Data.Sqlite;
using Sunjsong.Auth.Store.Sqlite;

namespace Sunjsong.Auth.WpfUI.Services;

public interface ILocalAccountService
{
    // User-linked operations
    Task<LocalAccountRecord?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task<LocalAccountRecord> UpsertAsync(string userId, string userName, string displayName, string? newPassword, bool isEnabled = true, CancellationToken ct = default);
    Task ChangePasswordAsync(string userId, string oldPassword, string newPassword, CancellationToken ct = default);
    Task DeleteByUserIdAsync(string userId, CancellationToken ct = default);

    // Utility for listing all (used by account manager UI)
    Task<IReadOnlyList<LocalAccountRecord>> GetAllAsync(CancellationToken ct = default);

    // Legacy-style operations (id-based)
    Task<LocalAccountRecord> CreateAsync(string userName, string displayName, string password, bool isEnabled = true, CancellationToken ct = default);
    Task<LocalAccountRecord> UpdateAsync(string id, string userName, string displayName, string? newPassword, bool isEnabled = true, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

public sealed record LocalAccountRecord(
    string Id,
    string UserId,
    string UserName,
    string DisplayName,
    bool IsEnabled,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed class LocalAccountService : ILocalAccountService
{
    private readonly string _connectionString;

    public LocalAccountService(SqliteRbacStoreOptions options)
    {
        _connectionString = ResolveConnectionString(options);
    }

    public async Task<IReadOnlyList<LocalAccountRecord>> GetAllAsync(CancellationToken ct = default)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, UserId, UserName, DisplayName, IsEnabled, CreatedAt, UpdatedAt
            FROM LocalAccounts
            ORDER BY UserName COLLATE NOCASE ASC;
            """;

        var list = new List<LocalAccountRecord>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new LocalAccountRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetBoolean(4),
                reader.GetDateTimeOffset(5),
                reader.GetDateTimeOffset(6)));
        }

        return list;
    }

    public async Task<LocalAccountRecord> CreateAsync(string userName, string displayName, string password, bool isEnabled = true, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var id = Guid.NewGuid().ToString("N");
        var userId = id;
        return await UpsertAsync(userId, userName, displayName, password, isEnabled, ct);
    }

    public async Task<LocalAccountRecord> UpdateAsync(string id, string userName, string displayName, string? newPassword, bool isEnabled = true, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var userId = await GetUserIdByAccountIdAsync(id, ct) ?? id;
        return await UpsertAsync(userId, userName, displayName, newPassword, isEnabled, ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        var userId = await GetUserIdByAccountIdAsync(id, ct) ?? id;
        await DeleteByUserIdAsync(userId, ct);
    }

    public async Task<LocalAccountRecord?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        await using var connection = await OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, UserId, UserName, DisplayName, IsEnabled, PasswordHash, Salt, CreatedAt, UpdatedAt
            FROM LocalAccounts
            WHERE UserId = $userId;
            """;
        command.Parameters.AddWithValue("$userId", userId);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new LocalAccountRecord(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetBoolean(4),
                reader.GetDateTimeOffset(7),
                reader.GetDateTimeOffset(8));
        }

        return null;
    }

    public async Task<LocalAccountRecord> UpsertAsync(string userId, string userName, string displayName, string? newPassword, bool isEnabled = true, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userName);

        var now = DateTimeOffset.UtcNow;
        var updatePassword = !string.IsNullOrWhiteSpace(newPassword);
        var id = await GetAccountIdAsync(userId, ct) ?? Guid.NewGuid().ToString("N");
        var (hash, salt) = updatePassword ? PasswordHasher.HashPassword(newPassword!) : (null, null);

        await using var connection = await OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO LocalAccounts (Id, UserId, UserName, DisplayName, IsEnabled, PasswordHash, Salt, CreatedAt, UpdatedAt)
            VALUES ($id, $userId, $userName, $displayName, $isEnabled, COALESCE($hash, ''), COALESCE($salt, ''), $createdAt, $updatedAt)
            ON CONFLICT(UserId) DO UPDATE SET
                UserName = excluded.UserName,
                DisplayName = excluded.DisplayName,
                IsEnabled = excluded.IsEnabled,
                PasswordHash = CASE WHEN $hash IS NULL THEN LocalAccounts.PasswordHash ELSE excluded.PasswordHash END,
                Salt = CASE WHEN $salt IS NULL THEN LocalAccounts.Salt ELSE excluded.Salt END,
                UpdatedAt = excluded.UpdatedAt;
            """;

        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$userName", userName);
        command.Parameters.AddWithValue("$displayName", displayName ?? string.Empty);
        command.Parameters.AddWithValue("$isEnabled", isEnabled ? 1 : 0);
        command.Parameters.AddWithValue("$hash", (object?)hash ?? DBNull.Value);
        command.Parameters.AddWithValue("$salt", (object?)salt ?? DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", now);
        command.Parameters.AddWithValue("$updatedAt", now);

        await command.ExecuteNonQueryAsync(ct);

        return new LocalAccountRecord(id, userId, userName, displayName ?? string.Empty, isEnabled, now, now);
    }

    public async Task ChangePasswordAsync(string userId, string oldPassword, string newPassword, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(oldPassword);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        await using var connection = await OpenConnectionAsync(ct);

        string? storedHash = null;
        string? storedSalt = null;

        await using (var select = connection.CreateCommand())
        {
            select.CommandText = """
                SELECT PasswordHash, Salt
                FROM LocalAccounts
                WHERE UserId = $userId;
                """;
            select.Parameters.AddWithValue("$userId", userId);

            await using var reader = await select.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                storedHash = reader.GetString(0);
                storedSalt = reader.GetString(1);
            }
        }

        if (storedHash is null || storedSalt is null || !PasswordHasher.Verify(oldPassword, storedHash, storedSalt))
        {
            throw new InvalidOperationException("舊密碼不正確");
        }

        var (hash, salt) = PasswordHasher.HashPassword(newPassword);
        var now = DateTimeOffset.UtcNow;

        await using var update = connection.CreateCommand();
        update.CommandText = """
            UPDATE LocalAccounts
            SET PasswordHash = $hash,
                Salt = $salt,
                UpdatedAt = $updatedAt
            WHERE UserId = $userId;
            """;
        update.Parameters.AddWithValue("$hash", hash);
        update.Parameters.AddWithValue("$salt", salt);
        update.Parameters.AddWithValue("$updatedAt", now);
        update.Parameters.AddWithValue("$userId", userId);

        await update.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteByUserIdAsync(string userId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM LocalAccounts WHERE UserId = $userId;";
        command.Parameters.AddWithValue("$userId", userId);
        await command.ExecuteNonQueryAsync(ct);
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
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS LocalAccounts (
                    Id TEXT NOT NULL PRIMARY KEY,
                    UserId TEXT NOT NULL UNIQUE,
                    UserName TEXT NOT NULL UNIQUE,
                    DisplayName TEXT NOT NULL,
                    IsEnabled INTEGER NOT NULL DEFAULT 1,
                    PasswordHash TEXT NOT NULL,
                    Salt TEXT NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS IX_LocalAccounts_UserName ON LocalAccounts(UserName);
                CREATE INDEX IF NOT EXISTS IX_LocalAccounts_UserId ON LocalAccounts(UserId);
                """;
            await command.ExecuteNonQueryAsync(ct);
        }

        var hasIsEnabled = false;
        await using (var pragma = connection.CreateCommand())
        {
            pragma.CommandText = "PRAGMA table_info('LocalAccounts');";
            await using var reader = await pragma.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (reader.GetString(1).Equals("IsEnabled", StringComparison.OrdinalIgnoreCase))
                {
                    hasIsEnabled = true;
                    break;
                }
            }
        }

        if (!hasIsEnabled)
        {
            await using var alter = connection.CreateCommand();
            alter.CommandText = "ALTER TABLE LocalAccounts ADD COLUMN IsEnabled INTEGER NOT NULL DEFAULT 1;";
            await alter.ExecuteNonQueryAsync(ct);
        }
    }

    private async Task<string?> GetAccountIdAsync(string userId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id FROM LocalAccounts WHERE UserId = $userId LIMIT 1;";
        command.Parameters.AddWithValue("$userId", userId);
        var result = await command.ExecuteScalarAsync(ct);
        return result as string;
    }

    private async Task<string?> GetUserIdByAccountIdAsync(string accountId, CancellationToken ct)
    {
        await using var connection = await OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT UserId FROM LocalAccounts WHERE Id = $id LIMIT 1;";
        command.Parameters.AddWithValue("$id", accountId);
        var result = await command.ExecuteScalarAsync(ct);
        return result as string;
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
