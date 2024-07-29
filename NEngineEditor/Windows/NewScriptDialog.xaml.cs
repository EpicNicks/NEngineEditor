using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

using NEngineEditor.Commands;
using NEngineEditor.ViewModel;

namespace NEngineEditor.Windows;

public partial class NewScriptDialog : Window, INotifyPropertyChanged
{
    public enum CsScriptType
    {
        GAMEOBJECT,
        POSITIONABLE,
        MOVEABLE,
        UIANCHORED
    }

    private CsScriptType _scriptType = CsScriptType.GAMEOBJECT;
    public CsScriptType ScriptType
    {
        get => _scriptType;
        set
        {
            if (_scriptType != value)
            {
                _scriptType = value;
                OnPropertyChanged();
            }
        }
    }

    public string CreateItemTypeString => CreateItemType switch
    {
        ContentBrowserViewModel.CreateItemType.CS_SCRIPT => "New C# Script",
        ContentBrowserViewModel.CreateItemType.FOLDER => "New folder",
        ContentBrowserViewModel.CreateItemType.SCENE => "New Scene",
        _ => "Something completely different"
    };

    public ContentBrowserViewModel.CreateItemType CreateItemType { get; private set; }
    public string? EnteredName { get; private set; }

    private ICommand? _okCommand;
    public ICommand OkCommand => _okCommand ??= new ActionCommand(() => Accept());

    private ICommand? _cancelCommand;
    public ICommand CancelCommand => _cancelCommand ??= new ActionCommand(() => Cancel());

    public NewScriptDialog(ContentBrowserViewModel.CreateItemType createItemType)
    {
        InitializeComponent();
        DataContext = this;
        CreateItemType = createItemType;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ScriptNameTextBox.Focus();
    }

    private void OK_Click(object? sender, RoutedEventArgs e)
    {
        Accept();
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Cancel();
    }

    private void Accept()
    {
        EnteredName = ScriptNameTextBox.Text;
        DialogResult = true;
    }

    private void Cancel()
    {
        DialogResult = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class EnumBindingSource : MarkupExtension
{
    private readonly Type _enumType;

    public EnumBindingSource(Type enumType)
    {
        if (enumType is null || !enumType.IsEnum)
            throw new ArgumentException("EnumType must be an enum type");

        _enumType = enumType;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        return Enum.GetValues(_enumType);
    }
}