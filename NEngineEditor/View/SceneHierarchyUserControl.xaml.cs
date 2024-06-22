using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NEngineEditor.View;
/// <summary>
/// Interaction logic for SceneEditorUserControl.xaml
/// </summary>
public partial class SceneHierarchyUserControl : UserControl
{
    public ObservableCollection<string> Items { get; set; }

    public SceneHierarchyUserControl()
    {
        InitializeComponent();
        Items = LoadItems();
        LeftListView.ItemsSource = Items;
    }

    private ObservableCollection<string> LoadItems()
    {
        return [
                "Item 1",
                "Item 2",
                "Item 3",
                "Item 4",
                "Item 5"
            ];
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
