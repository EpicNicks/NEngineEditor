using System.IO;
using System.Reflection;
using System.Runtime.Loader;

using Microsoft.CodeAnalysis;

namespace NEngineEditor.ScriptCompilation;
public class HotReloadableAssemblyManager
{
    public bool IsAssemblyLoaded => _currentAssembly != null;

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

    public async Task<bool> UpdateAssemblyAsync(ScriptCompilationSystem compilationSystem)
    {
        try
        {
            var compilation = await compilationSystem.GetCompilationAsync();
            if (compilation is null)
            {
                return false;
            }

            using var peStream = new MemoryStream();
            using var pdbStream = new MemoryStream();

            var emitResult = compilation.Emit(peStream, pdbStream);

            if (!emitResult.Success)
            {
                // Log errors but keep the old assembly working
                var errors = emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);
                foreach (var error in errors)
                {
                    Managers.Logger.LogError($"Compilation error in {error.Location.SourceTree?.FilePath}: {error.GetMessage()}");
                }
                return false;
            }

            // Only update if compilation succeeded
            peStream.Seek(0, SeekOrigin.Begin);
            pdbStream.Seek(0, SeekOrigin.Begin);

            var oldLoadContext = _loadContext;
            _loadContext = new AssemblyLoadContext("UpdateableScriptContext", isCollectible: true);
            _loadContext.Resolving += OnResolving;

            _currentAssembly = _loadContext.LoadFromStream(peStream, pdbStream);

            oldLoadContext.Unload();

            AssemblyUpdated?.Invoke(this, EventArgs.Empty);
            _instanceTracker.Clear();

            GC.Collect();
            GC.WaitForPendingFinalizers();

            Managers.Logger.LogInfo("Assembly updated successfully");
            return true;
        }
        catch (Exception ex)
        {
            Managers.Logger.LogError($"Failed to update assembly: {ex.Message}", ex.ToString());
            return false; // Old assembly remains loaded
        }
    }

    public T? CreateInstance<T>(string fullyQualifiedTypeName) where T : class
    {
        try
        {
            if (_currentAssembly == null)
            {
                Managers.Logger.LogError("No assembly has been loaded yet.");
                return null;
            }

            Type? type = _currentAssembly.GetType(fullyQualifiedTypeName);
            if (type == null)
            {
                Managers.Logger.LogError($"Type {fullyQualifiedTypeName} not found in the current assembly.");
                return null;
            }

            var instance = Activator.CreateInstance(type) as T;

            if (instance != null)
            {
                _instanceTracker[fullyQualifiedTypeName] = new WeakReference(instance);
            }

            return instance;
        }
        catch (TargetInvocationException ex)
        {
            // Constructor threw an exception
            Managers.Logger.LogError($"Failed to create instance of {fullyQualifiedTypeName}: Constructor threw an exception", ex.InnerException?.ToString() ?? ex.ToString());
            return null;
        }
        catch (Exception ex)
        {
            // Catch all other exceptions (NullReferenceException, TypeLoadException, etc.)
            Managers.Logger.LogError($"Failed to create instance of {fullyQualifiedTypeName}: {ex.Message}", ex.ToString());
            return null;
        }
    }

    public IEnumerable<string> GetAvailableTypeNames()
    {
        return _currentAssembly?.GetTypes().Select(t => t.FullName!).Where(name => name != null) ?? [];
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
