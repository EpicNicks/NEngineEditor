using SFML.System;

using NEngine.CoreLibs.GameObjects;
using NEngine.GameObjects;
using NEngine.Window;

namespace NEngineEditor.Model;
/// <summary>
/// A GUID identified container for a NEngine GameObject and its RenderLayer
/// </summary>
public class GameObjectWrapperModel
{
    /// <summary>
    /// Used in the scene to resolve references to other GameObjects
    /// </summary>
    public Guid Guid { get; set; }
    public string? Name { get; set; }
    public string? GameObjectClass { get; set; }
    public Dictionary<string, TypeValuePair>? GameObjectPropertyNameTypeValue { get; set; }
    public RenderLayer RenderLayer { get; set; }

    public class TypeValuePair
    {
        public string? Type { get; set; }
        public string? Value { get; set; }
    }

    public GameObjectWrapperModel(string gameObjectClass)
    {
        Type? gameObjectType = Type.GetType(gameObjectClass);

        GameObjectClass = gameObjectClass;
        GameObjectPropertyNameTypeValue = PropertiesAndValuesFromPublicFields(gameObjectType);
        RenderLayer = DefaultRenderLayer(gameObjectType);
    }

    public GameObjectWrapperModel FromJson(string jsonString)
    {
        // TODO: load and parse into properties as well as property dictionary following the SceneData.example.json file

        throw new NotImplementedException();
    }

    public override string ToString()
    {
        return Name ?? "NamelessGO";
    }

    /// <summary>
    /// Creates the dictionary of default values for use in creating a fresh GameObjectWrapper in the Inspector
    /// </summary>
    /// <param name="gameObjectType"></param>
    /// <returns></returns>
    private static Dictionary<string, TypeValuePair> PropertiesAndValuesFromPublicFields(Type? gameObjectType)
    {
        Dictionary<string, TypeValuePair> ret = [];
        if (gameObjectType is null || !gameObjectType.IsAssignableTo(typeof(GameObject)))
        {
            return [];
        }

        static bool IsAllowedValueType(Type type)
        {
            return new[] 
            { 
                typeof(byte),
                typeof(sbyte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(float),
                typeof(double),
                typeof(decimal),
                typeof(bool),
                typeof(Vector2f),
                typeof(Vector2i),
                typeof(Vector2u),
                typeof(Vector3f),
            }.Contains(type);
        }

        static string ValueTypeToString(object? value)
        {
            return value switch
            {
                sbyte or byte or int or uint or short or ushort or long or ulong or float or double or decimal or bool => value.ToString()!,
                Vector2i v => $"{{ {v.X}, {v.Y} }}",
                Vector2f v => $"{{ {v.X}, {v.Y} }}",
                Vector2u v => $"{{ {v.X}, {v.Y} }}",
                Vector3f v => $"{{ {v.X}, {v.Y}, {v.Z} }}",
                _ => ""
            };
        }

        foreach (var publicMember in gameObjectType.GetMembers())
        {
            if (publicMember.DeclaringType is null)
            {
                continue;
            }
            if (IsAllowedValueType(publicMember.DeclaringType))
            {
                ret.Add(publicMember.Name, new() { Type = publicMember.DeclaringType.ToString(), Value = ValueTypeToString(Activator.CreateInstance(publicMember.DeclaringType)) });
            }
            else if (publicMember.DeclaringType.IsAssignableTo(typeof(GameObject)))
            {
                ret.Add(publicMember.Name, new() { Type = typeof(GameObject).ToString(), Value = default(Guid).ToString() });
            }
        }

        return ret;
    }

    private static RenderLayer DefaultRenderLayer(Type? gameObjectDerivedType)
    {
        if (gameObjectDerivedType is null)
        {
            return RenderLayer.NONE;
        }
        if (gameObjectDerivedType.IsAssignableTo(typeof(UIAnchored)))
        {
            return RenderLayer.UI;
        }
        if (gameObjectDerivedType.IsAssignableTo(typeof(Positionable)))
        {
            return RenderLayer.BASE;
        }
        return RenderLayer.NONE;
    }
}
