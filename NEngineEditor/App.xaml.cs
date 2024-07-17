using NEngineEditor.Managers;
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
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        // MainWindow needs a valid project directory to start so there needs to be a Project Selection Window
        new ProjectOpenWindow().Show();
    }

    private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.LogError($"Unhandled exception: {e.Exception}");
        e.Handled = true;
    }
}

