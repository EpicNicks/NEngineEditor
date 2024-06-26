using System.Windows;

using NEngineEditor.ViewModel;

namespace NEngineEditor.Windows;
/// <summary>
/// Interaction logic for NewItemDialog.xaml
/// </summary>
public partial class NewItemDialog : Window
{
    public string CreateItemTypeString => CreateItemType switch
    {
        ContentBrowserViewModel.CreateItemType.CS_SCRIPT => "New C# Script",
        ContentBrowserViewModel.CreateItemType.FOLDER => "New folder",
        _ => "Something completely different"
    };
    public ContentBrowserViewModel.CreateItemType CreateItemType { get; private set; }
    public string? EnteredName { get; private set; }

    public NewItemDialog(ContentBrowserViewModel.CreateItemType createItemType)
    {
        InitializeComponent();
        DataContext = this;
        CreateItemType = createItemType;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        EnteredName = ProjectNameTextBox.Text;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
