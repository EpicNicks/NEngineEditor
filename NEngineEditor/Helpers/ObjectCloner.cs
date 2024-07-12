using System.Reflection;
using System.Collections;

namespace NEngineEditor.Helpers;
public static class ObjectCloner
{
    [Flags]
    public enum MemberTypes
    {
        None = 0,
        Fields = 1,
        Properties = 1 << 1,
        Events = 1 << 2,
        All = Fields | Properties | Events
    }

    public static void CloneMembers(object source, object target, MemberTypes memberTypes = MemberTypes.All, bool deepClone = false)
    {
        Type sourceType = source.GetType();
        Type targetType = target.GetType();

        if (memberTypes.HasFlag(MemberTypes.Properties))
        {
            CloneProperties(source, target, sourceType, targetType, deepClone);
        }

        if (memberTypes.HasFlag(MemberTypes.Fields))
        {
            CloneFields(source, target, sourceType, targetType, deepClone);
        }

        if (memberTypes.HasFlag(MemberTypes.Events))
        {
            CloneEvents(source, target, sourceType, targetType);
        }
    }

    private static void CloneProperties(object source, object target, Type sourceType, Type targetType, bool deepClone)
    {
        foreach (PropertyInfo sourceProperty in sourceType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            PropertyInfo? targetProperty = targetType.GetProperty(sourceProperty.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetProperty is not null && targetProperty.CanWrite && targetProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
            {
                object? value = sourceProperty.GetValue(source);
                if (deepClone)
                {
                    value = DeepCloneValue(value);
                }
                targetProperty.SetValue(target, value);
            }
        }
    }

    private static void CloneFields(object source, object target, Type sourceType, Type targetType, bool deepClone)
    {
        foreach (FieldInfo sourceField in sourceType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            FieldInfo? targetField = targetType.GetField(sourceField.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetField != null && targetField.FieldType.IsAssignableFrom(sourceField.FieldType))
            {
                object? value = sourceField.GetValue(source);
                if (deepClone)
                {
                    value = DeepCloneValue(value);
                }
                targetField.SetValue(target, value);
            }
        }
    }

    private static void CloneEvents(object source, object target, Type sourceType, Type targetType)
    {
        foreach (EventInfo sourceEvent in sourceType.GetEvents(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            EventInfo? targetEvent = targetType.GetEvent(sourceEvent.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (targetEvent != null)
            {
                var field = sourceType.GetField(sourceEvent.Name, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    var value = field.GetValue(source);
                    if (value != null)
                    {
                        field = targetType.GetField(targetEvent.Name, BindingFlags.NonPublic | BindingFlags.Instance);
                        field?.SetValue(target, value);
                    }
                }
            }
        }
    }

    private static object? DeepCloneValue(object? value)
    {
        if (value == null)
            return null;

        Type type = value.GetType();

        if (type.IsPrimitive || type == typeof(string))
            return value;

        if (type.IsArray)
        {
            if (type.GetElementType() is not Type elementType || value is not Array array)
            {
                return null;
            }
            Array copiedArray = Array.CreateInstance(elementType, array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                copiedArray.SetValue(DeepCloneValue(array.GetValue(i)), i);
            }
            return copiedArray;
        }

        if (value is IList list)
        {
            Type listType = type.GetGenericArguments()[0];
            IList? copiedList = Activator.CreateInstance(typeof(List<>).MakeGenericType(listType)) as IList;
            foreach (var item in list)
            {
                copiedList?.Add(DeepCloneValue(item));
            }
            return copiedList;
        }

        if (type.IsClass)
        {
            object? newObject = Activator.CreateInstance(type);
            if (newObject is null)
            {
                return null;
            }
            CloneMembers(value, newObject, MemberTypes.All, true);
            return newObject;
        }

        return value;
    }
}