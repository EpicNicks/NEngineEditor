using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NEngineEditor.Managers;
using NEngine.GameObjects;

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

        var references = new List<MetadataReference>();

        // Add references to the core .NET assemblies
        var assemblyNames = new[]
        {
            "System.Private.CoreLib",
            "System.Console",
            "System.Linq",
            "System.IO",
            "System.Xml",
            "System.Net.Http",
            "System.Collections",
            "System.Runtime",
            "netstandard"
        };

        foreach (var assemblyName in assemblyNames)
        {
            try
            {
                var assembly = Assembly.Load(assemblyName);
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
        var sfmlAssemblies = new[] { "SFML.System", "SFML.Window", "SFML.Graphics", "SFML.Audio" };
        foreach (var assemblyName in sfmlAssemblies)
        {
            try
            {
                var sfmlAssembly = Assembly.Load(assemblyName);
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
            var nengineAssembly = Assembly.Load("NEngine");
            references.Add(MetadataReference.CreateFromFile(nengineAssembly.Location));
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"Failed to load NEngine assembly: {ex.Message}");
        }

        // Create the compilation
        var syntaxTree = CSharpSyntaxTree.ParseText(scriptCode);
        var compilation = CSharpCompilation.Create(
            "DynamicAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Compile to memory
        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            // Handle compilation errors
            var errors = result.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);

            foreach (var error in errors)
            {
                Logger.LogError($"{error.Id}: {error.GetMessage()}");
            }
            return null;
        }

        ms.Seek(0, SeekOrigin.Begin);
        var compiledAssembly = Assembly.Load(ms.ToArray());

        // Create an instance of the compiled class
        var type = compiledAssembly.GetType(className);
        if (type == null)
        {
            throw new InvalidOperationException($"Class '{className}' not found in the script. Make sure the filename and class name match");
        }

        // Check if the type derives from GameObject
        if (!typeof(GameObject).IsAssignableFrom(type))
        {
            throw new InvalidOperationException($"The class '{className}' does not derive from GameObject and cannot be added to the scene.");
        }

        return Activator.CreateInstance(type);
    }
}