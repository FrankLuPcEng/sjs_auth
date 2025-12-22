using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Sunjsong.Auth.Abstractions;

namespace RbacWpfDemo.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IAuthorizationService _authorizationService;
    private readonly IUserContext _userContext;
    private readonly IRbacStore _rbacStore;
    private readonly IPermissionCatalog _permissionCatalog;
    private RbacSnapshot _snapshot = new();
    private string? _selectedUserId;
    private bool _canOpenDevice;
    private bool _canEditDevice;
    private bool _canViewReport;

    public MainViewModel(
        IAuthorizationService authorizationService,
        IUserContext userContext,
        IRbacStore rbacStore,
        IPermissionCatalog permissionCatalog)
    {
        _authorizationService = authorizationService;
        _userContext = userContext;
        _rbacStore = rbacStore;
        _permissionCatalog = permissionCatalog;

        Users = new ObservableCollection<User>();
        CurrentRoles = new ObservableCollection<string>();
        CurrentPermissions = new ObservableCollection<string>();

        OpenDeviceCommand = new RelayCommand(() => ExecuteDemand("Device.Read"), () => CanOpenDevice);
        EditDeviceCommand = new RelayCommand(() => ExecuteDemand("Device.Edit"), () => CanEditDevice);
        ViewReportCommand = new RelayCommand(() => ExecuteDemand("Report.View"), () => CanViewReport);

        _authorizationService.AuthorizationChanged += (_, _) =>
        {
            UpdateAuthorizationState();
        };
    }

    public ObservableCollection<User> Users { get; }

    public ObservableCollection<string> CurrentRoles { get; }

    public ObservableCollection<string> CurrentPermissions { get; }

    public RelayCommand OpenDeviceCommand { get; }

    public RelayCommand EditDeviceCommand { get; }

    public RelayCommand ViewReportCommand { get; }

    public string? SelectedUserId
    {
        get => _selectedUserId;
        set
        {
            if (SetProperty(ref _selectedUserId, value))
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    _userContext.CurrentUserId = value;
                }
            }
        }
    }

    public bool CanOpenDevice
    {
        get => _canOpenDevice;
        private set => SetProperty(ref _canOpenDevice, value);
    }

    public bool CanEditDevice
    {
        get => _canEditDevice;
        private set => SetProperty(ref _canEditDevice, value);
    }

    public bool CanViewReport
    {
        get => _canViewReport;
        private set => SetProperty(ref _canViewReport, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task InitializeAsync()
    {
        await _authorizationService.RefreshAsync().ConfigureAwait(false);
        _snapshot = await _rbacStore.LoadAsync().ConfigureAwait(false);
        LoadUsers();

        if (string.IsNullOrWhiteSpace(_userContext.CurrentUserId) && Users.Count > 0)
        {
            SelectedUserId = Users[0].Id;
        }

        UpdateAuthorizationState();
    }

    private void LoadUsers()
    {
        Users.Clear();
        foreach (var user in _snapshot.Users)
        {
            Users.Add(user);
        }
    }

    private void UpdateAuthorizationState()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            LoadCurrentRoles();
            LoadCurrentPermissions();
            UpdateCanFlags();
        });
    }

    private void LoadCurrentRoles()
    {
        CurrentRoles.Clear();
        var roleIds = _snapshot.UserRoles
            .Where(link => link.UserId == _userContext.CurrentUserId)
            .Select(link => link.RoleId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var role in _snapshot.Roles.Where(role => roleIds.Contains(role.Id)))
        {
            CurrentRoles.Add(role.Name);
        }
    }

    private void LoadCurrentPermissions()
    {
        CurrentPermissions.Clear();
        var roleIds = _snapshot.UserRoles
            .Where(link => link.UserId == _userContext.CurrentUserId)
            .Select(link => link.RoleId)
            .ToHashSet(StringComparer.Ordinal);

        var permissions = _snapshot.RolePermissions
            .Where(link => roleIds.Contains(link.RoleId))
            .Select(link => link.PermissionKey)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var catalog = _permissionCatalog.GetAll()
            .ToDictionary(def => def.Key, StringComparer.Ordinal);

        foreach (var permissionKey in permissions)
        {
            if (catalog.TryGetValue(permissionKey, out var definition))
            {
                CurrentPermissions.Add($"{definition.Key} - {definition.Name}");
            }
            else
            {
                CurrentPermissions.Add(permissionKey);
            }
        }
    }

    private void UpdateCanFlags()
    {
        CanOpenDevice = _authorizationService.Can("Device.Read");
        CanEditDevice = _authorizationService.Can("Device.Edit");
        CanViewReport = _authorizationService.Can("Report.View");

        OpenDeviceCommand.RaiseCanExecuteChanged();
        EditDeviceCommand.RaiseCanExecuteChanged();
        ViewReportCommand.RaiseCanExecuteChanged();
    }

    private void ExecuteDemand(string permissionKey)
    {
        try
        {
            _authorizationService.Demand(permissionKey);
            MessageBox.Show($"OK: {permissionKey}", "Authorization", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show($"沒有權限：{permissionKey}", "Authorization", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
