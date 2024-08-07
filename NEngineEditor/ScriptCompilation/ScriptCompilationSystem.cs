using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;

using NEngineEditor.Managers;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace NEngineEditor.ScriptCompilation;
public class ScriptCompilationSystem
{
    private readonly AdhocWorkspace _workspace;
    private readonly VSCompatibleFileWatcher _fileWatcher;
    private readonly ConcurrentDictionary<string, DocumentId> _documentIds;
    private readonly ConcurrentDictionary<string, MetadataReference> _metadataReferences;
    private readonly HotReloadableAssemblyManager _hotReloadableAssemblyManager;
    private readonly string _targetFramework;
    private readonly string _projectFilePath;
    private readonly string _projectDirectory;

    private Project _project;

    public event EventHandler<FileSystemEventArgs>? FileChanged;
    public event EventHandler<string>? CompilationSucceeded;
    public event EventHandler<string>? CompilationFailed;

    public ScriptCompilationSystem(string projectFilePath)
    {
        _projectFilePath = projectFilePath;
        _projectDirectory = Path.GetDirectoryName(projectFilePath)!;
        _workspace = new AdhocWorkspace();
        _documentIds = new ConcurrentDictionary<string, DocumentId>();
        _metadataReferences = new ConcurrentDictionary<string, MetadataReference>();
        _targetFramework = GetTargetFrameworkFromProject();
        _fileWatcher = new VSCompatibleFileWatcher(Path.GetDirectoryName(projectFilePath)!);
        _fileWatcher.FileChanged += OnFileChanged;
        _hotReloadableAssemblyManager = new HotReloadableAssemblyManager(Path.GetDirectoryName(projectFilePath)!, _targetFramework);

        InitializeProject();
    }
    public T? CreateInstance<T>(string fullyQualifiedTypeName) where T : class
    {
        return _hotReloadableAssemblyManager.CreateInstance<T>(fullyQualifiedTypeName);
    }


    public void UpdateScript(string scriptPath, string scriptContent)
    {
        if (!_documentIds.TryGetValue(scriptPath, out var documentId))
        {
            documentId = DocumentId.CreateNewId(_project.Id);
            _documentIds[scriptPath] = documentId;
            var sourceText = SourceText.From(scriptContent);
            _project = _project.AddDocument(
                Path.GetFileName(scriptPath),  // name
                sourceText,                    // text
                filePath: scriptPath           // filePath
            ).Project;
        }
        else
        {
            var document = _project.GetDocument(documentId);
            var sourceText = SourceText.From(scriptContent);
            var newDocument = document.WithText(sourceText);
            _project = newDocument.Project;
        }
    }

    public void RemoveScript(string scriptPath)
    {
        if (_documentIds.TryRemove(scriptPath, out var documentId))
        {
            _project = _project.RemoveDocument(documentId);
        }
    }

    public async Task<Assembly?> CompileAsync()
    {
        byte[]? compiledResult = await CompileToByteArrayAsync();
        if (compiledResult is null)
        {
            Logger.LogError("An assembly could not be emitted");
            return null;
        }
        else
        {
            return Assembly.Load(compiledResult);
        }
    }

    public async Task<byte[]?> CompileToByteArrayAsync()
    {
        var compilation = await _project.GetCompilationAsync();
        using var ms = new MemoryStream();

        EmitResult? result = compilation?.Emit(ms);

        if (result is null)
        {
            Logger.LogError("Result of compilation emission in CompileAsync was null");
            return null;
        }
        if (!result.Success)
        {
            var failures = result.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);

            foreach (var diagnostic in failures)
            {
                Logger.LogError($"{diagnostic.Id}: {diagnostic.GetMessage()}");
            }
            CompilationFailed?.Invoke(this, "Compilation failed. See error output for details.");
            return null;
        }

        ms.Seek(0, SeekOrigin.Begin);
        CompilationSucceeded?.Invoke(this, "Compilation succeeded.");
        return ms.ToArray();
    }

    public void StartWatching()
    {
        _fileWatcher.StartWatching();
    }

    public void StopWatching()
    {
        _fileWatcher.StopWatching();
    }

    public void RemoveReference(string assemblyName)
    {
        if (_metadataReferences.TryRemove(assemblyName, out var reference))
        {
            _project = _project.RemoveMetadataReference(reference);
            Logger.LogInfo($"Removed reference: {assemblyName}");
        }
    }

    public bool HasReference(string assemblyName)
    {
        return _metadataReferences.ContainsKey(assemblyName);
    }

    public IEnumerable<string> GetLoadedReferences()
    {
        return _metadataReferences.Keys;
    }

    private void AddProjectReference(string projectPath)
    {
        try
        {
            if (!File.Exists(projectPath))
            {
                Logger.LogWarning($"Warning: Project file not found: {projectPath}");
                return;
            }

            var projectId = ProjectId.CreateNewId();
            var projectInfo = ProjectInfo.Create(
                projectId,
                VersionStamp.Create(),
                Path.GetFileNameWithoutExtension(projectPath),
                Path.GetFileNameWithoutExtension(projectPath),
                LanguageNames.CSharp,
                filePath: projectPath
            );

            var newProject = _workspace.AddProject(projectInfo);
            var projectReference = new ProjectReference(projectId);
            _project = _project.AddProjectReference(projectReference);

            Logger.LogInfo($"Added project reference: {projectPath}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to add project reference {projectPath}: {ex.Message}");
        }
    }

    private async void OnFileChanged(object? sender, FileSystemEventArgs e)
    {
        if (e.ChangeType == WatcherChangeTypes.Deleted)
        {
            RemoveScript(e.FullPath);
        }
        else
        {
            string content = File.ReadAllText(e.FullPath);
            UpdateScript(e.FullPath, content);
        }
        await _hotReloadableAssemblyManager.ReloadAssemblyAsync(this);
        FileChanged?.Invoke(sender, e);
    }

    [MemberNotNull(nameof(_project))]
    private void InitializeProject()
    {
        var projectName = Path.GetFileNameWithoutExtension(_projectFilePath);
        _project = _workspace.AddProject(projectName, LanguageNames.CSharp);
        LoadProjectReferences();
        AddDefaultReferences();
    }

    private void LoadProjectReferences()
    {
        XDocument projectFile = XDocument.Load(_projectFilePath);
        XNamespace ns = projectFile.Root.GetDefaultNamespace();

        // Load assembly references
        var assemblyReferences = projectFile.Descendants(ns + "Reference")
            .Select(r => r.Attribute("Include")?.Value)
            .Where(r => !string.IsNullOrEmpty(r));

        foreach (var reference in assemblyReferences)
        {
            AddAssemblyReference(reference);
        }

        // Load project references
        var projectReferences = projectFile.Descendants(ns + "ProjectReference")
            .Select(r => r.Attribute("Include")?.Value)
            .Where(r => !string.IsNullOrEmpty(r));

        foreach (var reference in projectReferences)
        {
            AddProjectReference(Path.GetFullPath(Path.Combine(_projectDirectory, reference)));
        }
    }

    private void AddAssemblyReference(string assemblyName)
    {
        try
        {
            string? assemblyPath = ResolveAssemblyPath(assemblyName);
            if (assemblyPath is not null)
            {
                var reference = MetadataReference.CreateFromFile(assemblyPath);
                _metadataReferences[assemblyName] = reference;
                _project = _project.AddMetadataReference(reference);
                Logger.LogInfo($"Added assembly reference: {assemblyName}");
            }
            else
            {
                Logger.LogWarning($"Warning: Could not resolve assembly path for {assemblyName}");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Failed to add assembly reference {assemblyName}: {ex.Message}");
        }
    }

    private string GetTargetFrameworkFromProject()
    {
        XDocument projectFile = XDocument.Load(_projectFilePath);
        XNamespace ns = projectFile.Root.GetDefaultNamespace();

        var targetFrameworkNode = projectFile.Descendants(ns + "TargetFramework").FirstOrDefault();
        if (targetFrameworkNode != null)
        {
            return targetFrameworkNode.Value;
        }

        // For multi-targeting projects
        var targetFrameworksNode = projectFile.Descendants(ns + "TargetFrameworks").FirstOrDefault();
        if (targetFrameworksNode != null)
        {
            // Taking the first framework in case of multi-targeting
            return targetFrameworksNode.Value.Split(';').First();
        }

        // Default to a common framework version if not found
        return "net8.0";
    }

    private string? ResolveAssemblyPath(string assemblyName)
    {
        // Remove .dll extension if present
        assemblyName = Path.GetFileNameWithoutExtension(assemblyName);

        string[] searchPaths =
        [
            Path.Combine(_projectDirectory, ".Engine"),
            Path.Combine(_projectDirectory, "bin", "Debug", _targetFramework),
            Path.Combine(_projectDirectory, "bin", "Release", _targetFramework),
            Path.GetDirectoryName(typeof(object).Assembly.Location)!  // Framework directory
        ];

        foreach (var searchPath in searchPaths)
        {
            string[] possibleExtensions = [".dll", ".exe", ""];
            foreach (var ext in possibleExtensions)
            {
                string fullPath = Path.Combine(searchPath, assemblyName + ext);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        // If not found in predefined paths, try to load from GAC or current directory
        try
        {
            var assembly = Assembly.Load(new AssemblyName(assemblyName));
            return assembly.Location;
        }
        catch
        {
            // Assembly not found
            return null;
        }
    }

    private void AddDefaultReferences()
    {
        var defaultAssemblies = new[]
        {
            typeof(object).Assembly.Location,
            typeof(Console).Assembly.Location,
            typeof(System.Linq.Enumerable).Assembly.Location,
            typeof(System.Xml.XmlDocument).Assembly.Location
        };

        foreach (var assembly in defaultAssemblies)
        {
            AddAssemblyReference(Path.GetFileNameWithoutExtension(assembly));
        }
    }
}