using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed class UserEditModel : ObservableObject
{
    private string _userName = string.Empty;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string? _selectedRoleId;
    private bool _isEnabled = true;
    private IEnumerable<RoleItem> _roles = Array.Empty<RoleItem>();

    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public string? SelectedRoleId
    {
        get => _selectedRoleId;
        set => SetProperty(ref _selectedRoleId, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public IEnumerable<RoleItem> Roles
    {
        get => _roles;
        set => SetProperty(ref _roles, value);
    }
}
