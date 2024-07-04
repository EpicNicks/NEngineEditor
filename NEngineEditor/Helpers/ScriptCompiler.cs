using System.IO;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

using NEngineEditor.Managers;

namespace NEngineEditor.Helpers;
public static class ScriptCompiler
{
    public static object? CompileAndInstantiateFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Script file not found.", filePath);
        }

        string scriptCode = File.ReadAllText(filePath);
        string className = Path.GetFileNameWithoutExtension(filePath);

        List<MetadataReference> references = [];

        // Add references to the core .NET assemblies
        string[] assemblyNames =
        [
            "System.Private.CoreLib",
            "System.Console",
            "System.Linq",
            "System.IO",
            "System.Xml",
            "System.Net.Http",
            "System.Collections",
            "System.Runtime",
            "netstandard"
        ];

        foreach (string assemblyName in assemblyNames)
        {
            try
            {
                Assembly assembly = Assembly.Load(assemblyName);
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to load assembly {assemblyName}: {ex.Message}");
            }
        }

        // Add reference to the NEngineEditor assembly
        references.Add(MetadataReference.CreateFromFile(typeof(ScriptCompiler).Assembly.Location));

        // Add references to SFML.NET assemblies
        string[] sfmlAssemblies = ["SFML.System", "SFML.Window", "SFML.Graphics", "SFML.Audio"];
        foreach (string assemblyName in sfmlAssemblies)
        {
            try
            {
                Assembly sfmlAssembly = Assembly.Load(assemblyName);
                references.Add(MetadataReference.CreateFromFile(sfmlAssembly.Location));
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Failed to load SFML assembly {assemblyName}: {ex.Message}");
            }
        }

        // Add reference to NEngine
        try
        {
            Assembly nengineAssembly = Assembly.Load("NEngine");
            references.Add(MetadataReference.CreateFromFile(nengineAssembly.Location));
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Failed to load NEngine assembly: {ex.Message}");
        }

        // Create the compilation
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(scriptCode);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "DynamicAssembly",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Compile to memory
        MemoryStream ms = new();
        EmitResult result = compilation.Emit(ms);

        if (!result.Success)
        {
            // Handle compilation errors
            IEnumerable<Diagnostic> errors = result.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);

            foreach (Diagnostic error in errors)
            {
                Logger.LogError($"Script Instantiation Error: {error.Id}: {error.GetMessage()}");
            }
            return null;
        }

        ms.Seek(0, SeekOrigin.Begin);
        Assembly compiledAssembly = Assembly.Load(ms.ToArray());

        // Create an instance of the compiled class
        Type? type = compiledAssembly.GetType(className);
        if (type == null)
        {
            throw new InvalidOperationException($"Class '{className}' not found in the script. Make sure the filename and class name match");
        }

        return Activator.CreateInstance(type);
    }
}