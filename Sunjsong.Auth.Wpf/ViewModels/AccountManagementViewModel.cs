using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sunjsong.Auth.WpfUI.Services;

namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed class AccountManagementViewModel : ObservableObject
{
    private readonly ILocalAccountService _accounts;
    private bool _isBusy;
    private string _status = "就緒";
    private AccountItem? _selected;

    public AccountManagementViewModel(ILocalAccountService accounts)
    {
        _accounts = accounts;
        Accounts = new ObservableCollection<AccountItem>();

        RefreshCommand = new AsyncRelayCommand(() => RunGuardedAsync(LoadAsync), () => !IsBusy);
        AddCommand = new AsyncRelayCommand(() => RunGuardedAsync(AddAsync), () => !IsBusy);
        SaveCommand = new AsyncRelayCommand(() => RunGuardedAsync(SaveAsync), () => !IsBusy && Selected is not null);
        DeleteCommand = new AsyncRelayCommand(() => RunGuardedAsync(DeleteAsync), () => !IsBusy && Selected is not null);
    }

    public ObservableCollection<AccountItem> Accounts { get; }

    public AccountItem? Selected
    {
        get => _selected;
        set
        {
            if (SetProperty(ref _selected, value))
            {
                UpdateCommands();
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                UpdateCommands();
            }
        }
    }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand AddCommand { get; }
    public IAsyncRelayCommand SaveCommand { get; }
    public IAsyncRelayCommand DeleteCommand { get; }

    public async Task InitializeAsync()
    {
        await RunGuardedAsync(LoadAsync);
    }

    private async Task LoadAsync()
    {
        var list = await _accounts.GetAllAsync();
        Accounts.Clear();
        foreach (var acc in list)
        {
            Accounts.Add(new AccountItem
            {
                Id = acc.Id,
                UserName = acc.UserName,
                DisplayName = acc.DisplayName,
                CreatedAt = acc.CreatedAt,
                UpdatedAt = acc.UpdatedAt
            });
        }

        Selected = Accounts.FirstOrDefault();
        Status = $"已載入 {Accounts.Count} 筆帳號";
    }

    private async Task AddAsync()
    {
        var acc = await _accounts.CreateAsync(
            userName: $"user{Accounts.Count + 1}",
            displayName: $"使用者 {Accounts.Count + 1}",
            password: "P@ssword1!");

        var item = new AccountItem
        {
            Id = acc.Id,
            UserName = acc.UserName,
            DisplayName = acc.DisplayName,
            CreatedAt = acc.CreatedAt,
            UpdatedAt = acc.UpdatedAt
        };

        Accounts.Add(item);
        Selected = item;
        Status = $"已新增帳號 {acc.UserName}（預設密碼 P@ssword1!，請立即修改）";
    }

    private async Task SaveAsync()
    {
        if (Selected is null)
        {
            return;
        }

        var updated = await _accounts.UpdateAsync(
            Selected.Id,
            Selected.UserName.Trim(),
            Selected.DisplayName?.Trim() ?? string.Empty,
            string.IsNullOrWhiteSpace(Selected.NewPassword) ? null : Selected.NewPassword);

        Selected.UserName = updated.UserName;
        Selected.DisplayName = updated.DisplayName;
        Selected.UpdatedAt = updated.UpdatedAt;
        Selected.NewPassword = string.Empty;

        Status = $"已更新帳號 {updated.UserName}{(string.IsNullOrWhiteSpace(Selected.NewPassword) ? string.Empty : "（含密碼）")}";
    }

    private async Task DeleteAsync()
    {
        if (Selected is null)
        {
            return;
        }

        var name = Selected.UserName;
        await _accounts.DeleteAsync(Selected.Id);
        Accounts.Remove(Selected);
        Selected = Accounts.FirstOrDefault();

        Status = $"已刪除帳號 {name}";
    }

    private async Task RunGuardedAsync(Func<Task> action)
    {
        if (IsBusy)
        {
            return;
        }

        try
        {
            IsBusy = true;
            await action();
        }
        catch (Exception ex)
        {
            Status = $"錯誤：{ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateCommands()
    {
        RefreshCommand.NotifyCanExecuteChanged();
        AddCommand.NotifyCanExecuteChanged();
        SaveCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
    }
}
