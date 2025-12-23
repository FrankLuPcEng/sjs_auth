using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sunjsong.Auth.Abstractions;
using Sunjsong.Auth.WpfUI.Options;
using Sunjsong.Auth.WpfUI.Services;
using System.Collections.ObjectModel;

namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed class UserManagementViewModel : ObservableObject
{
    private const string RootUserId = "root";
    private const string RootRoleId = "root-role";
    private const string RootName = "ROOT";
    private const string RootDefaultPassword = "root123456";
    private const string AdminUserId = "admin";
    private const string AdminRoleId = "admin-role";
    private const string AdminName = "Administrator";
    private const string AdminDefaultPassword = "admin123";

    private readonly IRbacRepository _repository;
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly UserManagementOptions _options;
    private readonly ILocalAccountService _accounts;

    private bool _isBusy;
    private string _statusMessage = "Ready";
    private UserItem? _selectedUser;
    private RoleItem? _selectedRole;
    private List<UserRole> _userRoles = new();
    private List<RolePermission> _rolePermissions = new();

    public string CurrentIdentity { get; }

    public UserManagementViewModel(
        IRbacRepository repository,
        IPermissionCatalog permissionCatalog,
        UserManagementOptions options,
        ILocalAccountService accounts)
    {
        _repository = repository;
        _permissionCatalog = permissionCatalog;
        _options = options;
        _accounts = accounts;
        CurrentIdentity = BuildCurrentIdentity(options);

        Users = new ObservableCollection<UserItem>();
        Roles = new ObservableCollection<RoleItem>();
        PermissionSelections = new ObservableCollection<PermissionSelectionItem>();

        RefreshCommand = new AsyncRelayCommand(() => RunGuardedAsync(LoadSnapshotAsync), () => !IsBusy);
        AddUserCommand = new AsyncRelayCommand(() => RunGuardedAsync(AddUserAsync), () => !IsBusy);
        SaveUserCommand = new AsyncRelayCommand<object?>(user => RunGuardedAsync(() => SaveUserAsync(user as UserItem)), user => CanExecuteUserCommand(user as UserItem));
        DeleteUserCommand = new AsyncRelayCommand(() => RunGuardedAsync(DeleteUserAsync), () => !IsBusy && SelectedUser is not null && !SelectedUser.IsRoot);
        SaveUserRolesCommand = new AsyncRelayCommand<object?>(user => RunGuardedAsync(() => SaveUserRolesAsync(user as UserItem)), user => CanExecuteUserCommand(user as UserItem));
        SaveUserWithRoleCommand = new AsyncRelayCommand<object?>(user => RunGuardedAsync(() => SaveUserWithRoleAsync(user as UserItem)), user => CanExecuteUserCommand(user as UserItem));

        AddRoleCommand = new AsyncRelayCommand(() => RunGuardedAsync(AddRoleAsync), () => !IsBusy);
        SaveRoleCommand = new AsyncRelayCommand(() => RunGuardedAsync(SaveRoleAsync), () => !IsBusy && SelectedRole is not null && !IsRootRole(SelectedRole));
        DeleteRoleCommand = new AsyncRelayCommand(() => RunGuardedAsync(DeleteRoleAsync), () => !IsBusy && SelectedRole is not null && !IsRootRole(SelectedRole));
        SaveRolePermissionsCommand = new AsyncRelayCommand(() => RunGuardedAsync(SaveRolePermissionsAsync), () => !IsBusy && SelectedRole is not null && !IsRootRole(SelectedRole));
    }

    public string WindowTitle => _options.WindowTitle;

    public ObservableCollection<UserItem> Users { get; }
    public ObservableCollection<RoleItem> Roles { get; }
    public ObservableCollection<PermissionSelectionItem> PermissionSelections { get; }

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand AddUserCommand { get; }
    public IAsyncRelayCommand SaveUserCommand { get; }
    public IAsyncRelayCommand DeleteUserCommand { get; }
    public IAsyncRelayCommand SaveUserRolesCommand { get; }
    public IAsyncRelayCommand SaveUserWithRoleCommand { get; }
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

        await EnsureRootAsync();
        var snapshot = await _repository.LoadAsync();
        var roleLookup = snapshot.Roles.ToDictionary(r => r.Id, r => r.Name, StringComparer.OrdinalIgnoreCase);

        Users.Clear();
        foreach (var user in snapshot.Users.OrderBy(u => u.Name, StringComparer.OrdinalIgnoreCase))
        {
            var account = await _accounts.GetByUserIdAsync(user.Id);
            var assignedRole = snapshot.UserRoles.FirstOrDefault(x => x.UserId == user.Id)?.RoleId;
            var roleName = assignedRole is not null && roleLookup.TryGetValue(assignedRole, out var r) ? r : string.Empty;

            Users.Add(new UserItem
            {
                Id = user.Id,
                Name = user.Name,
                AccountUserName = account?.UserName ?? user.Name,
                Description = account?.DisplayName ?? user.Name,
                RoleName = roleName,
                IsRoot = string.Equals(user.Id, RootUserId, StringComparison.OrdinalIgnoreCase),
                IsAdmin = string.Equals(user.Id, AdminUserId, StringComparison.OrdinalIgnoreCase),
                IsEnabled = account?.IsEnabled ?? true,
                SelectedRoleId = assignedRole
            });
        }

        Roles.Clear();
        foreach (var role in snapshot.Roles.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
        {
            Roles.Add(new RoleItem
            {
                Id = role.Id,
                Name = role.Name,
                IsRoot = string.Equals(role.Id, RootRoleId, StringComparison.OrdinalIgnoreCase),
                IsAdmin = string.Equals(role.Id, AdminRoleId, StringComparison.OrdinalIgnoreCase)
            });
        }

        _userRoles = snapshot.UserRoles.ToList();
        _rolePermissions = snapshot.RolePermissions.ToList();

        SelectedUser = Users.FirstOrDefault(u => u.Id == currentUserId) ?? Users.FirstOrDefault();
        SelectedRole = Roles.FirstOrDefault(r => r.Id == currentRoleId) ?? Roles.FirstOrDefault();

        UpdatePermissionSelections();
        StatusMessage = $"Loaded {Users.Count} users / {Roles.Count} roles";
    }

    private async Task AddUserAsync()
    {
        var request = new AddUserRequest(
            $"user{Users.Count + 1}",
            $"User {Users.Count + 1}",
            $"User {Users.Count + 1}",
            null,
            null,
            true);

        await AddUserFromDialogAsync(request);
    }

    public Task AddUserFromDialogAsync(AddUserRequest request)
    {
        return RunGuardedAsync(() => AddUserInternalAsync(request));
    }

    private async Task AddUserInternalAsync(AddUserRequest request)
    {
        var account = request.Account?.Trim();
        var name = request.Name?.Trim();
        var desc = request.Description?.Trim();
        var password = string.IsNullOrWhiteSpace(request.Password) ? "P@ssword1!" : request.Password;
        if (string.Equals(request.RoleId, RootRoleId, StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = "Cannot assign ROOT role when creating a user";
            return;
        }

        if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Account and name cannot be empty";
            return;
        }

        var user = await _repository.CreateUserAsync(new UserUpsertInput
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name
        });

        await _accounts.UpsertAsync(user.Id, account, desc ?? name, password, request.IsEnabled);

        var assignedRole = request.RoleId;
        if (!string.IsNullOrWhiteSpace(assignedRole))
        {
            await _repository.CreateUserRoleAsync(new UserRoleUpsertInput { UserId = user.Id, RoleId = assignedRole });
            _userRoles = _userRoles.Concat(new[] { new UserRole { UserId = user.Id, RoleId = assignedRole } }).ToList();
        }

        var item = new UserItem
        {
            Id = user.Id,
            Name = user.Name,
            AccountUserName = account,
            Description = desc ?? user.Name,
            RoleName = Roles.FirstOrDefault(r => r.Id == assignedRole)?.Name ?? string.Empty,
            IsRoot = false,
            IsEnabled = request.IsEnabled,
            SelectedRoleId = assignedRole
        };

        Users.Add(item);
        SelectedUser = item;
        StatusMessage = $"Added {user.Name}";
    }

    public Task UpdateAccountAsync(UserItem user, string accountUserName, string? newPassword)
    {
        ArgumentNullException.ThrowIfNull(user);
        return RunGuardedAsync(() => UpdateAccountInternalAsync(user, accountUserName, newPassword, user.Description, user.IsEnabled));
    }

    private async Task UpdateAccountInternalAsync(UserItem user, string accountUserName, string? newPassword, string? description, bool isEnabled)
    {
        var trimmedAccount = accountUserName?.Trim();
        if (string.IsNullOrWhiteSpace(trimmedAccount))
        {
            StatusMessage = "Account cannot be empty";
            return;
        }

        await _accounts.UpsertAsync(
            user.Id,
            trimmedAccount,
            description?.Trim() ?? user.Name,
            string.IsNullOrWhiteSpace(newPassword) ? null : newPassword,
            isEnabled);

        user.AccountUserName = trimmedAccount;
        user.Description = description?.Trim() ?? user.Description;
        user.IsEnabled = isEnabled;
        StatusMessage = $"Updated account info for {user.Name}";
    }

    private async Task SaveUserAsync(UserItem? user)
    {
        var target = user ?? SelectedUser;
        if (target is null)
        {
            return;
        }
        if (target.IsRoot || target.IsAdmin)
        {
            StatusMessage = target.IsRoot ? "ROOT cannot be modified" : "ADMIN cannot be modified";
            return;
        }

        var trimmed = target.Name?.Trim() ?? string.Empty;
        var updated = await _repository.UpdateUserAsync(new UserUpsertInput
        {
            Id = target.Id,
            Name = trimmed
        });

        target.Name = updated.Name;
        var accountUserName = string.IsNullOrWhiteSpace(target.AccountUserName)
            ? updated.Name
            : target.AccountUserName.Trim();

        if (string.IsNullOrWhiteSpace(accountUserName))
        {
            StatusMessage = "Account cannot be empty";
            return;
        }

        await _accounts.UpsertAsync(
            updated.Id,
            accountUserName,
            target.Description?.Trim() ?? updated.Name,
            null,
            target.IsEnabled);

        StatusMessage = $"Updated user {updated.Name}";
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
        await _accounts.DeleteByUserIdAsync(targetId);

        Users.Remove(SelectedUser);
        _userRoles = _userRoles.Where(link => link.UserId != targetId).ToList();
        SelectedUser = Users.FirstOrDefault();

        StatusMessage = $"Deleted user {targetName}";
    }

    private async Task SaveUserRolesAsync(UserItem? user)
    {
        var target = user ?? SelectedUser;
        if (target is null)
        {
            return;
        }
        if (target.IsRoot || target.IsAdmin)
        {
            StatusMessage = target.IsRoot ? "ROOT cannot be modified" : "ADMIN cannot be modified";
            return;
        }

        var targetRoleId = target.SelectedRoleId;
        var existingRoleIds = _userRoles.Where(link => link.UserId == target.Id)
            .Select(link => link.RoleId)
            .ToList();

        foreach (var roleId in existingRoleIds)
        {
            await _repository.DeleteUserRoleAsync(new UserRoleKey { UserId = target.Id, RoleId = roleId });
        }

        if (!string.IsNullOrWhiteSpace(targetRoleId))
        {
            await _repository.CreateUserRoleAsync(new UserRoleUpsertInput { UserId = target.Id, RoleId = targetRoleId });
            _userRoles = _userRoles
                .Where(link => link.UserId != target.Id)
                .Concat(new[] { new UserRole { UserId = target.Id, RoleId = targetRoleId } })
                .ToList();
        }
        else
        {
            _userRoles = _userRoles.Where(link => link.UserId != target.Id).ToList();
        }

        target.RoleName = Roles.FirstOrDefault(r => r.Id == targetRoleId)?.Name ?? string.Empty;
        StatusMessage = $"Updated role for {target.Name}";
    }

    public async Task SaveUserWithRoleAsync(UserItem? user)
    {
        var target = user ?? SelectedUser;
        if (target is null)
        {
            return;
        }
        if (target.IsRoot || target.IsAdmin)
        {
            StatusMessage = target.IsRoot ? "ROOT cannot be modified" : "ADMIN cannot be modified";
            return;
        }

        await SaveUserAsync(target);
        await SaveUserRolesAsync(target);
        target.RoleName = Roles.FirstOrDefault(r => r.Id == target.SelectedRoleId)?.Name ?? string.Empty;
        StatusMessage = $"Saved {target.Name} (with role)";
    }

    public async Task ChangePasswordAsync(UserItem? user, string oldPassword, string newPassword)
    {
        var target = user ?? SelectedUser;
        if (target is null)
        {
            return;
        }
        if (target.IsRoot || target.IsAdmin)
        {
            StatusMessage = target.IsRoot ? "ROOT cannot be modified" : "ADMIN cannot be modified";
            return;
        }

        if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
        {
            StatusMessage = "Old and new password cannot be empty";
            return;
        }

        await RunGuardedAsync(async () =>
        {
            await _accounts.ChangePasswordAsync(target.Id, oldPassword, newPassword);
            StatusMessage = $"Changed password for {target.Name}";
        });
    }

    public ObservableCollection<PermissionTreeNode> BuildPermissionTree(RoleItem role)
    {
        if (role is null)
        {
            return new ObservableCollection<PermissionTreeNode>();
        }

        var assigned = _rolePermissions.Where(link => link.RoleId == role.Id)
            .Select(link => link.PermissionKey)
            .Where(key => IsRootRole(role) || !IsRootPermission(key))
            .ToHashSet(StringComparer.Ordinal);

        var definitions = _permissionCatalog.GetAll()
            ?.Where(def => IsRootRole(role) || !IsRootPermission(def.Key))
            .ToList() ?? new List<PermissionDefinition>();
        if (definitions.Count == 0 && assigned.Count > 0)
        {
            definitions = assigned.Select(key => new PermissionDefinition { Key = key, Name = key }).ToList();
        }

        var root = new Dictionary<string, PermissionTreeNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in definitions)
        {
            if (string.IsNullOrWhiteSpace(def.Key))
            {
                continue;
            }

            var segments = def.Key.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                continue;
            }

            PermissionTreeNode current;
            if (!root.TryGetValue(segments[0], out current!))
            {
                current = new PermissionTreeNode { Name = segments[0] };
                root[segments[0]] = current;
            }

            for (int i = 1; i < segments.Length; i++)
            {
                var part = segments[i];
                var next = current.Children.FirstOrDefault(c => string.Equals(c.Name, part, StringComparison.OrdinalIgnoreCase));
                if (next is null)
                {
                    next = new PermissionTreeNode { Name = part };
                    current.Children.Add(next);
                }
                current = next;
            }

            current.Key = def.Key;
            current.Name = string.IsNullOrWhiteSpace(def.Name) ? def.Key : def.Name;
            current.IsSelected = assigned.Contains(def.Key);
        }

        return new ObservableCollection<PermissionTreeNode>(root.Values.OrderBy(n => n.Name, StringComparer.OrdinalIgnoreCase));
    }

    private async Task AddRoleAsync()
    {
        await AddRoleFromDialogAsync(new AddRoleRequest($"Role {Roles.Count + 1}"));
    }

    public Task AddRoleFromDialogAsync(AddRoleRequest request)
    {
        return RunGuardedAsync(() => AddRoleInternalAsync(request));
    }

    private async Task AddRoleInternalAsync(AddRoleRequest request)
    {
        var name = request.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusMessage = "Role name cannot be empty";
            return;
        }

        var role = await _repository.CreateRoleAsync(new RoleUpsertInput
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name
        });

        var item = new RoleItem { Id = role.Id, Name = role.Name, IsRoot = false, IsAdmin = string.Equals(role.Id, AdminRoleId, StringComparison.OrdinalIgnoreCase) };
        Roles.Add(item);
        SelectedRole = item;
        StatusMessage = $"Added role {role.Name}";
    }

    private async Task SaveRoleAsync()
    {
        if (SelectedRole is null)
        {
            return;
        }
        if (IsRootRole(SelectedRole) || SelectedRole.IsAdmin)
        {
            StatusMessage = SelectedRole.IsRoot ? "ROOT role cannot be modified" : "ADMIN role cannot be modified";
            return;
        }

        var trimmed = SelectedRole.Name?.Trim() ?? string.Empty;
        var updated = await _repository.UpdateRoleAsync(new RoleUpsertInput
        {
            Id = SelectedRole.Id,
            Name = trimmed
        });

        SelectedRole.Name = updated.Name;
        StatusMessage = $"Updated role {updated.Name}";
    }

    public async Task SaveRoleWithPermissionsAsync(RoleItem role, string roleName, IEnumerable<string> selectedKeys)
    {
        ArgumentNullException.ThrowIfNull(role);
        if (string.Equals(role.Id, RootRoleId, StringComparison.OrdinalIgnoreCase) || string.Equals(role.Id, AdminRoleId, StringComparison.OrdinalIgnoreCase))
        {
            StatusMessage = string.Equals(role.Id, RootRoleId, StringComparison.OrdinalIgnoreCase)
                ? "ROOT role cannot be modified"
                : "ADMIN role cannot be modified";
            return;
        }

        var trimmed = roleName?.Trim() ?? string.Empty;
        var updated = await _repository.UpdateRoleAsync(new RoleUpsertInput
        {
            Id = role.Id,
            Name = trimmed
        });

        role.Name = updated.Name;
        await SaveRolePermissionsAsync(role, selectedKeys);

        foreach (var user in Users.Where(u => u.SelectedRoleId == role.Id))
        {
            user.RoleName = updated.Name;
        }

        StatusMessage = $"Saved role {updated.Name} with permissions";
    }

    private async Task DeleteRoleAsync()
    {
        if (SelectedRole is null)
        {
            return;
        }
        if (IsRootRole(SelectedRole) || SelectedRole.IsAdmin)
        {
            StatusMessage = SelectedRole.IsRoot ? "ROOT role cannot be modified" : "ADMIN role cannot be modified";
            return;
        }

        var targetId = SelectedRole.Id;
        var targetName = SelectedRole.Name;
        await _repository.DeleteRoleAsync(targetId);

        Roles.Remove(SelectedRole);
        _userRoles = _userRoles.Where(link => link.RoleId != targetId).ToList();
        _rolePermissions = _rolePermissions.Where(link => link.RoleId != targetId).ToList();
        SelectedRole = Roles.FirstOrDefault();
        UpdatePermissionSelections();

        StatusMessage = $"Deleted role {targetName}";
    }

    private async Task SaveRolePermissionsAsync()
    {
        if (SelectedRole is null)
        {
            return;
        }

        var selectedKeys = PermissionSelections.Where(p => p.IsSelected).Select(p => p.Key);
        await SaveRolePermissionsAsync(SelectedRole, selectedKeys);
    }

    public async Task SaveRolePermissionsAsync(RoleItem role, IEnumerable<string> selectedKeys)
    {
        ArgumentNullException.ThrowIfNull(role);

        var selectedSet = selectedKeys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Where(k => IsRootRole(role) || !IsRootPermission(k))
            .ToHashSet(StringComparer.Ordinal);

        var existingKeys = _rolePermissions.Where(link => link.RoleId == role.Id)
            .Select(link => link.PermissionKey)
            .ToHashSet(StringComparer.Ordinal);

        var toAdd = selectedSet.Except(existingKeys).ToList();
        var toRemove = existingKeys.Except(selectedSet).ToList();

        foreach (var key in toRemove)
        {
            await _repository.DeleteRolePermissionAsync(new RolePermissionKey
            {
                RoleId = role.Id,
                PermissionKey = key
            });
        }

        foreach (var key in toAdd)
        {
            await _repository.CreateRolePermissionAsync(new RolePermissionUpsertInput
            {
                RoleId = role.Id,
                PermissionKey = key
            });
        }

        _rolePermissions = _rolePermissions
            .Where(link => link.RoleId != role.Id)
            .Concat(selectedSet.Select(key => new RolePermission { RoleId = role.Id, PermissionKey = key }))
            .ToList();

        StatusMessage = $"Updated permissions for role {role.Name}";
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
            StatusMessage = $"Error: {ex.Message}";
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
        SaveUserWithRoleCommand.NotifyCanExecuteChanged();
        AddRoleCommand.NotifyCanExecuteChanged();
        SaveRoleCommand.NotifyCanExecuteChanged();
        DeleteRoleCommand.NotifyCanExecuteChanged();
        SaveRolePermissionsCommand.NotifyCanExecuteChanged();
    }

    private bool CanExecuteUserCommand(UserItem? user)
    {
        var target = user ?? SelectedUser;
        return !IsBusy && target is not null && !target.IsRoot && !target.IsAdmin;
    }

    private static string BuildCurrentIdentity(UserManagementOptions options)
    {
        var user = options.CurrentUserName?.Trim();
        var role = options.CurrentRoleName?.Trim();

        if (!string.IsNullOrWhiteSpace(user) && !string.IsNullOrWhiteSpace(role))
        {
            return $"目前身份：{user}（{role}）";
        }

        if (!string.IsNullOrWhiteSpace(user))
        {
            return $"目前身份：{user}";
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            return $"目前角色：{role}";
        }

        return "目前身份：未設定";
    }

    private bool IsRootRole(RoleItem role) => string.Equals(role.Id, RootRoleId, StringComparison.OrdinalIgnoreCase);
    private bool IsRootPermission(string? key) => !string.IsNullOrWhiteSpace(key) && key.StartsWith("Root.", StringComparison.OrdinalIgnoreCase);

    private async Task EnsureRootAsync()
    {
        var snapshot = await _repository.LoadAsync();
        var users = snapshot.Users.ToDictionary(u => u.Id, StringComparer.OrdinalIgnoreCase);
        var roles = snapshot.Roles.ToDictionary(r => r.Id, StringComparer.OrdinalIgnoreCase);
        var userRoles = snapshot.UserRoles.ToHashSet(new UserRoleComparer());
        var permKeys = _permissionCatalog.GetAll()?.Select(p => p.Key).Where(k => !string.IsNullOrWhiteSpace(k)).ToList() ?? new List<string>();

        if (!users.ContainsKey(RootUserId))
        {
            await _repository.CreateUserAsync(new UserUpsertInput { Id = RootUserId, Name = RootName });
        }
        else if (!string.Equals(users[RootUserId].Name, RootName, StringComparison.Ordinal))
        {
            await _repository.UpdateUserAsync(new UserUpsertInput { Id = RootUserId, Name = RootName });
        }

        if (!roles.ContainsKey(RootRoleId))
        {
            await _repository.CreateRoleAsync(new RoleUpsertInput { Id = RootRoleId, Name = RootName });
        }
        else if (!string.Equals(roles[RootRoleId].Name, RootName, StringComparison.Ordinal))
        {
            await _repository.UpdateRoleAsync(new RoleUpsertInput { Id = RootRoleId, Name = RootName });
        }

        if (!userRoles.Contains(new UserRole { UserId = RootUserId, RoleId = RootRoleId }))
        {
            await _repository.CreateUserRoleAsync(new UserRoleUpsertInput { UserId = RootUserId, RoleId = RootRoleId });
        }

        var existingRolePerms = snapshot.RolePermissions.Where(rp => rp.RoleId == RootRoleId).Select(rp => rp.PermissionKey).ToHashSet(StringComparer.Ordinal);
        foreach (var key in permKeys)
        {
            if (!existingRolePerms.Contains(key))
            {
                await _repository.CreateRolePermissionAsync(new RolePermissionUpsertInput { RoleId = RootRoleId, PermissionKey = key });
            }
        }

        var account = await _accounts.GetByUserIdAsync(RootUserId);
        if (account is null)
        {
            await _accounts.UpsertAsync(RootUserId, RootName, RootName, RootDefaultPassword, true);
        }
        else if (!account.IsEnabled || !string.Equals(account.UserName, RootName, StringComparison.OrdinalIgnoreCase))
        {
            await _accounts.UpsertAsync(RootUserId, RootName, RootName, null, true);
        }

        // Admin seeding (all non-root permissions)
        if (!users.ContainsKey(AdminUserId))
        {
            await _repository.CreateUserAsync(new UserUpsertInput { Id = AdminUserId, Name = AdminName });
        }
        else if (!string.Equals(users[AdminUserId].Name, AdminName, StringComparison.Ordinal))
        {
            await _repository.UpdateUserAsync(new UserUpsertInput { Id = AdminUserId, Name = AdminName });
        }

        if (!roles.ContainsKey(AdminRoleId))
        {
            await _repository.CreateRoleAsync(new RoleUpsertInput { Id = AdminRoleId, Name = AdminName });
        }
        else if (!string.Equals(roles[AdminRoleId].Name, AdminName, StringComparison.Ordinal))
        {
            await _repository.UpdateRoleAsync(new RoleUpsertInput { Id = AdminRoleId, Name = AdminName });
        }

        if (!userRoles.Contains(new UserRole { UserId = AdminUserId, RoleId = AdminRoleId }))
        {
            await _repository.CreateUserRoleAsync(new UserRoleUpsertInput { UserId = AdminUserId, RoleId = AdminRoleId });
        }

        var adminPerms = permKeys.Where(k => !IsRootPermission(k)).ToList();
        var existingAdminPerms = snapshot.RolePermissions.Where(rp => rp.RoleId == AdminRoleId).Select(rp => rp.PermissionKey).ToHashSet(StringComparer.Ordinal);
        foreach (var key in adminPerms)
        {
            if (!existingAdminPerms.Contains(key))
            {
                await _repository.CreateRolePermissionAsync(new RolePermissionUpsertInput { RoleId = AdminRoleId, PermissionKey = key });
            }
        }

        var adminAccount = await _accounts.GetByUserIdAsync(AdminUserId);
        if (adminAccount is null)
        {
            await _accounts.UpsertAsync(AdminUserId, AdminName, AdminName, AdminDefaultPassword, true);
        }
        else if (!adminAccount.IsEnabled || !string.Equals(adminAccount.UserName, AdminName, StringComparison.OrdinalIgnoreCase))
        {
            await _accounts.UpsertAsync(AdminUserId, AdminName, AdminName, null, true);
        }
    }

    private sealed class UserRoleComparer : IEqualityComparer<UserRole>
    {
        public bool Equals(UserRole? x, UserRole? y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;
            return string.Equals(x.UserId, y.UserId, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(x.RoleId, y.RoleId, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(UserRole obj)
        {
            return HashCode.Combine(obj.UserId?.ToLowerInvariant(), obj.RoleId?.ToLowerInvariant());
        }
    }
}
