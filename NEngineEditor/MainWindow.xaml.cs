using System.Windows;

using NEngineEditor.ViewModel;

namespace NEngineEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(string projectPath)
    {
        // InitializeComponent creates SubComponents in the XAML so the shared project directory has to be set first
        //  yes I hate this singleton design right now but I just need it working for the time being
        MainViewModel.Instance.ProjectDirectory = projectPath;
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
