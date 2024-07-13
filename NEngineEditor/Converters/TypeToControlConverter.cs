using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;
using System.Reflection;

using SFML.System;

using NEngineEditor.ViewModel;

namespace NEngineEditor.Converters;

public class TypeToControlConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not MemberWrapper memberWrapper) return null;

        Type type = memberWrapper.MemberInfo switch
        {
            PropertyInfo prop => prop.PropertyType,
            FieldInfo field => field.FieldType,
            _ => typeof(object)
        };

        if (type == typeof(bool))
        {
            return CreateBooleanControl(memberWrapper);
        }
        else if (type == typeof(string))
        {
            return CreateStringControl(memberWrapper);
        }
        else if (type.IsEnum)
        {
            return CreateEnumControl(memberWrapper);
        }
        else if (type == typeof(int) || type == typeof(float) || type == typeof(double))
        {
            return CreateNumericControl(memberWrapper);
        }
        else if (type == typeof(Vector2f) || type == typeof(Vector2i) || type == typeof(Vector2u))
        {
            return CreateVector2Control(memberWrapper);
        }
        else if (type == typeof(Vector3f))
        {
            return CreateVector3Control(memberWrapper);
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private UIElement CreateBooleanControl(MemberWrapper memberWrapper)
    {
        var checkBox = new CheckBox();
        checkBox.SetBinding(CheckBox.IsCheckedProperty, new Binding("Value")
        {
            Source = memberWrapper,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        return checkBox;
    }

    private UIElement CreateStringControl(MemberWrapper memberWrapper)
    {
        var textBox = new TextBox { Width = 100 };
        textBox.SetBinding(TextBox.TextProperty, new Binding("Value")
        {
            Source = memberWrapper,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        return textBox;
    }

    private UIElement CreateEnumControl(MemberWrapper memberWrapper)
    {
        var comboBox = new ComboBox
        {
            ItemsSource = Enum.GetValues(memberWrapper.MemberInfo switch
            {
                PropertyInfo prop => prop.PropertyType,
                FieldInfo field => field.FieldType,
                _ => typeof(object)
            }),
            Width = 100
        };
        comboBox.SetBinding(ComboBox.SelectedItemProperty, new Binding("Value")
        {
            Source = memberWrapper,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });
        return comboBox;
    }

    private UIElement CreateNumericControl(MemberWrapper memberWrapper)
    {
        var textBox = new TextBox { Width = 100 };
        Type memberType = memberWrapper.MemberInfo switch
        {
            PropertyInfo prop => prop.PropertyType,
            FieldInfo field => field.FieldType,
            _ => typeof(object)
        };

        if (memberType == typeof(float) || memberType == typeof(double))
        {
            textBox.SetBinding(TextBox.TextProperty, new Binding("ValueString")
            {
                Source = memberWrapper,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });

            textBox.PreviewTextInput += (sender, e) =>
            {
                e.Handled = !IsValidFloatInput(((TextBox)sender).Text, e.Text, ((TextBox)sender).SelectionStart);
            };

            textBox.LostFocus += (sender, e) =>
            {
                if (((TextBox)sender).Text.EndsWith("."))
                {
                    ((TextBox)sender).Text = ((TextBox)sender).Text.TrimEnd('.');
                }
            };
        }
        else
        {
            textBox.SetBinding(TextBox.TextProperty, new Binding("Value")
            {
                Source = memberWrapper,
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
        }

        return textBox;
    }

    private bool IsValidFloatInput(string currentText, string newInput, int selectionStart)
    {
        string potentialNewValue = currentText.Insert(selectionStart, newInput);

        // Allow empty input, single minus sign at start, or single decimal point
        if (string.IsNullOrEmpty(potentialNewValue) ||
            (potentialNewValue == "-" && selectionStart == 0) ||
            (newInput == "." && !currentText.Contains(".")))
            return true;

        // Try parsing the potential new value
        return float.TryParse(potentialNewValue, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _);
    }

    private UIElement CreateVector2Control(MemberWrapper memberWrapper)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new TextBlock { Text = "x", Margin = new Thickness(0, 0, 5, 0) });
        panel.Children.Add(CreateVectorComponentTextBox("X", memberWrapper));
        panel.Children.Add(new TextBlock { Text = "y", Margin = new Thickness(10, 0, 5, 0) });
        panel.Children.Add(CreateVectorComponentTextBox("Y", memberWrapper));
        return panel;
    }

    private UIElement CreateVector3Control(MemberWrapper memberWrapper)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(new TextBlock { Text = "x", Margin = new Thickness(0, 0, 5, 0) });
        panel.Children.Add(CreateVectorComponentTextBox("X", memberWrapper));
        panel.Children.Add(new TextBlock { Text = "y", Margin = new Thickness(10, 0, 5, 0) });
        panel.Children.Add(CreateVectorComponentTextBox("Y", memberWrapper));
        panel.Children.Add(new TextBlock { Text = "z", Margin = new Thickness(10, 0, 5, 0) });
        panel.Children.Add(CreateVectorComponentTextBox("Z", memberWrapper));
        return panel;
    }

    private TextBox CreateVectorComponentTextBox(string component, MemberWrapper memberWrapper)
    {
        var textBox = new TextBox { Width = 50 };
        textBox.SetBinding(TextBox.TextProperty, new Binding($"VectorWrapper.{component}String")
        {
            Source = memberWrapper,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
        });

        textBox.PreviewTextInput += (sender, e) =>
        {
            e.Handled = !IsValidVectorInput(textBox, textBox.Text, e.Text, memberWrapper);
        };

        return textBox;
    }

    private bool IsValidVectorInput(TextBox textBox, string currentText, string newInput, MemberWrapper memberWrapper)
    {
        Type vectorType = memberWrapper.MemberInfo switch
        {
            PropertyInfo prop => prop.PropertyType,
            FieldInfo field => field.FieldType,
            _ => typeof(object)
        };

        string potentialNewValue = currentText.Insert(textBox.SelectionStart, newInput);

        if (vectorType == typeof(Vector2f) || vectorType == typeof(Vector3f))
        {
            // Allow empty input, single minus sign, or single decimal point
            if (string.IsNullOrEmpty(potentialNewValue) || potentialNewValue == "-" || potentialNewValue == ".")
                return true;

            // Try parsing the potential new value
            return float.TryParse(potentialNewValue, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _);
        }
        else if (vectorType == typeof(Vector2i))
        {
            return int.TryParse(potentialNewValue, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _) || potentialNewValue == "-";
        }
        else if (vectorType == typeof(Vector2u))
        {
            return uint.TryParse(potentialNewValue, NumberStyles.None, CultureInfo.InvariantCulture, out _);
        }

        return false;
    }
}