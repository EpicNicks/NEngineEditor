using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

using NEngineEditor.Model;
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
        if (logListView.SelectedItem is LogEntry logItem)
        {
            Clipboard.SetText($"{logItem.Level}: {logItem.Message}");
        }
    }
}
