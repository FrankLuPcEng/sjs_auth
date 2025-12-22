using CommunityToolkit.Mvvm.ComponentModel;

namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed class RoleItem : ObservableObject
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private bool _isRoot;

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

    public bool IsRoot
    {
        get => _isRoot;
        set => SetProperty(ref _isRoot, value);
    }
}
