using CommunityToolkit.Mvvm.ComponentModel;

namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed class PermissionSelectionItem : ObservableObject
{
    private string _key = string.Empty;
    private string _name = string.Empty;
    private bool _isSelected;

    public string Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
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
