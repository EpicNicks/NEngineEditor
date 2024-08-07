using System.Collections.Specialized;
using System.IO;
using System.Windows;

using NEngineEditor.Managers;
using NEngineEditor.ViewModel;

namespace NEngineEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public static string ProjectDirectory { get; private set; } = "";
    public static string AssetsPath => Path.Combine(ProjectDirectory, "Assets");
    public static string EngineFolderPath => Path.Combine(ProjectDirectory, ".Engine");
    public static string MainFolderPath => Path.Combine(ProjectDirectory, ".Main");

    public MainWindow(string projectPath)
    {
        ProjectDirectory = projectPath;
        MainViewModel.ClearInstance();
        InitializeComponent();
        MainViewModel.Instance.ContentBrowserViewModel = ContentBrowserControl.DataContext as ContentBrowserViewModel;
        Logger.Instance.Logs.CollectionChanged += LoggerLogs_CollectionChanged;
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
