// Implicit Usings not enabled in generated projects
using System;
using System.Collections.Generic;
using System.Linq;

using System.Reflection;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;

using SFML.System;
using SFML.Graphics;

using NEngine;
using NEngine.Window;
using NEngine.GameObjects;
using NEngine.CoreLibs.GameObjects;
using NEngine.CoreLibs.StandardFonts;

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

    // NO REFERENCES TO NEngineEditor ALLOWED
    //  ALL TYPES MUST BE DEFINED OR REDEFINED HERE EXPLICITLY
#if DEBUG
    class WarningText : UIAnchored
    {
        private Color _textColor = Color.White;
        public Color TextColor { get => _textColor; set => _textColor = value; }
        private Text warningText = new Text
        {
            DisplayedString = """
            You probably forgot to add scenes to the build.
            Go to 'File > Add Scenes To Build' and select a scene to add to the build!
            """,
            CharacterSize = 32,
            Font = Fonts.Arial
        };
        public override (UIAnchor x, UIAnchor y) Anchors => (UIAnchor.CENTER, UIAnchor.CENTER);

        public WarningText()
        {
            Drawables = [warningText];
        }

        public override void Update()
        {
            if (Application.Instance is not null)
            {
                uint originalColor = Application.Instance.GameWindow.WindowBackgroundColor.ToInteger();
                uint invertedRGB = ~originalColor & 0xFFFFFF00;
                uint originalAlpha = originalColor & 0x000000FF;
                warningText.FillColor = new Color(new Color(invertedRGB | originalAlpha));
            }
            warningText.Position = PositionLocally(warningText.GetGlobalBounds());
        }
    }
#endif


    public static void Main()
    {
        ProjectSettings? projectSettings = LoadProjectSettings();
        Application application = new Application();
        if (projectSettings is not null)
        {
            application.GameWindow.WindowBackgroundColor = projectSettings.BackgroundColor();
            Application.WindowTitle = projectSettings.ProjectName ?? "NEngine Game";

            // load scenes
            List<SceneData> sceneDataList = LoadSceneDataFiles(projectSettings);
            List<Scene> scenes = BuildScenesFromSceneData(sceneDataList);

            foreach (Scene scene in scenes)
            {
                Application.AddScene(scene);
            }
#if DEBUG
            if (scenes.Count == 0)
            {
                Application.AddScene(new Scene("You Forgot to Add Scenes to the build!", add => {
                    add((RenderLayer.UI, new WarningText()));
                }));
            }
#endif
        }
        application.Run();
    }

    private partial class ProjectSettings
    {
        public string? ProjectName { get; set; }
        public string? DefaultBackgroundColor { get; set; }
        /// <summary>
        /// An ordered list of scenes to be added
        /// </summary>
        public List<string>? Scenes { get; set; }

        public Color BackgroundColor()
        {
            byte ParseColorStringAtRange(Range idx) => byte.Parse(DefaultBackgroundColor[idx], NumberStyles.HexNumber);
            byte ParseColorStringAtIndex(int idx) => ParseColorStringAtRange(idx..++idx);

            if (DefaultBackgroundColor is null || !ValidColorRegex().IsMatch(DefaultBackgroundColor))
            {
                return Color.White;
            }
            if (DefaultBackgroundColor.Length == 4)
            {
                return new Color(ParseColorStringAtIndex(1), ParseColorStringAtIndex(2), ParseColorStringAtIndex(3));
            }
            return new Color(ParseColorStringAtRange(1..2), ParseColorStringAtRange(3..4), ParseColorStringAtRange(5..6));
        }

        [GeneratedRegex("^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$")]
        private static partial Regex ValidColorRegex();
    }

    private class SceneData
    {
        public string? Name { get; set; }
        public List<GameObjectData>? SceneGameObjects { get; set; }
    }

    private class GameObjectData
    {
        public string? GameObjectClass { get; set; }
        public RenderLayer RenderLayer { get; set; }
        public string? Name { get; set; }
        public Guid Guid { get; set; }
        public Dictionary<string, TypeValuePair>? GameObjectPropertyNameTypeValue { get; set; }

        public class TypeValuePair
        {
            public string? Type { get; set; }
            public string? Value { get; set; }
        }
    }

    private static partial class Vector2fParser
    {
        public static Vector2f ParseOrZero(string vector2fString)
        {
            var match = ValidVector2fRegex().Match(vector2fString);
            if (match.Groups.Count == 2 && float.TryParse(match.Groups[0].Value, out float x) && float.TryParse(match.Groups[1].Value, out float y))
            {
                return new(x, y);
            }
            return new(0, 0);
        }
        [GeneratedRegex(@"\{\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\}")]
        public static partial Regex ValidVector2fRegex();
    }

    private static partial class Vector2iParser
    {
        public static Vector2i ParseOrZero(string vector2iString)
        {
            var match = ValidVector2iRegex().Match(vector2iString);
            if (match.Groups.Count == 2 && int.TryParse(match.Groups[0].Value, out int x) && int.TryParse(match.Groups[1].Value, out int y))
            {
                return new(x, y);
            }
            return new(0, 0);
        }
        [GeneratedRegex(@"\{\s*(\d+)\s*,\s*(\d+)\s*\}")]
        public static partial Regex ValidVector2iRegex();
    }
    private static partial class Vector2uParser
    {
        public static Vector2u ParseOrZero(string vector2uString)
        {
            var match = ValidVector2uRegex().Match(vector2uString);
            if (match.Groups.Count == 2 && uint.TryParse(match.Groups[0].Value, out uint x) && uint.TryParse(match.Groups[1].Value, out uint y))
            {
                return new(x, y);
            }
            return new(0, 0);
        }
        [GeneratedRegex(@"\{\s*(\d+)\s*,\s*(\d+)\s*\}")]
        public static partial Regex ValidVector2uRegex();
    }

    private static partial class Vector3fParser
    {
        public static Vector3f ParseOrZero(string vector3fString)
        {
            var match = ValidVector3fRegex().Match(vector3fString);
            if (match.Groups.Count == 3 && float.TryParse(match.Groups[0].Value, out float x) && float.TryParse(match.Groups[1].Value, out float y) && float.TryParse(match.Groups[2].Value, out float z))
            {
                return new(x, y, z);
            }
            return new(0, 0, 0);
        }
        [GeneratedRegex(@"\{\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)\s*\}")]
        public static partial Regex ValidVector3fRegex();
    }

    private static object? ConvertProperty(string typeOfValue, string value)
    {
        return typeOfValue switch
        {
            "string" or "String" => value,
            "bool" or "Boolean" => bool.Parse(value),
            "int" or "Int32" => int.Parse(value),
            "float" or "Single" => float.Parse(value),
            "double" or "Double" => double.Parse(value),
            "Vector2u" => Vector2uParser.ParseOrZero(value),
            "Vector2f" => Vector2fParser.ParseOrZero(value),
            "Vector2i" => Vector2iParser.ParseOrZero(value),
            "Vector3f" => Vector3fParser.ParseOrZero(value),
            "Reference" or "reference" or "Guid" or "guid" => Guid.Parse(value),
            _ => ""
        };
    }

    private static ProjectSettings? LoadProjectSettings()
    {
        string configPath = Path.Combine(
            Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName,
            "Assets",
            "ProjectConfig.json"
        );
        if (!File.Exists(configPath))
        {
            return default;
        }
        string jsonContentString = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<ProjectSettings>(jsonContentString);
    }

    private static List<SceneData> LoadSceneDataFiles(ProjectSettings projectSettings)
    {
        List<SceneData> scenes = [];
        List<string> scenePaths = projectSettings.Scenes ?? [];
        foreach (string path in scenePaths)
        {
            string sceneJsonString = File.ReadAllText(path);
            try
            {
                SceneData? sceneData = JsonSerializer.Deserialize<SceneData>(sceneJsonString);
                // TODO: ensure the SceneData deserialization accounts for nested objects such as GameObjectData.TypeValuePair
                if (sceneData is not null)
                {
                    scenes.Add(sceneData);
                }
            }
            catch (JsonException)
            {
                // not sure how to handle debug logging on a build yet
                //  if debug set (somehow) set the build type to console app?
            }
        }

        return scenes;
    }

    private static readonly string[] _specialProperties = ["Position", "Rotation"];

    private static List<Scene> BuildScenesFromSceneData(List<SceneData> sceneDataList)
    {
        List<Scene> builtScenes = [];
        int unnamedSceneCount = 1;
        foreach (SceneData sceneData in sceneDataList)
        {
            List<GameObjectData> sceneGameObjectData = sceneData.SceneGameObjects ?? [];
            List<(RenderLayer renderLayer, GameObject gameObject)> toAddGameObjects = [];
            List<GameObjectData> invalidGameObjects = [];
            foreach (GameObjectData gameObjectData in sceneGameObjectData)
            {
                if
                (
                    gameObjectData.GameObjectClass is null
                    || Type.GetType(gameObjectData.GameObjectClass) is not Type gameObjectType
                    || Activator.CreateInstance(gameObjectType) is not GameObject gameObject
                )
                {
                    invalidGameObjects.Add(gameObjectData);
                    continue;
                }
                toAddGameObjects.Add((gameObjectData.RenderLayer, gameObject));
            }
            sceneGameObjectData.RemoveAll(invalidGameObjects.Contains);
            // resolve properties (second loop to resolve Guid references to objects which need to be instantiated)
            foreach ((int i, GameObjectData gameObjectData) in sceneGameObjectData.Select((value, i) => (i, value)))
            {
                try
                {
                    if (gameObjectData.GameObjectClass is null)
                    {
                        continue;
                    }
                    if (Type.GetType(gameObjectData.GameObjectClass) is not Type gameObjectType)
                    {
                        continue;
                    }
                    GameObject gameObject = toAddGameObjects[i].gameObject;
                    if (gameObjectData.GameObjectPropertyNameTypeValue is null)
                    {
                        continue;
                    }
                    foreach (string memberName in gameObjectData.GameObjectPropertyNameTypeValue.Keys)
                    {
                        if (_specialProperties.Contains(memberName) && gameObject is Positionable)
                        {
                            PropertyInfo? propertyInfo = gameObjectType.GetProperty(memberName);
                            GameObjectData.TypeValuePair typeValue = gameObjectData.GameObjectPropertyNameTypeValue[memberName];
                            if (propertyInfo is null || typeValue.Type is null || typeValue.Value is null || ConvertProperty(typeValue.Type, typeValue.Value) is not object propertyValue)
                            {
                                continue;
                            }
                            propertyInfo.SetValue(gameObject, propertyValue);
                        }
                        else
                        {
                            FieldInfo? fieldInfo = gameObjectType.GetField(memberName);
                            GameObjectData.TypeValuePair typeValue = gameObjectData.GameObjectPropertyNameTypeValue[memberName];
                            if (fieldInfo is null || typeValue.Type is null || typeValue.Value is null)
                            {
                                continue;
                            }
                            object? fieldValue = ConvertProperty(typeValue.Type, typeValue.Value);
                            if (fieldValue is string fieldString && string.IsNullOrEmpty(fieldString))
                            {
                                Type? fieldType = gameObject.GetType().GetField(memberName)?.FieldType;
                                if (fieldType is not null)
                                {
                                    Enum.TryParse(fieldType, typeValue.Value, out fieldValue);
                                }
                            }
                            if (fieldValue == null)
                            {
                                continue;
                            }
                            if (fieldValue is Guid guidProperty)
                            {
                                int foundIndex = sceneGameObjectData.IndexOf(sceneGameObjectData.Where(gObjData => gObjData.Guid == guidProperty).First());
                                if (foundIndex != -1) // Guid was either not found or Guid.Empty (and therefore not found)
                                {
                                    fieldInfo.SetValue(gameObject, toAddGameObjects[foundIndex].gameObject);
                                }
                                else
                                {
                                    fieldInfo.SetValue(gameObject, null);
                                }
                            }
                            else
                            {
                                fieldInfo.SetValue(gameObject, fieldValue);
                            }
                        }
                    }
                }
                catch (Exception) { }
            }
            builtScenes.Add(new Scene(sceneData.Name ?? $"Scene {unnamedSceneCount++}", add =>
            {
                add([.. toAddGameObjects]);
            }));
        }
        return builtScenes;
    }
}
