using System.Globalization;
using System.Windows.Data;
using System.Windows;

namespace NEngineEditor.Converters;
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isInverse = parameter as string == "inverse";
        bool isVisible = value != null;
        if (isInverse) isVisible = !isVisible;
        return isVisible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}