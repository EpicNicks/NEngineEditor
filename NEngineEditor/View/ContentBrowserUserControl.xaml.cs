using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

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
        DataContext = new ContentBrowserViewModel(Dispatcher);
    }

    private void ContentPanel_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // handle folder click (update current subdir), reload files in current dir for Items
            if (sender is StackPanel stackPanel)
            {
                if (stackPanel.Tag is not string filePath || stackPanel.Children[0] is not Image img || stackPanel.Children[1] is not TextBlock txtBlock)
                {
                    return;
                }
                if (img.Source == ContentBrowserViewModel.FOLDER_ICON)
                {
                    OpenFolder(filePath);
                    // tell ContentBrowserViewModel to set current folder to current path
                }
                else if (img.Source == ContentBrowserViewModel.CS_SCRIPT_ICON)
                {
                    MessageBox.Show($"script {txtBlock.Text} double-clicked. Opening script at {filePath}");
                    // open file in default editor/editor selected in editor settings
                }
            }
            // handle file click
        }
    }

    private void StackPanel_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not StackPanel stackPanel || stackPanel.Tag is not string filePath || stackPanel.Children[0] is not Image img || stackPanel.Children[1] is not TextBlock txtBlock)
        {
            return;
        }
        var contextMenu = new ContextMenu();
        if (img.Source == ContentBrowserViewModel.FOLDER_ICON)
        {
            var openMenuItem = new MenuItem { Header = "Open" };
            openMenuItem.Click += (s, args) => OpenFolder(filePath);
            var deleteMenuItem = new MenuItem { Header = "Delete" };
            deleteMenuItem.Click += (s, args) => DeleteItem(filePath);
            contextMenu.Items.Add(openMenuItem);
            contextMenu.Items.Add(deleteMenuItem);
        }
        else if (img.Source == ContentBrowserViewModel.CS_SCRIPT_ICON)
        {
            var openMenuItem = new MenuItem { Header = "Open" };
            openMenuItem.Click += (s, args) => OpenScript(filePath);
            var renameMenuItem = new MenuItem { Header = "Rename" };
            renameMenuItem.Click += (s, args) => RenameItem(stackPanel);
            var deleteMenuItem = new MenuItem { Header = "Delete" };
            deleteMenuItem.Click += (s, args) => DeleteItem(filePath);
            contextMenu.Items.Add(openMenuItem);
            contextMenu.Items.Add(renameMenuItem);
            contextMenu.Items.Add(deleteMenuItem);
        }
        stackPanel.ContextMenu = contextMenu;
        contextMenu.IsOpen = true;
    }

    private void OuterStackPanel_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not Grid grid || DataContext is not ContentBrowserViewModel cbvm)
        {
            return;
        }
        var contextMenu = new ContextMenu();

        var createCsScriptMenuItem = new MenuItem { Header = "Create C# Script" };
        createCsScriptMenuItem.Click += (s, args) => CreateItem(ContentBrowserViewModel.CreateItemType.CS_SCRIPT);
        var createFolderMenuItem = new MenuItem { Header = "Create Folder" };
        createFolderMenuItem.Click += (s, args) => CreateItem(ContentBrowserViewModel.CreateItemType.FOLDER);
        contextMenu.Items.Add(createCsScriptMenuItem);
        contextMenu.Items.Add(createFolderMenuItem);

        grid.ContextMenu = contextMenu;
        contextMenu.IsOpen = true;
    }

    private void OpenFolder(string filePath)
    {
        if (DataContext is ContentBrowserViewModel cbvm)
        {
            cbvm.subDirectory.CurrentSubDir = filePath;
        }
    }

    private void DeleteItem(string filePath)
    {
        if (DataContext is ContentBrowserViewModel cbvm)
        {
            cbvm.DeleteItem(filePath);
        }
    }
    
    private void RenameItem(StackPanel stackPanel)
    {
        // put the item into "rename mode"
        if (stackPanel.Tag is not string filePath || stackPanel.Children[1] is not TextBlock textBlock)
        {
            return;
        }
        void TryRename(string newName)
        {
            if (DataContext is ContentBrowserViewModel cbvm)
            {
                cbvm.RenameItem(filePath, newName);
            }
            stackPanel.Children[1] = textBlock;
        }
        TextBox renameBox = new()
        {
            Text = textBlock.Text,
            Focusable = true,
        };
        renameBox.KeyDown += (_, _) => TryRename(renameBox.Text);
        renameBox.LostFocus += (_, _) => TryRename(renameBox.Text);
        stackPanel.Children[1] = renameBox;
        renameBox.Focus();
    }

    private void CreateItem(ContentBrowserViewModel.CreateItemType createItemType)
    {
        // dialog creation
    }

    private void OpenScript(string filePath)
    {
        using Process? p = Process.Start(filePath);
    }
}
