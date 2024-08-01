using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using NEngine.GameObjects;

using NEngineEditor.Extensions;
using NEngineEditor.Managers;
using NEngineEditor.ViewModel;

namespace NEngineEditor.View;
/// <summary>
/// Interaction logic for SceneEditorUserControl.xaml
/// </summary>
public partial class SceneHierarchyUserControl : UserControl
{
    public SceneHierarchyUserControl()
    {
        InitializeComponent();
        SceneHierarchyViewModel shvm = new();
        DataContext = shvm;
        LeftListView.ItemsSource = shvm.SceneGameObjects;
        shvm.PropertyChanged += (_, propChangedEventArgs) =>
        {
            if (propChangedEventArgs.PropertyName == nameof(shvm.SelectedGameObject))
            {
                MainViewModel.LayeredGameObject? foundObject = shvm.SceneGameObjects.Where(sgo => sgo.GameObject.Name == shvm.SelectedGameObject?.GameObject.Name).FirstOrDefault();
                LeftListView.SelectedIndex = shvm.SceneGameObjects.TryGetIndexOf(foundObject);
            }
        };
    }

    private void LeftListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is not SceneHierarchyViewModel shvm)
        {
            return;
        }
        shvm.SelectedGameObject = LeftListView.SelectedItem as MainViewModel.LayeredGameObject;
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
        if (LeftListView.SelectedItem is MainViewModel.LayeredGameObject layeredGameObject && DataContext is SceneHierarchyViewModel shvm)
        {
            shvm.DeleteInstanceCommand.Execute(layeredGameObject);
        }
        else
        {
            Logger.LogError("Selected GameObject instance to delete was not a LayeredGameObject somehow. Your scene may be corrupted.");
        }
    }
}
