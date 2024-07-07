using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;

using NEngineEditor.Model.JsonSerialized;

namespace NEngineEditor.Windows;
/// <summary>
/// Interaction logic for ProjectOpenWindow.xaml
/// </summary>
public partial class ProjectOpenWindow : Window
{
    private static readonly JsonSerializerOptions NEW_PROJECT_JSON_OPTIONS = new() { WriteIndented = true };

    public ProjectOpenWindow()
    {
        InitializeComponent();
        DataContext = this;
        BaseFilePathTextBox.TextChanged += BaseFilePathTextBox_TextChanged;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        using System.Windows.Forms.FolderBrowserDialog dialog = new();
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            BaseFilePathTextBox.Text = dialog.SelectedPath;
        }
    }

    private void BaseFilePathTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        Refresh();
    }

    private void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        Refresh();
    }

    private void CreateNewProject_Click(object sender, RoutedEventArgs e)
    {
        SetupNewProject();
    }

    private void Refresh()
    {
        var basePath = BaseFilePathTextBox.Text;
        ProjectListBox.Items.Clear();

        if (Directory.Exists(basePath))
        {
            var projectDirs = Directory.GetDirectories(basePath)
                .Where(dir => File.Exists(Path.Combine(dir, "NEngineProject.json")));

            foreach (var dir in projectDirs)
            {
                ProjectListBox.Items.Add(new ListBoxItem { Content = Path.GetFileName(dir), Tag = dir });
            }
        }
    }

    private void SetupNewProject()
    {
        var basePath = BaseFilePathTextBox.Text;
        // revalidate
        if (Directory.Exists(basePath))
        {
            NewProjectDialog newProjectDialog = new NewProjectDialog();
            if (newProjectDialog.ShowDialog() == true)
            {
                if (newProjectDialog.ProjectName is null)
                {
                    MessageBox.Show("The Project Name was null somehow in ProjectOpenWindow::SetupNewProject", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                string projectName = newProjectDialog.ProjectName;
                string sanitizedProjectName = projectName.Replace(" ", "_");
                // Create the new project directory and NEngineProject.json file
                string projectPath = Path.Combine(BaseFilePathTextBox.Text, projectName);
                string assetsPath = Path.Combine(projectPath, "Assets");
                string engineFolderPath = Path.Combine(projectPath, ".Engine");
                string mainFolderPath = Path.Combine(projectPath, ".Main");
                if (!Directory.Exists(projectPath))
                {
                    Directory.CreateDirectory(projectPath);
                    Directory.CreateDirectory(assetsPath);
                    DirectoryInfo engineDirectoryInfo = Directory.CreateDirectory(engineFolderPath);
                    engineDirectoryInfo.Attributes |= FileAttributes.Hidden;
                    DirectoryInfo mainDirectoryInfo = Directory.CreateDirectory(mainFolderPath);
                    mainDirectoryInfo.Attributes |= FileAttributes.Hidden;

                    NEngineProject projectData = new()
                    {
                        ProjectName = projectName,
                        EngineVersion = "0.0.1"
                        // placeholder since we aren't doing versioning yet anyway, not even sure how to version between the engine core and editor right now anyway
                        //  engine version selection would be selected or populated once it matters and the editor has a reference to it
                    };

                    ProjectConfig projectConfig = new()
                    {
                        DefaultBackgroundColor = "#000",
                        Scenes = [],
                    };

                    // compile the engine and copy it to the new project

                    // Get the executing assembly
                    Assembly executingAssembly = Assembly.GetExecutingAssembly();
                    AssemblyName[] referencedAssemblies = executingAssembly.GetReferencedAssemblies();

                    foreach (AssemblyName assemblyName in referencedAssemblies)
                    {
                        // SFML.NET reference is added in the generated csproj file
                        if (assemblyName.Name == "NEngine")
                        {
                            Assembly referencedAssembly = Assembly.Load(assemblyName);
                            string sourcePath = referencedAssembly.Location;
                            string destinationPath = Path.Combine(engineFolderPath, Path.GetFileName(sourcePath));
                            if (File.Exists(sourcePath))
                            {
                                File.Copy(sourcePath, destinationPath, true);
                                Console.WriteLine($"Copied {sourcePath} to {destinationPath}");
                            }
                        }
                    }

                    Task[] fileTasks =
                    [
                        File.WriteAllTextAsync(Path.Combine(projectPath, $"{sanitizedProjectName}.csproj"), Properties.Resources.CsProjTemplate_csproj),
                        File.WriteAllTextAsync(Path.Combine(projectPath, "NEngineProject.json"), JsonSerializer.Serialize(projectData, NEW_PROJECT_JSON_OPTIONS)),
                        File.WriteAllTextAsync(Path.Combine(assetsPath, "ProjectConfig.json"), JsonSerializer.Serialize(projectConfig, NEW_PROJECT_JSON_OPTIONS)),
                        File.WriteAllTextAsync(Path.Combine(mainFolderPath, "Project.cs"), Properties.Resources.ProjectProgram),
                        // TODO: consider writing the shared json data definitions to the mainFolderPath to avoid defining it twice
                        //  drawback to this: the namespace the file will be pulling from won't exist while editing it in this project
                    ];
                    Task.WaitAll(fileTasks);
                    // Add the new project to the list
                    ProjectListBox.Items.Add(new ListBoxItem { Content = projectName, Tag = projectPath });
                }
                else
                {
                    MessageBox.Show("A project with that name already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
    private static void OpenProjectWindow(string projectPath)
    {
        MainWindow projectWindow = new(projectPath);
        projectWindow.Show();
    }

    private void ProjectListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ProjectListBox.SelectedItem is ListBoxItem selectedItem)
        {
            if (selectedItem.Tag is string projectPath)
            {
                OpenProjectWindow(projectPath);
            }
            else
            {
                MessageBox.Show("The Project Path was null somehow", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private static string? ProjectPathFromContextMenuHelper(object? contextMenuItem)
    {
        return contextMenuItem is MenuItem { Parent: ContextMenu { PlacementTarget: ListBox { SelectedItem: ListBoxItem { Tag: string path } } } }
            ? path
            : null;
    }

    private void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        string? projectPath = ProjectPathFromContextMenuHelper(sender);
        if (projectPath is null)
        {
            MessageBox.Show("Something went wrong");
            return;
        }
        OpenProjectWindow(projectPath);
    }

    private void DeleteProject_Click(object sender, RoutedEventArgs e)
    {
        static bool ConfirmDelete(string projectPath)
        {
            MessageBoxResult result = MessageBox.Show(
                $"Are you sure you want to delete the following project?\n\n{projectPath}\n\nThis action cannot be undone.",
                "Confirm Project Deletion",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );
            return result == MessageBoxResult.Yes;
        }

        string? projectPath = ProjectPathFromContextMenuHelper(sender);
        if (projectPath is null)
        {
            MessageBox.Show("Unable to delete project: path not found");
            return;
        }
        if (ConfirmDelete(projectPath))
        {
            try
            {
                Directory.Delete(projectPath, true);
                Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An exception has occurred:\n\n{ex}", "Failure", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void UpdateEngineOnProject_Click(object sender, RoutedEventArgs e)
    {
        string? projectPath = ProjectPathFromContextMenuHelper(sender);
        if (projectPath is null)
        {
            MessageBox.Show("Unable to update project: path not found", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        string engineFolderPath = Path.Combine(projectPath, ".Engine");
        Directory.CreateDirectory(engineFolderPath);

        AssemblyName? assemblyName = Assembly.GetExecutingAssembly().GetReferencedAssemblies().Where(assName => assName.Name == "NEngine").FirstOrDefault();
        if (assemblyName is null)
        {
            MessageBox.Show("NEngine Assembly was not found locally and could not be copied into the project", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        try
        {
            Assembly referencedAssembly = Assembly.Load(assemblyName);
            string sourcePath = referencedAssembly.Location;
            string destinationPath = Path.Combine(engineFolderPath, Path.GetFileName(sourcePath));
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, destinationPath, true);
            }
            MessageBox.Show("Up-to-date NEngine Assemblies Successfully Copied!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error updating NEngine: {ex.Message}", "Update Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
