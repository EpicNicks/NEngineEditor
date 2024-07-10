using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using NEngineEditor.Managers;
using NEngineEditor.Model;
using NEngineEditor.ViewModel;

namespace NEngineEditor.Windows;
/// <summary>
/// Interaction logic for AddScenesToBuildWindow.xaml
/// </summary>
public partial class AddScenesToBuildWindow : Window
{
    private ObservableCollection<PathedSceneData> _pathedSceneModels = [];
    public ObservableCollection<PathedSceneData> PathedSceneModels => _pathedSceneModels;
    private Point startPoint;

    public List<string> SelectedScenePaths { get; private set; } = [];

    public AddScenesToBuildWindow()
    {
        InitializeComponent();
        DataContext = this;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        FindScenesInProject();
    }

    private void FindScenesInProject()
    {
        static List<string> FindSceneFiles(string directory)
        {
            List<string> sceneFiles = [];
            try
            {
                sceneFiles.AddRange(Directory.GetFiles(directory, "*.scene"));
                foreach (string subdirectory in Directory.GetDirectories(directory))
                {
                    sceneFiles.AddRange(FindSceneFiles(subdirectory));
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine($"Access denied: {e.Message}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred: {e.Message}");
            }
            return sceneFiles;
        }

        string startingDirectory = Path.Join(MainViewModel.Instance.ProjectDirectory, "Assets");
        List<string> sceneFilePaths = FindSceneFiles(startingDirectory);
        sceneFilePaths.ForEach(sfp =>
        {
            try
            {
                string sceneModelJson = File.ReadAllText(sfp);
                SceneModel? parsedSceneModel = JsonSerializer.Deserialize<SceneModel>(sceneModelJson);
                if (parsedSceneModel is not null)
                {
                    _pathedSceneModels.Add(new() { Path = sfp, SceneData = parsedSceneModel });
                }
            }
            catch (JsonException ex)
            {
                Logger.LogError($"A JsonException occurred while parsing the json of scene with path\n\n{sfp}\n\n{ex}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"An Exception occurred while parsing the json of scene with path\n\n{sfp}\n\n{ex}");
            }
        });
    }

    public class PathedSceneData
    {
        public required string Path { get; set; }
        public required SceneModel SceneData { get; set; }
        public bool IsSelected { get; set; } = false;
    }

    private void ProcessSelectedScenes()
    {
        SelectedScenePaths = PathedSceneModels.Where(scene => scene.IsSelected).Select(psd => psd.Path).ToList();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void AcceptButton_Click(object sender, RoutedEventArgs e)
    {
        ProcessSelectedScenes();
        DialogResult = true;
        Close();
    }

    private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        startPoint = e.GetPosition(null);
    }

    private void ListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        Point mousePos = e.GetPosition(null);
        Vector diff = startPoint - mousePos;

        if (e.LeftButton == MouseButtonState.Pressed &&
            (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
             Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
        {
            ListBoxItem? listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);
            if (listBoxItem is not null)
            {
                PathedSceneData draggedItem = (PathedSceneData)ScenesListBox.ItemContainerGenerator.ItemFromContainer(listBoxItem);
                DragDrop.DoDragDrop(listBoxItem, draggedItem, DragDropEffects.Move);
            }
        }
    }

    private void ListBox_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(typeof(PathedSceneData)))
        {
            return;
        }

        if (e.Data.GetData(typeof(PathedSceneData)) is not PathedSceneData droppedData)
        {
            return;
        }

        int removedIdx = _pathedSceneModels.IndexOf(droppedData);
        if (removedIdx < 0)
        {
            return;
        }

        if (((FrameworkElement)sender).DataContext is PathedSceneData target)
        {
            int targetIdx = _pathedSceneModels.IndexOf(target);
            if (targetIdx < 0)
            {
                return;
            }

            if (removedIdx != targetIdx)
            {
                _pathedSceneModels.RemoveAt(removedIdx);
                if (removedIdx < targetIdx)
                {
                    _pathedSceneModels.Insert(targetIdx, droppedData);
                }
                else
                {
                    _pathedSceneModels.Insert(targetIdx + 1, droppedData);
                }
            }
        }
        else
        {
            _pathedSceneModels.RemoveAt(removedIdx);
            _pathedSceneModels.Add(droppedData);
        }
    }
    private void ListBox_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(PathedSceneData)))
        {
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private static T? FindAncestor<T>(DependencyObject current) where T : DependencyObject
    {
        do
        {
            if (current is T t)
            {
                return t;
            }
            current = VisualTreeHelper.GetParent(current);
        }
        while (current != null);
        return null;
    }
}
