using NEngine;

using SFML.Graphics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Based in the root directory of your project.
/// Runs the scenes created here along with your config files in the Engine's Application class.
/// Modify at your own risk.
/// </summary>
public partial class Program
{
    //  Application.WindowName = <project property window name>
    //      ... rest of properties
    //  List<SceneData> = TraverseAndDeserializeScenesInProjectFolders()
    //  foreach SceneData s => Application.AddScene(s.ToScene())
    //  Application.Run()

    // TODO: click on this file and remove the C# Compile build action 
    public static void Main()
    {
        ProjectSettings? projectSettings = LoadProjectSettings();
        if (projectSettings is not null)
        {
            Application.Instance.GameWindow.WindowBackgroundColor = projectSettings.BackgroundColor();
            Application.WindowTitle = projectSettings?.ProjectName ?? "NEngine Game";
        }

        // load scenes

        Application.Run();
    }

    private partial class ProjectSettings
    {
        public string? ProjectName { get; set; }
        public string? DefaultBackgroundColor { get; set; }
        /// <summary>
        /// An ordered list of scenes to be added
        /// </summary>
        public List<string>? ScenePaths { get; set; }

        public Color BackgroundColor()
        {
            byte ParseColorStringAtRange(Range idx) => byte.Parse(DefaultBackgroundColor[idx], NumberStyles.HexNumber);
            byte ParseColorStringAtIndex(int idx) => ParseColorStringAtRange(idx..++idx);

            if (DefaultBackgroundColor is null || ValidColorRegex().IsMatch(DefaultBackgroundColor))
            {
                return Color.White;
            }
            if (DefaultBackgroundColor.Length == 3)
            {
                return new Color(ParseColorStringAtIndex(1), ParseColorStringAtIndex(2), ParseColorStringAtIndex(3));
            }
            return new Color(ParseColorStringAtRange(1..2), ParseColorStringAtRange(3..4), ParseColorStringAtRange(5..6));
        }

        [GeneratedRegex("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
        private static partial Regex ValidColorRegex();
    }

    private static ProjectSettings? LoadProjectSettings()
    {
        string configPath = "Assets/ProjectConfig.json";
        if (!Directory.Exists(configPath))
        {
            return default;
        }
        string jsonContentString = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<ProjectSettings>(jsonContentString);
    }

    //private static List<string> FindScenesInPath(string path)
    //{
    //    static List<string> GetScenePathsInDirectory(string directoryPath)
    //    {
    //        return Directory.GetFiles(directoryPath).Where(filePath => Path.GetExtension(filePath).Equals(".scene")).ToList();
    //    }
    //    if (Directory.GetDirectories(path).Length == 0)
    //    {
    //        return GetScenePathsInDirectory(path);
    //    }
    //    else
    //    {
    //        return [.. GetScenePathsInDirectory(path), .. Directory.GetDirectories(path).SelectMany(FindScenesInPath).ToList()];
    //    }
    //}

    // TODO: Change "void" to "SceneData" define the SceneData class and how it is serialized/deserialized.
    //  probably json - class name (System.Activator<T>(deserializedType)), properties + values key-value pair of class property name, value (string, number, scene id for a linked GameObject)
    private static void LoadSceneDataFiles()
    {
        // Scenes need to be specified in order in Project Settings

        //List<string> scenePaths = FindScenesInPath("Assets");
        // foreach string path in scenePaths => File.ReadAllText(path) |> JsonSerializer.Deserialize<SceneData>
    }
}
