using System.Windows;

namespace NEngineEditor.Windows;

/// <summary>
/// Interaction logic for NewProjectDialog.xaml
/// </summary>
public partial class NewProjectDialog : Window
{
    public string? ProjectName { get; private set; }

    public NewProjectDialog()
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
    private void OK_Click(object sender, RoutedEventArgs e)
    {
        ProjectName = ProjectNameTextBox.Text;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
