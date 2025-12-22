using System.Globalization;
using System.Windows.Data;
using RbacWpfDemo.Services;

namespace RbacWpfDemo.Converters;

public sealed class PermissionToIsEnabledConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var permissionKey = parameter as string;
        if (string.IsNullOrWhiteSpace(permissionKey))
        {
            return false;
        }

        return AuthorizationServiceLocator.AuthorizationService.Can(permissionKey);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
