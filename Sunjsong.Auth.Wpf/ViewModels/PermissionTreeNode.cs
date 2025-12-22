using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed class PermissionTreeNode : ObservableObject
{
    private string _name = string.Empty;
    private string? _key;
    private bool _isSelected;
    private ObservableCollection<PermissionTreeNode> _children = new();

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string? Key
    {
        get => _key;
        set => SetProperty(ref _key, value);
    }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ObservableCollection<PermissionTreeNode> Children
    {
        get => _children;
        set => SetProperty(ref _children, value);
    }
}
