using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using NEngineEditor.Model;

namespace NEngineEditor.Converters
{
    public class LogLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LogEntry.LogLevel logLevel)
            {
                return logLevel switch
                {
                    LogEntry.LogLevel.INFO => Brushes.Blue,
                    LogEntry.LogLevel.WARNING => Brushes.DarkOrange,
                    LogEntry.LogLevel.ERROR => Brushes.Red,
                    _ => Brushes.Black,
                };
            }
            return Brushes.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}