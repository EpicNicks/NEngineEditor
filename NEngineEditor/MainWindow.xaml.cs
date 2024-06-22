using System;
using System.Reflection;

using System.Windows.Forms;

using SFML.Graphics;
using SFML.Window;
using SFML.System;

namespace NEngineEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : System.Windows.Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void SaveMenuItemClick(object sender, System.Windows.RoutedEventArgs e)
    {
        MessageBox.Show("Save Clicked");
    }

    private void EditMenuItemClick(object sender, System.Windows.RoutedEventArgs e)
    {
        // check for unsaved changes and ask before close
        Close();
    }
}
