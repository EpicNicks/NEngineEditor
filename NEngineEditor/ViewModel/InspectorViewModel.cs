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
            if (IsVectorType(memberType))
            {
                var wrapper = new VectorWrapper { Value = GetMemberValue(memberInfo, target) };
                wrapper.ValueChanged += (sender, args) =>
                {
                    SetMemberValue(memberInfo, target, wrapper.Value);
                    MainViewModel.Instance.SelectedGameObject = SelectedLayeredGameObject;
                };
                collection.Add(new MemberWrapper(memberInfo, wrapper, target));
            }
            else
            {
                collection.Add(new MemberWrapper(memberInfo, null, target));
            }
        }
    }

    private bool IsVectorType(Type type)
    {
        return type == typeof(Vector2f) || type == typeof(Vector2i) || type == typeof(Vector2u) || type == typeof(Vector3f);
    }

    private object? GetMemberValue(MemberInfo memberInfo, object target)
    {
        return memberInfo switch
        {
            PropertyInfo prop => prop.GetValue(target),
            FieldInfo field => field.GetValue(target),
            _ => null
        };
    }

    private void SetMemberValue(MemberInfo memberInfo, object target, object? value)
    {
        switch (memberInfo)
        {
            case PropertyInfo prop:
                prop.SetValue(target, value);
                break;
            case FieldInfo field:
                field.SetValue(target, value);
                break;
        }
    }
}

public class MemberWrapper : INotifyPropertyChanged
{
    public MemberInfo MemberInfo { get; }
    public VectorWrapper? VectorWrapper { get; }
    private object? _target;

    public MemberWrapper(MemberInfo memberInfo, VectorWrapper? vectorWrapper, object? target)
    {
        MemberInfo = memberInfo;
        VectorWrapper = vectorWrapper;
        _target = target;
    }

    public string Name => MemberInfo.Name;

    public object? Value
    {
        get => VectorWrapper?.Value ?? GetValue();
        set
        {
            if (VectorWrapper != null)
            {
                VectorWrapper.Value = value;
                VectorWrapper.NotifyValueChanged();
            }
            else
            {
                SetValue(ConvertValue(value));
            }
            OnPropertyChanged(nameof(Value));
        }
    }

    private object? ConvertValue(object? value)
    {
        if (value == null) return null;

        Type targetType = MemberInfo switch
        {
            PropertyInfo prop => prop.PropertyType,
            FieldInfo field => field.FieldType,
            _ => typeof(object)
        };

        if (value is string stringValue)
        {
            if (targetType == typeof(float) || targetType == typeof(Single))
                return float.TryParse(stringValue, out float result) ? result : 0f;
            if (targetType == typeof(int))
                return int.TryParse(stringValue, out int result) ? result : 0;
            if (targetType == typeof(double))
                return double.TryParse(stringValue, out double result) ? result : 0d;
        }

        return value;
    }

    private object? GetValue()
    {
        return MemberInfo switch
        {
            PropertyInfo prop => prop.GetValue(_target),
            FieldInfo field => field.GetValue(_target),
            _ => null
        };
    }

    private void SetValue(object? value)
    {
        switch (MemberInfo)
        {
            case PropertyInfo prop:
                prop.SetValue(_target, value);
                break;
            case FieldInfo field:
                field.SetValue(_target, value);
                break;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class VectorWrapper
{
    public object? Value { get; set; }
    public event EventHandler? ValueChanged;

    public float X
    {
        get => GetVectorComponent(0);
        set => SetVectorComponent(0, value);
    }

    public float Y
    {
        get => GetVectorComponent(1);
        set => SetVectorComponent(1, value);
    }

    public float Z
    {
        get => GetVectorComponent(2);
        set => SetVectorComponent(2, value);
    }

    private float GetVectorComponent(int index)
    {
        return Value switch
        {
            Vector2f v2f => index == 0 ? v2f.X : v2f.Y,
            Vector2i v2i => index == 0 ? v2i.X : v2i.Y,
            Vector2u v2u => index == 0 ? v2u.X : v2u.Y,
            Vector3f v3f => index == 0 ? v3f.X : (index == 1 ? v3f.Y : v3f.Z),
            _ => 0f
        };
    }

    private void SetVectorComponent(int index, float value)
    {
        Value = Value switch
        {
            Vector2f v2f => index == 0 ? new Vector2f(value, v2f.Y) : new Vector2f(v2f.X, value),
            Vector2i v2i => index == 0 ? new Vector2i((int)value, v2i.Y) : new Vector2i(v2i.X, (int)value),
            Vector2u v2u => index == 0 ? new Vector2u((uint)value, v2u.Y) : new Vector2u(v2u.X, (uint)value),
            Vector3f v3f => index == 0 ? new Vector3f(value, v3f.Y, v3f.Z) :
                            (index == 1 ? new Vector3f(v3f.X, value, v3f.Z) : new Vector3f(v3f.X, v3f.Y, value)),
            _ => Value
        };
        NotifyValueChanged();
    }

    public void NotifyValueChanged()
    {
        ValueChanged?.Invoke(this, EventArgs.Empty);
    }
}