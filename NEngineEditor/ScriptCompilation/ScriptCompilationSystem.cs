﻿using System.IO;
using System.Xml.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace NEngineEditor.ScriptCompilation;
public class ScriptCompilationSystem
{
    private readonly MSBuildWorkspace _workspace;
    private readonly VSCompatibleFileWatcher _fileWatcher;
    private readonly HotReloadableAssemblyManager _hotReloadableAssemblyManager;
    private readonly string _projectFilePath;

    private Project _project;

    public event EventHandler<FileSystemEventArgs>? FileChanged;
    public event EventHandler? AssemblyInitialized;
    public event EventHandler<string>? CompilationSucceeded;
    public event EventHandler<string>? CompilationFailed;

    public ScriptCompilationSystem(string projectFilePath)
    {
        _projectFilePath = projectFilePath;
        _workspace = MSBuildWorkspace.Create();
        _project = _workspace.OpenProjectAsync(projectFilePath).GetAwaiter().GetResult();
        _fileWatcher = new VSCompatibleFileWatcher(Path.GetDirectoryName(projectFilePath)!);
        _fileWatcher.FileChanged += OnFileChanged;
        _hotReloadableAssemblyManager = new HotReloadableAssemblyManager(Path.GetDirectoryName(projectFilePath)!, GetTargetFrameworkFromProject());
    }
    public T? CreateInstance<T>(string fullyQualifiedTypeName) where T : class
    {
        return _hotReloadableAssemblyManager.CreateInstance<T>(fullyQualifiedTypeName);
    }

    public async void InitializeAssembliesAsync()
    {
        await _hotReloadableAssemblyManager.InitializeAsync(this);
        AssemblyInitialized?.Invoke(null, EventArgs.Empty);
    }

    public Project UpdateScript(string scriptPath)
    {
        string scriptContent = File.ReadAllText(scriptPath);
        string scriptName = Path.GetFileName(scriptPath);
        Document? document = _project.Documents.FirstOrDefault(d => d.Name == scriptName) ?? throw new ArgumentException($"Document '{scriptPath}' not found in the project.");
        SourceText newText = SourceText.From(scriptContent);
        Document newDocument = document.WithText(newText);
        Project newProject = newDocument.Project;
        Solution newSolution = newProject.Solution;

        if (!_project.Solution.Workspace.TryApplyChanges(newSolution))
        {
            Managers.Logger.LogInfo("Project was already up to date.");
        }
        else
        {
            Managers.Logger.LogInfo($"Project updated at script {scriptName}");
        }

        return newProject;
    }

    public Project AddScript(string scriptPath)
    {
        try
        {
            string scriptContent = File.ReadAllText(scriptPath);
            Project updatedProject = AddDocument(scriptPath, scriptContent);

            // If you need to update the workspace with the new project:
            var newSolution = updatedProject.Solution;
            _workspace.TryApplyChanges(newSolution);

            return updatedProject;
        }
        catch (Exception)
        {
            Managers.Logger.LogError($"Unable to add script with path: {scriptPath}");
            throw;
        }
    }

    public Project AddDocument(string name, string content, string? filePath = null)
    {
        DocumentId documentId = DocumentId.CreateNewId(_project.Id);
        Solution solution = _project.Solution.AddDocument(documentId, name, content, filePath: filePath);
        return solution.GetProject(_project.Id)!;
    }

    public void RemoveScript(string scriptPath)
    {
        var document = _project.Documents.FirstOrDefault(d => d.FilePath == scriptPath) ?? throw new FileNotFoundException($"Script not found in project: {scriptPath}");
        _project = _project.RemoveDocument(document.Id);

        // Optionally, you can delete the file from disk
        // File.Delete(scriptPath);

        UpdateProjectFile();
    }

    public async Task<Compilation?> GetCompilationAsync()
    {
        return await _project.GetCompilationAsync();
    }

    public void StartWatching()
    {
        _fileWatcher.StartWatching();
    }

    public void StopWatching()
    {
        _fileWatcher.StopWatching();
    }

    private void UpdateProjectFile()
    {
        Solution solution = _project.Solution;
        if (_workspace.TryApplyChanges(solution))
        {
            Managers.Logger.LogInfo("Project updated successfully.");
        }
        else
        {
            Managers.Logger.LogError("Failed to update project.");
        }
    }

    private async void OnFileChanged(object? sender, FileSystemEventArgs e)
    {
        if (e.ChangeType == WatcherChangeTypes.Deleted)
        {
            RemoveScript(e.FullPath);
        }
        else if (e.ChangeType == WatcherChangeTypes.Created)
        {
            AddScript(e.FullPath);
        }
        else
        {
            UpdateScript(e.FullPath);
        }
        await _hotReloadableAssemblyManager.UpdateAssemblyAsync(this);
        FileChanged?.Invoke(sender, e);
    }

    private string GetTargetFrameworkFromProject()
    {
        XDocument projectFile = XDocument.Load(_projectFilePath);
        XNamespace? ns = projectFile.Root?.GetDefaultNamespace();
        if (ns is not null)
        {
            XElement? targetFrameworkNode = projectFile.Descendants(ns + "TargetFramework").FirstOrDefault();
            if (targetFrameworkNode is not null)
            {
                return targetFrameworkNode.Value;
            }

            // For multi-targeting projects
            XElement? targetFrameworksNode = projectFile.Descendants(ns + "TargetFrameworks").FirstOrDefault();
            if (targetFrameworksNode is not null)
            {
                // Taking the first framework in case of multi-targeting
                return targetFrameworksNode.Value.Split(';').First();
            }
        }

        // Default to a common framework version if not found
        return "net8.0";
    }
}