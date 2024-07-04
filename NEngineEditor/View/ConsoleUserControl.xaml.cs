using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

using NEngineEditor.ViewModel;

namespace NEngineEditor.View;
/// <summary>
/// Interaction logic for ConsoleUserControl.xaml
/// </summary>
public partial class ConsoleUserControl : UserControl
{
    public ConsoleUserControl()
    {
        InitializeComponent();
        logListView.ItemsSource = MainViewModel.Instance.Logs;
        MainViewModel.Instance.Logs.CollectionChanged += Logs_CollectionChanged;
    }
    private void Logs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (logListView.Items.Count > 0)
        {
            logListView.ScrollIntoView(logListView.Items[^1]);
        }
    }

    private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
    {
        if (logListView.SelectedItem != null)
        {
            // Assuming your log items have Level and Message properties
            var logItem = (dynamic)logListView.SelectedItem;
            string clipboardText = $"{logItem.Level}: {logItem.Message}";
            Clipboard.SetText(clipboardText);
        }
    }
}
