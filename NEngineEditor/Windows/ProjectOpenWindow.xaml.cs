using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

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
        using var dialog = new FolderBrowserDialog();
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
        //System.Windows.MessageBox.Show("Create new project functionality to be implemented.");
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
                    System.Windows.MessageBox.Show("The Project Name was null somehow in ProjectOpenWindow::SetupNewProject", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    string destinationFolder = engineFolderPath;

                    foreach (AssemblyName assemblyName in referencedAssemblies)
                    {
                        // SFML.NET reference is added in the generated csproj file
                        if (assemblyName.Name == "NEngine")
                        {
                            Assembly referencedAssembly = Assembly.Load(assemblyName);
                            string sourcePath = referencedAssembly.Location;
                            string destinationPath = Path.Combine(destinationFolder, Path.GetFileName(sourcePath));
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
                    System.Windows.MessageBox.Show("A project with that name already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    private void ProjectListBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (ProjectListBox.SelectedItem is ListBoxItem selectedItem)
        {
            var projectPath = selectedItem.Tag as string;
            if (projectPath is not null)
            {
                OpenProjectWindow(projectPath);
            }
            else
            {
                System.Windows.MessageBox.Show("The Project Path was null somehow", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    private void OpenProjectWindow(string projectPath)
    {
        var projectWindow = new MainWindow(projectPath);
        projectWindow.Show();
    }
}
