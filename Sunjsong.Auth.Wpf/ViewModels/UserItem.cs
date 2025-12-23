using CommunityToolkit.Mvvm.ComponentModel;

namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed class UserItem : ObservableObject
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _accountUserName = string.Empty;
    private string _description = string.Empty;
    private string _roleName = string.Empty;
    private bool _isRoot;
    private bool _isAdmin;
    private bool _isEnabled = true;
    private string? _selectedRoleId;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string AccountUserName
    {
        get => _accountUserName;
        set => SetProperty(ref _accountUserName, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public string RoleName
    {
        get => _roleName;
        set => SetProperty(ref _roleName, value);
    }

    public bool IsRoot
    {
        get => _isRoot;
        set => SetProperty(ref _isRoot, value);
    }

    public bool IsAdmin
    {
        get => _isAdmin;
        set => SetProperty(ref _isAdmin, value);
    }

    public string? SelectedRoleId
    {
        get => _selectedRoleId;
        set => SetProperty(ref _selectedRoleId, value);
    }
}
