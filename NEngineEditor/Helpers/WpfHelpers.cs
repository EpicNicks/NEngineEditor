using System.Windows.Media;
using System.Windows;

namespace NEngineEditor.Helpers;
public static class WpfHelpers
{
    public static T? GetChildOfType<T>(this DependencyObject? depObj) where T : DependencyObject
    {
        if (depObj == null) return null;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
        {
            var child = VisualTreeHelper.GetChild(depObj, i);

            var result = (child as T) ?? child.GetChildOfType<T>();
            if (result != null) return result;
        }
        return null;
    }
}
