using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Loader;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace NEngineEditor.ScriptCompilation;
public class HotReloadableAssemblyManager
{
    private AssemblyLoadContext _loadContext;
    private Assembly? _currentAssembly;
    private readonly Dictionary<string, WeakReference> _instanceTracker = new();
    private readonly string _projectDirectory;
    private readonly string _targetFramework;

    public event EventHandler<EventArgs>? AssemblyUpdated;

    public HotReloadableAssemblyManager(string projectDirectory, string targetFramework)
    {
        _projectDirectory = projectDirectory;
        _targetFramework = targetFramework;
        _loadContext = new AssemblyLoadContext("UpdateableScriptContext", isCollectible: true);
        _loadContext.Resolving += OnResolving;
    }

    private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        string[] searchPaths =
        [
            Path.Combine(_projectDirectory, ".Engine"),
                Path.Combine(_projectDirectory, "bin", "Debug", _targetFramework),
                Path.Combine(_projectDirectory, "bin", "Release", _targetFramework),
                Path.GetDirectoryName(typeof(object).Assembly.Location)!, // Framework directory
                AppDomain.CurrentDomain.BaseDirectory // Application base directory
        ];

        foreach (var searchPath in searchPaths)
        {
            string assemblyPath = Path.Combine(searchPath, $"{assemblyName.Name}.dll");
            if (File.Exists(assemblyPath))
            {
                return context.LoadFromAssemblyPath(assemblyPath);
            }
        }

        return null;
    }

    public async Task InitializeAsync(ScriptCompilationSystem compilationSystem)
    {
        await UpdateAssemblyAsync(compilationSystem);
    }

    public async Task UpdateAssemblyAsync(ScriptCompilationSystem compilationSystem)
    {
        var compilation = await compilationSystem.GetCompilationAsync();
        if (compilation is null)
        {
            return;
        }
        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();

        var emitResult = compilation.Emit(peStream, pdbStream);

        if (!emitResult.Success)
        {
            throw new InvalidOperationException("Compilation failed: " + string.Join(", ", emitResult.Diagnostics));
        }

        peStream.Seek(0, SeekOrigin.Begin);
        pdbStream.Seek(0, SeekOrigin.Begin);

        var oldLoadContext = _loadContext;
        _loadContext = new AssemblyLoadContext("UpdateableScriptContext", isCollectible: true);
        _loadContext.Resolving += OnResolving;

        _currentAssembly = _loadContext.LoadFromStream(peStream, pdbStream);

        oldLoadContext.Unload();

        AssemblyUpdated?.Invoke(this, EventArgs.Empty);

        // Clear the instance tracker as all old instances are now invalid
        _instanceTracker.Clear();

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    public T? CreateInstance<T>(string fullyQualifiedTypeName) where T : class
    {
        if (_currentAssembly == null)
        {
            throw new InvalidOperationException("No assembly has been loaded yet.");
        }

        Type? type = _currentAssembly.GetType(fullyQualifiedTypeName)
            ?? throw new ArgumentException($"Type {fullyQualifiedTypeName} not found in the current assembly.");
        var instance = Activator.CreateInstance(type) as T;
        _instanceTracker[fullyQualifiedTypeName] = new WeakReference(instance);
        return instance;
    }

    public IEnumerable<string> GetAvailableTypeNames()
    {
        if (_currentAssembly is null)
        {
            return Array.Empty<string>();
        }

        return _currentAssembly.GetTypes().Select(t => t.FullName!).Where(name => name != null);
    }

    public void InvalidateInstances()
    {
        foreach (var kvp in _instanceTracker.ToList())
        {
            if (!kvp.Value.IsAlive)
            {
                _instanceTracker.Remove(kvp.Key);
            }
        }
    }
}
