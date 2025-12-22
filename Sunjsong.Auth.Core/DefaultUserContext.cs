using Sunjsong.Auth.Abstractions;

namespace Sunjsong.Auth.Core;

public sealed class DefaultUserContext : IUserContext
{
    private string _currentUserId = string.Empty;

    public string CurrentUserId
    {
        get => _currentUserId;
        set
        {
            if (string.Equals(_currentUserId, value, StringComparison.Ordinal))
            {
                return;
            }

            _currentUserId = value;
            UserChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? UserChanged;
}
