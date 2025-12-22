using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RbacWpfDemo.Models;

public sealed class Device : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _name = string.Empty;
    private string _category = string.Empty;
    private string _status = "Active";
    private DateTime _createdAt = DateTime.UtcNow;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set => SetProperty(ref _createdAt, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }

        storage = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
