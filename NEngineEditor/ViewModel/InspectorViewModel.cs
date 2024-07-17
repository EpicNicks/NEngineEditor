using System.Reflection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using SFML.System;

using NEngine.GameObjects;
using NEngine.Window;
using System.Globalization;

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
    private string _valueString = "";

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
        else if (memberType == typeof(float) || memberType == typeof(double))
        {
            _valueString = GetValue()?.ToString() ?? "0";
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

    public string ValueString
    {
        get => _valueString;
        set
        {
            if (_valueString != value)
            {
                _valueString = value;
                UpdateValueFromString();
                OnPropertyChanged(nameof(ValueString));
            }
        }
    }

    private void UpdateValueFromString()
    {
        Type memberType = MemberInfo switch
        {
            PropertyInfo prop => prop.PropertyType,
            FieldInfo field => field.FieldType,
            _ => typeof(object)
        };

        if (memberType == typeof(float) || memberType == typeof(double))
        {
            // Allow trailing decimal point
            string parseValue = _valueString;
            if (_valueString.EndsWith("."))
            {
                parseValue += "0";
            }

            if (float.TryParse(parseValue, NumberStyles.Any, CultureInfo.InvariantCulture, out float result))
            {
                SetValue(result);
            }
        }
        // Handle other types if necessary
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

        // Don't update _valueString here to preserve user input
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
                return float.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out float floatResult) ? floatResult : 0f;
            if (targetType == typeof(int))
                return int.TryParse(stringValue, out int intResult) ? intResult : 0;
            if (targetType == typeof(double))
                return double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleResult) ? doubleResult : 0d;
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
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
            Type t when t == typeof(bool) => false,
            Type t when t == typeof(byte) => (byte)0,
            Type t when t == typeof(sbyte) => (sbyte)0,
            Type t when t == typeof(short) => (short)0,
            Type t when t == typeof(ushort) => (ushort)0,
            Type t when t == typeof(long) => 0L,
            Type t when t == typeof(ulong) => 0UL,
            Type t when t == typeof(decimal) => 0m,
            Type t when t.IsEnum => Enum.ToObject(t, 0),
            _ =>
                Activator.CreateInstance(targetType) ??
                throw new InvalidOperationException($"Cannot create an instance of {targetType}"),
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
    private string _xString = "0";
    private string _yString = "0";
    private string _zString = "0";

    public VectorWrapper(object initialValue)
    {
        _value = initialValue;
        UpdateStringRepresentations();
    }

    public object Value
    {
        get => _value;
        set
        {
            if (!_value.Equals(value))
            {
                _value = value;
                UpdateStringRepresentations();
                OnPropertyChanged();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public string XString
    {
        get => _xString;
        set
        {
            if (_xString != value)
            {
                _xString = value;
                UpdateVectorFromStrings();
                OnPropertyChanged();
            }
        }
    }

    public string YString
    {
        get => _yString;
        set
        {
            if (_yString != value)
            {
                _yString = value;
                UpdateVectorFromStrings();
                OnPropertyChanged();
            }
        }
    }

    public string ZString
    {
        get => _zString;
        set
        {
            if (_zString != value)
            {
                _zString = value;
                UpdateVectorFromStrings();
                OnPropertyChanged();
            }
        }
    }

    private void UpdateStringRepresentations()
    {
        switch (_value)
        {
            case Vector2f v2f:
                _xString = v2f.X.ToString(CultureInfo.InvariantCulture);
                _yString = v2f.Y.ToString(CultureInfo.InvariantCulture);
                break;
            case Vector2i v2i:
                _xString = v2i.X.ToString(CultureInfo.InvariantCulture);
                _yString = v2i.Y.ToString(CultureInfo.InvariantCulture);
                break;
            case Vector2u v2u:
                _xString = v2u.X.ToString(CultureInfo.InvariantCulture);
                _yString = v2u.Y.ToString(CultureInfo.InvariantCulture);
                break;
            case Vector3f v3f:
                _xString = v3f.X.ToString(CultureInfo.InvariantCulture);
                _yString = v3f.Y.ToString(CultureInfo.InvariantCulture);
                _zString = v3f.Z.ToString(CultureInfo.InvariantCulture);
                break;
        }
        OnPropertyChanged(nameof(XString));
        OnPropertyChanged(nameof(YString));
        OnPropertyChanged(nameof(ZString));
    }

    private void UpdateVectorFromStrings()
    {
        switch (_value)
        {
            case Vector2f _:
                if (float.TryParse(_xString, NumberStyles.Any, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(_yString, NumberStyles.Any, CultureInfo.InvariantCulture, out float y))
                {
                    _value = new Vector2f(x, y);
                }
                break;
            case Vector2i _:
                if (int.TryParse(_xString, out int xi) && int.TryParse(_yString, out int yi))
                {
                    _value = new Vector2i(xi, yi);
                }
                break;
            case Vector2u _:
                if (uint.TryParse(_xString, out uint xu) && uint.TryParse(_yString, out uint yu))
                {
                    _value = new Vector2u(xu, yu);
                }
                break;
            case Vector3f _:
                if (float.TryParse(_xString, NumberStyles.Any, CultureInfo.InvariantCulture, out float x3) &&
                    float.TryParse(_yString, NumberStyles.Any, CultureInfo.InvariantCulture, out float y3) &&
                    float.TryParse(_zString, NumberStyles.Any, CultureInfo.InvariantCulture, out float z3))
                {
                    _value = new Vector3f(x3, y3, z3);
                }
                break;
        }
        OnPropertyChanged(nameof(Value));
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? ValueChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}