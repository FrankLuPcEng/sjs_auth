using System.IO;
using System.Text.Json;
using RbacWpfDemo.Models;

namespace RbacWpfDemo.Services;

public interface IDeviceRepository
{
    Task<IReadOnlyList<Device>> LoadAsync();

    Task SaveAsync(IEnumerable<Device> devices);
}

public sealed class DeviceRepository : IDeviceRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public DeviceRepository(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<IReadOnlyList<Device>> LoadAsync()
    {
        if (!File.Exists(_filePath))
        {
            var seeds = GetSeedDevices();
            await SaveAsync(seeds);
            return seeds;
        }

        await using var stream = File.OpenRead(_filePath);
        var devices = await JsonSerializer.DeserializeAsync<List<Device>>(stream, _jsonOptions)
                      ?? new List<Device>();

        return devices;
    }

    public async Task SaveAsync(IEnumerable<Device> devices)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, devices, _jsonOptions);
    }

    private static List<Device> GetSeedDevices()
    {
        return new List<Device>
        {
            new()
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Edge Camera",
                Category = "Video",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new()
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Door Sensor",
                Category = "Sensor",
                Status = "Inactive",
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            },
            new()
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = "Smart Panel",
                Category = "HMI",
                Status = "Active",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
    }
}
