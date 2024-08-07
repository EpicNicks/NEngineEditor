using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace NEngineEditor.ScriptCompilation;
public class HotReloadableAssemblyManager
{
    private AssemblyLoadContext _loadContext;
    private Assembly? _currentAssembly;
    private readonly Dictionary<string, WeakReference> _instanceTracker = [];
    private readonly string _projectDirectory;
    private readonly string _targetFramework;

    public event EventHandler<EventArgs>? AssemblyReloaded;

    public HotReloadableAssemblyManager(string projectDirectory, string targetFramework)
    {
        _projectDirectory = projectDirectory;
        _targetFramework = targetFramework;
        CreateNewLoadContext();
    }

    [MemberNotNull(nameof(_loadContext))]
    private void CreateNewLoadContext()
    {
        _loadContext = new AssemblyLoadContext("DynamicScriptContext", isCollectible: true);
        _loadContext.Resolving += OnResolving;
    }

    private Assembly? OnResolving(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        // Define search paths for assemblies
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

        // If the assembly is not found in any of the search paths, return null
        // This allows the runtime to continue with its default probing
        return null;
    }

    public async Task ReloadAssemblyAsync(ScriptCompilationSystem compilationSystem)
    {
        byte[]? assemblyBytes = await compilationSystem.CompileToByteArrayAsync() ?? throw new InvalidOperationException("Compilation failed");
        var oldLoadContext = _loadContext;
        CreateNewLoadContext();

        using (MemoryStream ms = new(assemblyBytes))
        {
            _currentAssembly = _loadContext.LoadFromStream(ms);
        }

        // Unload the old context
        oldLoadContext.Unload();

        // Raise the AssemblyReloaded event
        AssemblyReloaded?.Invoke(this, EventArgs.Empty);

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

        Type? type = _currentAssembly.GetType(fullyQualifiedTypeName) ?? throw new ArgumentException($"Type {fullyQualifiedTypeName} not found in the current assembly.");
        var instance = Activator.CreateInstance(type) as T;
        _instanceTracker[fullyQualifiedTypeName] = new WeakReference(instance);
        return instance;
    }

    public IEnumerable<string> GetAvailableTypeNames()
    {
        if (_currentAssembly is null)
        {
            return [];
        }

        return _currentAssembly.GetTypes().Select(t => t.FullName).OfType<string>();
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
