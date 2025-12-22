using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sunjsong.Auth.Abstractions;
using Sunjsong.Auth.WpfUI.Options;

namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed class UserManagementViewModel : ObservableObject
{
    private readonly IRbacRepository _repository;
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly UserManagementOptions _options;
    private bool _isBusy;
    private string _statusMessage = "就緒";
    private UserItem? _selectedUser;
    private RoleItem? _selectedRole;
    private List<UserRole> _userRoles = new();
    private List<RolePermission> _rolePermissions = new();

    public UserManagementViewModel(
        IRbacRepository repository,
        IPermissionCatalog permissionCatalog,
        UserManagementOptions options)
    {
        _repository = repository;
        _permissionCatalog = permissionCatalog;
        _options = options;

        Users = new ObservableCollection<UserItem>();
        Roles = new ObservableCollection<RoleItem>();
        RoleSelections = new ObservableCollection<RoleSelectionItem>();
        PermissionSelections = new ObservableCollection<PermissionSelectionItem>();

        RefreshCommand = new AsyncRelayCommand(() => RunGuardedAsync(LoadSnapshotAsync), () => !IsBusy);
        AddUserCommand = new AsyncRelayCommand(() => RunGuardedAsync(AddUserAsync), () => !IsBusy);
        SaveUserCommand = new AsyncRelayCommand(() => RunGuardedAsync(SaveUserAsync), () => !IsBusy && SelectedUser is not null);
        DeleteUserCommand = new AsyncRelayCommand(() => RunGuardedAsync(DeleteUserAsync), () => !IsBusy && SelectedUser is not null);
        SaveUserRolesCommand = new AsyncRelayCommand(() => RunGuardedAsync(SaveUserRolesAsync), () => !IsBusy && SelectedUser is not null);

        AddRoleCommand = new AsyncRelayCommand(() => RunGuardedAsync(AddRoleAsync), () => !IsBusy);
        SaveRoleCommand = new AsyncRelayCommand(() => RunGuardedAsync(SaveRoleAsync), () => !IsBusy && SelectedRole is not null);
        DeleteRoleCommand = new AsyncRelayCommand(() => RunGuardedAsync(DeleteRoleAsync), () => !IsBusy && SelectedRole is not null);
        SaveRolePermissionsCommand = new AsyncRelayCommand(() => RunGuardedAsync(SaveRolePermissionsAsync), () => !IsBusy && SelectedRole is not null);
    }

    public string WindowTitle => _options.WindowTitle;

    public ObservableCollection<UserItem> Users { get; }

    public ObservableCollection<RoleItem> Roles { get; }

    public ObservableCollection<RoleSelectionItem> RoleSelections { get; }

    public ObservableCollection<PermissionSelectionItem> PermissionSelections { get; }

    public IAsyncRelayCommand RefreshCommand { get; }

    public IAsyncRelayCommand AddUserCommand { get; }

    public IAsyncRelayCommand SaveUserCommand { get; }

    public IAsyncRelayCommand DeleteUserCommand { get; }

    public IAsyncRelayCommand SaveUserRolesCommand { get; }

    public IAsyncRelayCommand AddRoleCommand { get; }

    public IAsyncRelayCommand SaveRoleCommand { get; }

    public IAsyncRelayCommand DeleteRoleCommand { get; }

    public IAsyncRelayCommand SaveRolePermissionsCommand { get; }

    public UserItem? SelectedUser
    {
        get => _selectedUser;
        set
        {
            if (SetProperty(ref _selectedUser, value))
            {
                UpdateRoleSelections();
                UpdateCommands();
            }
        }
    }

    public RoleItem? SelectedRole
    {
        get => _selectedRole;
        set
        {
            if (SetProperty(ref _selectedRole, value))
            {
                UpdatePermissionSelections();
                UpdateCommands();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                UpdateCommands();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public async Task InitializeAsync()
    {
        await RunGuardedAsync(LoadSnapshotAsync);
    }

    private async Task LoadSnapshotAsync()
    {
        var currentUserId = SelectedUser?.Id;
        var currentRoleId = SelectedRole?.Id;

        var snapshot = await _repository.LoadAsync();

        Users.Clear();
        foreach (var user in snapshot.Users.OrderBy(u => u.Name, StringComparer.OrdinalIgnoreCase))
        {
            Users.Add(new UserItem { Id = user.Id, Name = user.Name });
        }

        Roles.Clear();
        foreach (var role in snapshot.Roles.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
        {
            Roles.Add(new RoleItem { Id = role.Id, Name = role.Name });
        }

        _userRoles = snapshot.UserRoles.ToList();
        _rolePermissions = snapshot.RolePermissions.ToList();

        SelectedUser = Users.FirstOrDefault(u => u.Id == currentUserId) ?? Users.FirstOrDefault();
        SelectedRole = Roles.FirstOrDefault(r => r.Id == currentRoleId) ?? Roles.FirstOrDefault();

        UpdateRoleSelections();
        UpdatePermissionSelections();
        StatusMessage = $"已載入 {Users.Count} 位使用者 / {Roles.Count} 個角色";
    }

    private async Task AddUserAsync()
    {
        var name = $"使用者 {Users.Count + 1}";
        var user = await _repository.CreateUserAsync(new UserUpsertInput
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name
        });

        var item = new UserItem { Id = user.Id, Name = user.Name };
        Users.Add(item);
        SelectedUser = item;
        StatusMessage = $"已新增 {user.Name}";
    }

    private async Task SaveUserAsync()
    {
        if (SelectedUser is null)
        {
            return;
        }

        var trimmed = SelectedUser.Name?.Trim() ?? string.Empty;
        var updated = await _repository.UpdateUserAsync(new UserUpsertInput
        {
            Id = SelectedUser.Id,
            Name = trimmed
        });

        SelectedUser.Name = updated.Name;
        StatusMessage = $"已更新使用者 {updated.Name}";
    }

    private async Task DeleteUserAsync()
    {
        if (SelectedUser is null)
        {
            return;
        }

        var targetId = SelectedUser.Id;
        var targetName = SelectedUser.Name;

        await _repository.DeleteUserAsync(targetId);

        Users.Remove(SelectedUser);
        _userRoles = _userRoles.Where(link => link.UserId != targetId).ToList();
        SelectedUser = Users.FirstOrDefault();
        UpdateRoleSelections();

        StatusMessage = $"已刪除使用者 {targetName}";
    }

    private async Task SaveUserRolesAsync()
    {
        if (SelectedUser is null)
        {
            return;
        }

        var selectedRoleIds = RoleSelections.Where(r => r.IsSelected).Select(r => r.RoleId).ToHashSet(StringComparer.Ordinal);
        var existingRoleIds = _userRoles.Where(link => link.UserId == SelectedUser.Id)
            .Select(link => link.RoleId)
            .ToHashSet(StringComparer.Ordinal);

        var toAdd = selectedRoleIds.Except(existingRoleIds).ToList();
        var toRemove = existingRoleIds.Except(selectedRoleIds).ToList();

        foreach (var roleId in toRemove)
        {
            await _repository.DeleteUserRoleAsync(new UserRoleKey { UserId = SelectedUser.Id, RoleId = roleId });
        }

        foreach (var roleId in toAdd)
        {
            await _repository.CreateUserRoleAsync(new UserRoleUpsertInput { UserId = SelectedUser.Id, RoleId = roleId });
        }

        _userRoles = _userRoles
            .Where(link => link.UserId != SelectedUser.Id)
            .Concat(selectedRoleIds.Select(roleId => new UserRole { UserId = SelectedUser.Id, RoleId = roleId }))
            .ToList();

        StatusMessage = $"已更新 {SelectedUser.Name} 的角色";
    }

    private async Task AddRoleAsync()
    {
        var name = $"角色 {Roles.Count + 1}";
        var role = await _repository.CreateRoleAsync(new RoleUpsertInput
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name
        });

        var item = new RoleItem { Id = role.Id, Name = role.Name };
        Roles.Add(item);
        SelectedRole = item;
        UpdateRoleSelections();
        StatusMessage = $"已新增角色 {role.Name}";
    }

    private async Task SaveRoleAsync()
    {
        if (SelectedRole is null)
        {
            return;
        }

        var trimmed = SelectedRole.Name?.Trim() ?? string.Empty;
        var updated = await _repository.UpdateRoleAsync(new RoleUpsertInput
        {
            Id = SelectedRole.Id,
            Name = trimmed
        });

        SelectedRole.Name = updated.Name;
        StatusMessage = $"已更新角色 {updated.Name}";
        UpdateRoleSelections();
    }

    private async Task DeleteRoleAsync()
    {
        if (SelectedRole is null)
        {
            return;
        }

        var targetId = SelectedRole.Id;
        var targetName = SelectedRole.Name;
        await _repository.DeleteRoleAsync(targetId);

        Roles.Remove(SelectedRole);
        _userRoles = _userRoles.Where(link => link.RoleId != targetId).ToList();
        _rolePermissions = _rolePermissions.Where(link => link.RoleId != targetId).ToList();
        SelectedRole = Roles.FirstOrDefault();
        UpdateRoleSelections();
        UpdatePermissionSelections();

        StatusMessage = $"已刪除角色 {targetName}";
    }

    private async Task SaveRolePermissionsAsync()
    {
        if (SelectedRole is null)
        {
            return;
        }

        var selectedKeys = PermissionSelections.Where(p => p.IsSelected)
            .Select(p => p.Key)
            .ToHashSet(StringComparer.Ordinal);

        var existingKeys = _rolePermissions.Where(link => link.RoleId == SelectedRole.Id)
            .Select(link => link.PermissionKey)
            .ToHashSet(StringComparer.Ordinal);

        var toAdd = selectedKeys.Except(existingKeys).ToList();
        var toRemove = existingKeys.Except(selectedKeys).ToList();

        foreach (var key in toRemove)
        {
            await _repository.DeleteRolePermissionAsync(new RolePermissionKey
            {
                RoleId = SelectedRole.Id,
                PermissionKey = key
            });
        }

        foreach (var key in toAdd)
        {
            await _repository.CreateRolePermissionAsync(new RolePermissionUpsertInput
            {
                RoleId = SelectedRole.Id,
                PermissionKey = key
            });
        }

        _rolePermissions = _rolePermissions
            .Where(link => link.RoleId != SelectedRole.Id)
            .Concat(selectedKeys.Select(key => new RolePermission { RoleId = SelectedRole.Id, PermissionKey = key }))
            .ToList();

        StatusMessage = $"已更新角色 {SelectedRole.Name} 的權限";
    }

    private void UpdateRoleSelections()
    {
        RoleSelections.Clear();

        if (SelectedUser is null)
        {
            return;
        }

        var assigned = _userRoles.Where(link => link.UserId == SelectedUser.Id)
            .Select(link => link.RoleId)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var role in Roles.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
        {
            RoleSelections.Add(new RoleSelectionItem
            {
                RoleId = role.Id,
                Name = role.Name,
                IsSelected = assigned.Contains(role.Id)
            });
        }
    }

    private void UpdatePermissionSelections()
    {
        PermissionSelections.Clear();

        if (SelectedRole is null)
        {
            return;
        }

        var assigned = _rolePermissions.Where(link => link.RoleId == SelectedRole.Id)
            .Select(link => link.PermissionKey)
            .ToHashSet(StringComparer.Ordinal);

        var definitions = _permissionCatalog.GetAll()?.ToList() ?? new List<PermissionDefinition>();
        if (definitions.Count == 0 && assigned.Count > 0)
        {
            definitions = assigned.Select(key => new PermissionDefinition { Key = key, Name = key }).ToList();
        }

        foreach (var def in definitions.OrderBy(d => d.Name ?? d.Key, StringComparer.OrdinalIgnoreCase))
        {
            PermissionSelections.Add(new PermissionSelectionItem
            {
                Key = def.Key,
                Name = string.IsNullOrWhiteSpace(def.Name) ? def.Key : def.Name,
                IsSelected = assigned.Contains(def.Key)
            });
        }
    }

    private async Task RunGuardedAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            StatusMessage = $"錯誤：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateCommands()
    {
        RefreshCommand.NotifyCanExecuteChanged();
        AddUserCommand.NotifyCanExecuteChanged();
        SaveUserCommand.NotifyCanExecuteChanged();
        DeleteUserCommand.NotifyCanExecuteChanged();
        SaveUserRolesCommand.NotifyCanExecuteChanged();
        AddRoleCommand.NotifyCanExecuteChanged();
        SaveRoleCommand.NotifyCanExecuteChanged();
        DeleteRoleCommand.NotifyCanExecuteChanged();
        SaveRolePermissionsCommand.NotifyCanExecuteChanged();
    }
}
