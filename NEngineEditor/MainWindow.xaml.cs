using System.Collections.Specialized;
using System.Windows;

using NEngineEditor.ViewModel;

namespace NEngineEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static string ProjectDirectory { get; private set; } = "";

    public MainWindow(string projectPath)
    {
        ProjectDirectory = projectPath;
        InitializeComponent();
        MainViewModel.Instance.ContentBrowserViewModel = ContentBrowserControl.DataContext as ContentBrowserViewModel;
        MainViewModel.Instance.Logs.CollectionChanged += LoggerLogs_CollectionChanged;
        DataContext = MainViewModel.Instance;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    private void LoggerLogs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        BottomTabControl.SelectedItem = ConsoleTab;
    }

    private void SaveScene()
    {
        MainViewModel.Instance.SaveScene();
    }

    private void SaveMenuItemClick(object sender, RoutedEventArgs e)
    {
        SaveScene();
    }

    private void ExitMenuItemClick(object sender, RoutedEventArgs e)
    {
        // check for unsaved changes and ask before close
        Close();
    }

    private void OpenAddScenesToBuildWindow_Click(object sender, RoutedEventArgs e)
    {
        MainViewModel.Instance.OpenAddScenesToBuildWindow();
    }

    private void ReloadSceneMenuItemClick(object sender, RoutedEventArgs e)
    {
        MainViewModel.Instance.ReloadScene();
    }
}
