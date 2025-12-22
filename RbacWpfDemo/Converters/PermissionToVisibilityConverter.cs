using System.Globalization;
using System.Windows;
using System.Windows.Data;
using RbacWpfDemo.Services;

namespace RbacWpfDemo.Converters;

public sealed class PermissionToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var permissionKey = parameter as string;
        if (string.IsNullOrWhiteSpace(permissionKey))
        {
            return Visibility.Collapsed;
        }

        var canAccess = AuthorizationServiceLocator.AuthorizationService.Can(permissionKey);
        return canAccess ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
