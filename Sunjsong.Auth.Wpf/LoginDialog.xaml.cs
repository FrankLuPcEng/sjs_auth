using System.Linq;
using System.Windows;
using Sunjsong.Auth.Abstractions;
using Sunjsong.Auth.WpfUI.Services;
using Sunjsong.Auth.WpfUI.ViewModels;
using Wpf.Ui.Controls;
using PasswordBox = System.Windows.Controls.PasswordBox;

namespace Sunjsong.Auth.WpfUI;

public partial class LoginDialog : FluentWindow
{
    private readonly ILocalAccountService _accounts;
    private readonly IRbacRepository _repository;

    public LoginDialog(ILocalAccountService accounts, IRbacRepository repository)
    {
        _accounts = accounts;
        _repository = repository;

        InitializeComponent();
        DataContext = new LoginDialogModel();
    }

    public LoginSuccessResult? Result { get; private set; }

    public LoginDialogModel Model => (LoginDialogModel)DataContext;

    private void OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is LoginDialogModel model && sender is PasswordBox box)
        {
            model.Password = box.Password;
        }
    }

    private async void OnLoginClick(object sender, RoutedEventArgs e)
    {
        Model.Error = string.Empty;
        var userName = Model.UserName?.Trim();
        var password = Model.Password ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
        {
            Model.Error = "帳號與密碼不可空白";
            return;
        }

        try
        {
            var account = await _accounts.AuthenticateAsync(userName, password);
            if (account is null)
            {
                Model.Error = "帳號或密碼錯誤，或帳號未啟用";
                return;
            }

            var snapshot = await _repository.LoadAsync();
            var roleIds = snapshot.UserRoles.Where(ur => ur.UserId == account.UserId).Select(ur => ur.RoleId).Distinct().ToList();
            var roleNames = snapshot.Roles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name).ToList();

            Result = new LoginSuccessResult(
                account.UserId,
                account.UserName,
                account.DisplayName,
                roleIds,
                roleNames,
                string.Equals(account.UserId, "root", StringComparison.OrdinalIgnoreCase),
                roleIds.Contains("admin-role", StringComparer.OrdinalIgnoreCase));

            DialogResult = true;
        }
        catch (Exception ex)
        {
            Model.Error = $"登入失敗：{ex.Message}";
        }
    }
}
