using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using NEngineEditor.ViewModel;

namespace NEngineEditor.View;
/// <summary>
/// Interaction logic for ContentBrowserUserControl.xaml
/// </summary>
public partial class ContentBrowserUserControl : UserControl
{
    public ContentBrowserUserControl()
    {
        InitializeComponent();
        DataContext = new ContentBrowserViewModel();
    }

    private void ContentPanel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // handle folder click (update current subdir), reload files in current dir for Items
            if (sender is StackPanel stackPanel)
            {
                if (stackPanel.Children[0] is not Image img || stackPanel.Children[1] is not TextBlock txtBlock)
                {
                    return;
                }
                if (img.Source == ContentBrowserViewModel.FOLDER_ICON)
                {
                    MessageBox.Show($"folder {txtBlock.Text} double-clicked");
                }
                else if (img.Source == ContentBrowserViewModel.CS_SCRIPT_ICON)
                {
                    MessageBox.Show($"script {txtBlock.Text} double-clicked");
                }
            }
            // handle file click
        }
    }
}
