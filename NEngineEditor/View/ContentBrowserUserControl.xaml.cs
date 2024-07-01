using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

using NEngineEditor.Managers;
using NEngineEditor.ViewModel;
using NEngineEditor.Windows;

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
                }
                else if (img.Source == ContentBrowserViewModel.UP_ONE_LEVEL_ICON)
                {
                    OpenFolder(filePath);
                }
                else if (img.Source == ContentBrowserViewModel.CS_SCRIPT_ICON)
                {
                    OpenScript(filePath);
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
            var addToSceneMenuItem = new MenuItem { Header = "Add To Scene" };
            addToSceneMenuItem.Click += (s, args) =>
            {
                MessageBox.Show("Not yet implemented");
                //MainViewModel.Instance.SceneGameObjects.Add();
            };
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
        // prevents bubbling to the outer grid
        e.Handled = true;
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
        var openCurrentDirInFileExplorer = new MenuItem { Header = "Open Current Directory in File Explorer" };
        openCurrentDirInFileExplorer.Click += (s, args) =>
        {
            string currentDirectory = cbvm.subDirectory.CurrentSubDir;
            Process.Start(new ProcessStartInfo
            {
                FileName = currentDirectory,
                UseShellExecute = true,
                Verb = "open"
            });
        };

        contextMenu.Items.Add(createCsScriptMenuItem);
        contextMenu.Items.Add(createFolderMenuItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(openCurrentDirInFileExplorer);

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
            // are you sure dialog goes here if I decide to add one
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
            stackPanel.Children.RemoveAt(1);
            stackPanel.Children.Insert(1, textBlock);
        }
        TextBox renameBox = new()
        {
            Text = textBlock.Text,
            Focusable = true,
        };
        renameBox.KeyDown += (_, e) =>
        {
            if (e.Key == System.Windows.Input.Key.Enter || e.Key == System.Windows.Input.Key.Escape)
            {
                TryRename(renameBox.Text);
            }
        };
        renameBox.LostFocus += (_, _) => TryRename(renameBox.Text);
        stackPanel.Children.RemoveAt(1);
        stackPanel.Children.Insert(1, renameBox);
        renameBox.Focus();
    }

    private void CreateItem(ContentBrowserViewModel.CreateItemType createItemType)
    {
        // dialog creation
        NewItemDialog newItemDialog = new NewItemDialog(createItemType);
        if (newItemDialog.ShowDialog() == true)
        {
            if (string.IsNullOrEmpty(newItemDialog.EnteredName))
            {
                MessageBox.Show("The Entered Name was empty", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Logger.LogError($"The name you entered for the {createItemType} you tried to create was empty somehow.");
                return;
            }
            if (DataContext is not ContentBrowserViewModel cbvm)
            {
                return;
            }
            cbvm.CreateItem(cbvm.subDirectory.CurrentSubDir, createItemType, newItemDialog.EnteredName);
        }
    }

    private void OpenScript(string filePath)
    {
        try
        {
            // assume visual studio for now
            string csProjFilePath = Directory.GetFiles(MainViewModel.Instance.ProjectDirectory).Where(path => Path.GetExtension(path) == ".csproj").First();
            FileDialogHelper.ShowOpenWithDialog(csProjFilePath);
            //Process.Start(new ProcessStartInfo
            //{
            //    FileName = "devenv.exe",
            //    Arguments = $"{csProjFilePath} /Edit {filePath}",
            //    UseShellExecute = true
            //});
        }
        catch (Exception ex)
        {
            Logger.LogError($"An error occurred: {ex.Message}");
        }
    }
}
