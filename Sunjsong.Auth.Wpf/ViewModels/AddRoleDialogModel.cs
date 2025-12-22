using CommunityToolkit.Mvvm.ComponentModel;

namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed class AddRoleDialogModel : ObservableObject
{
    private string _name = string.Empty;

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
}
