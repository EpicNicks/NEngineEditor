using System.Windows;

using NEngineEditor.ViewModel;

namespace NEngineEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = MainViewModel.Instance;
    }

    private void SaveMenuItemClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Save Clicked");
    }

    private void EditMenuItemClick(object sender, RoutedEventArgs e)
    {
        // check for unsaved changes and ask before close
        Close();
    }
}
