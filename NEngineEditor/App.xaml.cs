using NEngineEditor.Windows;
using System.Windows;

namespace NEngineEditor;
/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // MainWindow needs a valid project directory to start so there needs to be a Project Selection Window
        new ProjectOpenWindow().Show();
    }
}

