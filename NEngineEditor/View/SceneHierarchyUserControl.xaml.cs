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
        LeftListView.ItemsSource = MainViewModel.Instance.GameObjectWrapperModels;
    }

    private void LeftListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (LeftListView.SelectedItem != null)
        {
            MessageBox.Show($"Item double-clicked: {LeftListView.SelectedItem}");
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
