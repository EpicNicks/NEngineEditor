using System.Reflection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using SFML.System;

using NEngine.GameObjects;
using NEngine.Window;

namespace NEngineEditor.ViewModel;

public class InspectorViewModel : ViewModelBase
{
    private MainViewModel.LayeredGameObject? _selectedLayeredGameObject;
    public MainViewModel.LayeredGameObject? SelectedLayeredGameObject
    {
        get => _selectedLayeredGameObject;
        set
        {
            _selectedLayeredGameObject = value;
            OnPropertyChanged(nameof(SelectedLayeredGameObject));
            OnPropertyChanged(nameof(SelectedGameObject));
            UpdateProperties();
        }
    }

    public GameObject? SelectedGameObject => SelectedLayeredGameObject?.GameObject;

    public RenderLayer RenderLayer
    {
        get => SelectedLayeredGameObject?.RenderLayer ?? default(RenderLayer);
        set
        {
            if (SelectedLayeredGameObject != null && SelectedLayeredGameObject.RenderLayer != value)
            {
                SelectedLayeredGameObject.RenderLayer = value;
                OnPropertyChanged(nameof(RenderLayer));
                MainViewModel.Instance.SelectedGameObject = SelectedLayeredGameObject;
            }
        }
    }

    public ObservableCollection<MemberWrapper> PositionableProperties { get; } = new ObservableCollection<MemberWrapper>();
    public ObservableCollection<MemberWrapper> PublicMembers { get; } = new ObservableCollection<MemberWrapper>();

    public InspectorViewModel()
    {
        MainViewModel.Instance.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.SelectedGameObject))
            {
                SelectedLayeredGameObject = MainViewModel.Instance.SelectedGameObject;
            }
        };
    }

    private void UpdateProperties()
    {
        PositionableProperties.Clear();
        PublicMembers.Clear();

        if (SelectedGameObject == null) return;

        Type? type = SelectedGameObject.GetType();
        HashSet<string> processedMembers = ["Name"];

        // simple helper for repetitive calls with repeat args
        void AddMemberWrapperToProcess(MemberInfo memberInfo, object target, ObservableCollection<MemberWrapper> collection)
        {
            AddMemberWrapper(memberInfo, target, collection);
            processedMembers.Add(memberInfo.Name);
        }

        if (SelectedGameObject is Positionable positionable)
        {
            AddMemberWrapperToProcess(typeof(Positionable).GetProperty("Position")!, positionable, PositionableProperties);
            AddMemberWrapperToProcess(typeof(Positionable).GetProperty("Rotation")!, positionable, PositionableProperties);
            AddMemberWrapperToProcess(typeof(Positionable).GetProperty("Scale")!, positionable, PositionableProperties);
        }

        while (type != null && type != typeof(object))
        {
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (property.CanRead && property.CanWrite && !processedMembers.Contains(property.Name))
                {
                    AddMemberWrapperToProcess(property, SelectedGameObject, PublicMembers);
                }
            }

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (!processedMembers.Contains(field.Name))
                {
                    AddMemberWrapperToProcess(field, SelectedGameObject, PublicMembers);
                }
            }

            type = type.BaseType;
        }

        OnPropertyChanged(nameof(RenderLayer));
    }

    private static bool IsSupportedType(Type type)
    {
        return type == typeof(bool) ||
               type == typeof(string) ||
               type == typeof(int) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type.IsEnum ||
               type == typeof(Vector2f) ||
               type == typeof(Vector2i) ||
               type == typeof(Vector2u) ||
               type == typeof(Vector3f);
    }

    private void AddMemberWrapper(MemberInfo memberInfo, object target, ObservableCollection<MemberWrapper> collection)
    {
        Type memberType = memberInfo switch
        {
            PropertyInfo prop => prop.PropertyType,
            FieldInfo field => field.FieldType,
            _ => throw new ArgumentException("Invalid member type")
        };

        if (IsSupportedType(memberType))
        {
            var wrapper = new MemberWrapper(memberInfo, target);
            collection.Add(wrapper);
        }
    }
}

public class MemberWrapper : INotifyPropertyChanged
{
    public MemberInfo MemberInfo { get; }
    public VectorWrapper? VectorWrapper { get; }
    private object _target;

    public MemberWrapper(MemberInfo memberInfo, object target)
    {
        MemberInfo = memberInfo;
        _target = target;

        Type memberType = memberInfo switch
        {
            PropertyInfo prop => prop.PropertyType,
            FieldInfo field => field.FieldType,
            _ => typeof(object)
        };

        if (memberType == typeof(Vector2f) || memberType == typeof(Vector2i) ||
            memberType == typeof(Vector2u) || memberType == typeof(Vector3f))
        {
            object initialValue = GetValue() ?? GetDefaultValue();
            VectorWrapper = new VectorWrapper(initialValue);
            VectorWrapper.ValueChanged += (sender, args) =>
            {
                SetValue(VectorWrapper.Value);
            };
        }
    }

    public string Name => MemberInfo.Name;

    public object Value
    {
        get => VectorWrapper?.Value ?? GetValue() ?? GetDefaultValue();
        set
        {
            if (VectorWrapper != null)
            {
                VectorWrapper.Value = value;
            }
            else
            {
                SetValue(ConvertValue(value) ?? GetDefaultValue());
            }
            OnPropertyChanged(nameof(Value));
        }
    }

    private object? GetValue()
    {
        return MemberInfo switch
        {
            PropertyInfo prop => prop.GetValue(_target),
            FieldInfo field => field.GetValue(_target),
            _ => throw new InvalidOperationException("Unsupported MemberInfo type")
        };
    }

    private void SetValue(object value)
    {
        switch (MemberInfo)
        {
            case PropertyInfo prop:
                prop.SetValue(_target, value);
                break;
            case FieldInfo field:
                field.SetValue(_target, value);
                break;
            default:
                throw new InvalidOperationException("Unsupported MemberInfo type");
        }
        OnPropertyChanged(nameof(Value));
    }

    private object? ConvertValue(object value)
    {
        if (value == null || (value is string s && string.IsNullOrWhiteSpace(s)))
        {
            return GetDefaultValue();
        }

        Type targetType = MemberInfo switch
        {
            PropertyInfo prop => prop.PropertyType,
            FieldInfo field => field.FieldType,
            _ => typeof(object)
        };

        if (value is double doubleValue)
        {
            if (targetType == typeof(float))
                return (float)doubleValue;
            if (targetType == typeof(decimal))
                return (decimal)doubleValue;
            if (targetType == typeof(byte))
                return (byte)doubleValue;
            if (targetType == typeof(sbyte))
                return (sbyte)doubleValue;
            if (targetType == typeof(short))
                return (short)doubleValue;
            if (targetType == typeof(ushort))
                return (ushort)doubleValue;
            if (targetType == typeof(int))
                return (int)doubleValue;
            if (targetType == typeof(uint))
                return (uint)doubleValue;
            if (targetType == typeof(long))
                return (long)doubleValue;
            if (targetType == typeof(ulong))
                return (ulong)doubleValue;
        }

        if (value is string stringValue)
        {
            if (targetType == typeof(float))
                return float.TryParse(stringValue, out float floatResult) ? floatResult : 0f;
            if (targetType == typeof(int))
                return int.TryParse(stringValue, out int intResult) ? intResult : 0;
            if (targetType == typeof(double))
                return double.TryParse(stringValue, out double doubleResult) ? doubleResult : 0d;
        }

        return Convert.ChangeType(value, targetType);
    }
    private object GetDefaultValue()
    {
        Type targetType = MemberInfo switch
        {
            PropertyInfo prop => prop.PropertyType,
            FieldInfo field => field.FieldType,
            _ => typeof(object)
        };

        return targetType switch
        {
            Type t when t == typeof(float) => 0f,
            Type t when t == typeof(int) => 0,
            Type t when t == typeof(double) => 0d,
            Type t when t == typeof(Vector2f) => new Vector2f(0f, 0f),
            Type t when t == typeof(Vector2i) => new Vector2i(0, 0),
            Type t when t == typeof(Vector2u) => new Vector2u(0, 0),
            Type t when t == typeof(Vector3f) => new Vector3f(0f, 0f, 0f),
            Type t when t == typeof(string) => "",
            _ => targetType.IsValueType ? 
                    (Activator.CreateInstance(targetType) ?? throw new InvalidOperationException($"input for field with value type {targetType} could not be instantiated with Activator")) 
                    : throw new InvalidOperationException($"target type of field {targetType} was not a valid type")
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class VectorWrapper : INotifyPropertyChanged
{
    private object _value;

    public VectorWrapper(object initialValue)
    {
        _value = initialValue;
    }

    public object Value
    {
        get => _value;
        set
        {
            if (!_value.Equals(value))
            {
                _value = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(X));
                OnPropertyChanged(nameof(Y));
                OnPropertyChanged(nameof(Z));
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public float X
    {
        get => GetComponent(0);
        set => SetComponent(0, value);
    }

    public float Y
    {
        get => GetComponent(1);
        set => SetComponent(1, value);
    }

    public float Z
    {
        get => GetComponent(2);
        set => SetComponent(2, value);
    }

    private float GetComponent(int index)
    {
        return _value switch
        {
            Vector2f v2f => index == 0 ? v2f.X : v2f.Y,
            Vector2i v2i => index == 0 ? v2i.X : v2i.Y,
            Vector2u v2u => index == 0 ? v2u.X : v2u.Y,
            Vector3f v3f => index == 0 ? v3f.X : (index == 1 ? v3f.Y : v3f.Z),
            _ => 0f
        };
    }

    private void SetComponent(int index, float value)
    {
        Value = _value switch
        {
            Vector2f v2f => index == 0 ? new Vector2f(value, v2f.Y) : new Vector2f(v2f.X, value),
            Vector2i v2i => index == 0 ? new Vector2i((int)value, v2i.Y) : new Vector2i(v2i.X, (int)value),
            Vector2u v2u => index == 0 ? new Vector2u((uint)Math.Max(0, value), v2u.Y) : new Vector2u(v2u.X, (uint)Math.Max(0, value)),
            Vector3f v3f => index == 0 ? new Vector3f(value, v3f.Y, v3f.Z) :
                            (index == 1 ? new Vector3f(v3f.X, value, v3f.Z) : new Vector3f(v3f.X, v3f.Y, value)),
            _ => _value
        };
    }

    public event EventHandler? ValueChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}