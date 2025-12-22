using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Windows;
using RbacWpfDemo.Models;
using RbacWpfDemo.Services;

namespace RbacWpfDemo.ViewModels;

public sealed class DeviceCrudViewModel : INotifyPropertyChanged
{
    private readonly IDeviceRepository _repository;
    private Device? _selectedDevice;
    private string _statusMessage = "就緒";

    public DeviceCrudViewModel(IDeviceRepository repository)
    {
        _repository = repository;
        Devices = new ObservableCollection<Device>();

        AddCommand = new RelayCommand(AddDevice);
        DeleteCommand = new RelayCommand(DeleteSelected, () => SelectedDevice is not null);
        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => Devices.Count > 0);
        ReloadCommand = new RelayCommand(async () => await LoadAsync());

        Devices.CollectionChanged += (_, _) => UpdateCommandStates();
    }

    public ObservableCollection<Device> Devices { get; }

    public Device? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (SetProperty(ref _selectedDevice, value))
            {
                UpdateCommandStates();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public RelayCommand AddCommand { get; }

    public RelayCommand DeleteCommand { get; }

    public RelayCommand SaveCommand { get; }

    public RelayCommand ReloadCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task InitializeAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        var items = await _repository.LoadAsync();

        Application.Current.Dispatcher.Invoke(() =>
        {
            Devices.Clear();
            foreach (var device in items.OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase))
            {
                Devices.Add(device);
            }

            StatusMessage = $"已載入 {Devices.Count} 筆資料";
        });

        UpdateCommandStates();
    }

    private void AddDevice()
    {
        var device = new Device
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "New Device",
            Category = "Category",
            Status = "Active",
            CreatedAt = DateTime.UtcNow
        };

        Devices.Add(device);
        SelectedDevice = device;
        StatusMessage = "新增一筆資料，記得按下儲存。";

        UpdateCommandStates();
    }

    private void DeleteSelected()
    {
        if (SelectedDevice is null)
        {
            return;
        }

        var toRemove = SelectedDevice;
        Devices.Remove(toRemove);
        SelectedDevice = null;
        StatusMessage = "已刪除選取資料，記得按下儲存。";

        UpdateCommandStates();
    }

    private async Task SaveAsync()
    {
        await _repository.SaveAsync(Devices);
        StatusMessage = $"已儲存 {Devices.Count} 筆資料";
    }

    private void UpdateCommandStates()
    {
        DeleteCommand.RaiseCanExecuteChanged();
        SaveCommand.RaiseCanExecuteChanged();
    }

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
