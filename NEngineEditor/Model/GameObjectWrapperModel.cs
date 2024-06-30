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
    public Dictionary<string, (string, object)>? GameObjectPropertyNameTypeValue { get; set; }
    public RenderLayer RenderLayer { get; set; }

    public GameObjectWrapperModel(string gameObjectClass)
    {
        GameObjectClass = gameObjectClass;
        Type? gameObjectType = Type.GetType(gameObjectClass);
        GameObjectPropertyNameTypeValue = PropertiesAndValuesFromPublicFields(gameObjectType);
        RenderLayer = DefaultRenderLayer(gameObjectType);
    }

    public override string ToString()
    {
        return Name ?? "NamelessGO";
    }

    private static Dictionary<string, (string, object)> PropertiesAndValuesFromPublicFields(Type? gameObjectType)
    {
        Dictionary<string, (string, object)> ret = [];
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

        foreach (var publicMember in gameObjectType.GetMembers())
        {
            if (publicMember.DeclaringType is null)
            {
                continue;
            }
            if (IsAllowedValueType(publicMember.DeclaringType))
            {
                ret.Add(publicMember.Name, (publicMember.DeclaringType.ToString(), Activator.CreateInstance(publicMember.DeclaringType)!));
            }
            else if (publicMember.DeclaringType.IsAssignableTo(typeof(GameObject)))
            {
                // need to populate with Guid of something in the scene or some other way to reference another object in the scene
                ret.Add(publicMember.Name, (typeof(GameObject).ToString(), default(Guid)));
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
