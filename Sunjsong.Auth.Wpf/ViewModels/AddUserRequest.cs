namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed record AddUserRequest(
    string Account,
    string Name,
    string Description,
    string? Password,
    string? RoleId,
    bool IsEnabled);
