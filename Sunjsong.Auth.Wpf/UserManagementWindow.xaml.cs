using System.Linq;
using System.Windows;
using Sunjsong.Auth.WpfUI.ViewModels;
using Wpf.Ui.Controls;

namespace Sunjsong.Auth.WpfUI;

public partial class UserManagementWindow : FluentWindow
{
    private readonly UserManagementViewModel _viewModel;

    public UserManagementWindow(UserManagementViewModel viewModel)
    {
        _viewModel = viewModel;
        InitializeComponent();
        DataContext = _viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await _viewModel.InitializeAsync();
    }

    private async void OnEditUserClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { DataContext: UserItem user })
        {
            return;
        }

        var model = new UserEditModel
        {
            UserName = user.AccountUserName,
            Name = user.Name,
            Description = user.Description,
            SelectedRoleId = user.SelectedRoleId,
            IsEnabled = user.IsEnabled,
            Roles = _viewModel.Roles.ToList()
        };

        var dialog = new UserEditDialog(model)
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        user.AccountUserName = model.UserName;
        user.Name = model.Name;
        user.Description = model.Description;
        user.SelectedRoleId = model.SelectedRoleId;
        user.RoleName = _viewModel.Roles.FirstOrDefault(r => r.Id == model.SelectedRoleId)?.Name ?? string.Empty;
        user.IsEnabled = model.IsEnabled;

        await _viewModel.SaveUserWithRoleAsync(user);
    }

    private async void OnChangePasswordClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { DataContext: UserItem user })
        {
            return;
        }

        var model = new ChangePasswordModel();
        var dialog = new ChangePasswordDialog(model) { Owner = this };

        if (dialog.ShowDialog() == true)
        {
            await _viewModel.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
        }
    }

    private async void OnEditRoleClick(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { DataContext: RoleItem role })
        {
            return;
        }

        var model = new RoleEditDialogModel
        {
            RoleId = role.Id,
            RoleName = role.Name,
            Nodes = _viewModel.BuildPermissionTree(role)
        };

        var dialog = new RoleEditDialog(model) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            await _viewModel.SaveRoleWithPermissionsAsync(role, model.RoleName, dialog.SelectedKeys);
        }
    }

    private async void OnAddUserClick(object sender, RoutedEventArgs e)
    {
        var model = new AddUserDialogModel
        {
            Roles = new System.Collections.ObjectModel.ObservableCollection<RoleItem>(_viewModel.Roles)
        };

        var dialog = new AddUserDialog(model) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            var request = new AddUserRequest(
                model.Account,
                model.Name,
                model.Description,
                string.IsNullOrWhiteSpace(model.Password) ? null : model.Password,
                model.SelectedRoleId,
                model.IsEnabled);

            await _viewModel.AddUserFromDialogAsync(request);
        }
    }

    private async void OnAddRoleClick(object sender, RoutedEventArgs e)
    {
        var model = new AddRoleDialogModel();
        var dialog = new AddRoleDialog(model) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            await _viewModel.AddRoleFromDialogAsync(new AddRoleRequest(model.Name));
        }
    }
}
