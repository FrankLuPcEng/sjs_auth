using System.Collections.Generic;

namespace Sunjsong.Auth.WpfUI.ViewModels;

public sealed record LoginSuccessResult(
    string UserId,
    string UserName,
    string DisplayName,
    IReadOnlyList<string> RoleIds,
    IReadOnlyList<string> RoleNames,
    bool IsRoot,
    bool IsAdmin);
