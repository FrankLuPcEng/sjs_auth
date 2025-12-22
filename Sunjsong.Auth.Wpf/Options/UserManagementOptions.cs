using Sunjsong.Auth.Abstractions;
using System.IO;

namespace Sunjsong.Auth.WpfUI.Options;

public sealed class UserManagementOptions
{
    public string? ConnectionString { get; set; }

    public string DatabasePath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App.db");

    public string WindowTitle { get; set; } = "使用者管理";

    public IPermissionCatalog? PermissionCatalog { get; set; }
}
