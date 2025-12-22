using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed class AddUserDialogModel : ObservableObject
{
    private string _account = string.Empty;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private string _password = string.Empty;
    private string? _selectedRoleId;
    private bool _isEnabled = true;
    private ObservableCollection<RoleItem> _roles = new();

    public string Account
    {
        get => _account;
        set => SetProperty(ref _account, value);
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

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
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

    public ObservableCollection<RoleItem> Roles
    {
        get => _roles;
        set => SetProperty(ref _roles, value);
    }
}
