using NEngine.GameObjects;
using NEngineEditor.ViewModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            if (SceneEditViewUserControl.LazyInstance is not null && LeftListView.SelectedItem is MainViewModel.LayeredGameObject layeredGameObject && layeredGameObject.GameObject is Positionable positionable)
            {
                SceneEditViewUserControl.LazyInstance.MoveCameraToPositionable(positionable);
            }
        }
    }

    private void Action1_Click(object sender, RoutedEventArgs e)
    {
        if (LeftListView.SelectedItem != null)
        {
            MessageBox.Show($"Action 1 on: {LeftListView.SelectedItem}");
        }
    }

    private void Action2_Click(object sender, RoutedEventArgs e)
    {
        if (LeftListView.SelectedItem != null)
        {
            MessageBox.Show($"Action 2 on: {LeftListView.SelectedItem}");
        }
    }
}
