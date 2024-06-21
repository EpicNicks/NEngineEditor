using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NEngineEditor.View;
/// <summary>
/// Interaction logic for SceneEditorUserControl.xaml
/// </summary>
public partial class SceneEditorUserControl : UserControl
{
    public ObservableCollection<string> Items { get; set; }

    public SceneEditorUserControl()
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
