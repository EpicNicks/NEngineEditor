using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using NEngineEditor.ViewModel;

using NEngine.GameObjects;
using NEngineEditor.Managers;

namespace NEngineEditor.View;
/// <summary>
/// Interaction logic for SceneEditorUserControl.xaml
/// </summary>
public partial class SceneHierarchyUserControl : UserControl
{
    public SceneHierarchyUserControl()
    {
        InitializeComponent();
        LeftListView.ItemsSource = MainViewModel.Instance.SceneGameObjects;
    }

    private void LeftListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // as cast returns null on failure (intended behavior)
        MainViewModel.Instance.SelectedGameObject = LeftListView.SelectedItem as MainViewModel.LayeredGameObject;
    }

    private void LeftListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LeftListView.SelectedItem != null)
        {
            if (SceneEditViewUserControl.LazyInstance is not null && LeftListView.SelectedItem is MainViewModel.LayeredGameObject { GameObject: Positionable positionable })
            {
                SceneEditViewUserControl.LazyInstance.MoveCameraToPositionable(positionable);
            }
        }
    }

    private void DeleteInstance_Click(object sender, RoutedEventArgs e)
    {
        if (LeftListView.SelectedItem is MainViewModel.LayeredGameObject layeredGameObject)
        {
            MainViewModel.Instance.SceneGameObjects.Remove(layeredGameObject);
        }
        else
        {
            Logger.LogError("Selected GameObject instance to delete was not a LayeredGameObject somehow. Your scene may be corrupted.");
        }
    }
}
