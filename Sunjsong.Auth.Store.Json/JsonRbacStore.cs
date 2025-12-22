using System.Text.Json;
using Sunjsong.Auth.Abstractions;

namespace Sunjsong.Auth.Store.Json;

public sealed class JsonRbacStore : IRbacStore
{
    private readonly JsonRbacStoreOptions _options;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public JsonRbacStore(JsonRbacStoreOptions options)
    {
        _options = options;
    }

    public async Task<RbacSnapshot> LoadAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.FilePath))
        {
            throw new InvalidOperationException("JsonRbacStoreOptions.FilePath must be configured.");
        }

        if (!File.Exists(_options.FilePath))
        {
            var defaultSnapshot = CreateDefaultSnapshot();
            var directory = Path.GetDirectoryName(_options.FilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await using var createStream = File.Create(_options.FilePath);
            await JsonSerializer.SerializeAsync(createStream, defaultSnapshot, _serializerOptions, ct)
                .ConfigureAwait(false);
            return defaultSnapshot;
        }

        await using var stream = File.OpenRead(_options.FilePath);
        var snapshot = await JsonSerializer.DeserializeAsync<RbacSnapshot>(stream, _serializerOptions, ct)
            .ConfigureAwait(false);

        return snapshot ?? new RbacSnapshot();
    }

    private static RbacSnapshot CreateDefaultSnapshot()
    {
        return new RbacSnapshot
        {
            Users = Array.Empty<User>(),
            Roles = Array.Empty<Role>(),
            UserRoles = Array.Empty<UserRole>(),
            RolePermissions = Array.Empty<RolePermission>()
        };
    }
}
