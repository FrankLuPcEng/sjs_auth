namespace Sunjsong.Auth.Abstractions;

public interface IUserContext
{
    string CurrentUserId { get; set; }

    event EventHandler? UserChanged;
}
