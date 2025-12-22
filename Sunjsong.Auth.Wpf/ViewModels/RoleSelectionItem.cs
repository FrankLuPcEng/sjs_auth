using CommunityToolkit.Mvvm.ComponentModel;

namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed class RoleSelectionItem : ObservableObject
{
    private string _roleId = string.Empty;
    private string _name = string.Empty;
    private bool _isSelected;

    public string RoleId
    {
        get => _roleId;
        set => SetProperty(ref _roleId, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}
