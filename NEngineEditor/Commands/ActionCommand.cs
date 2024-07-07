using System.Windows.Input;

namespace NEngineEditor.Commands;
public class ActionCommand : ICommand
{
    private readonly Action _action;

    public ActionCommand(Action action)
    {
        _action = action;
    }

    public void Execute(object? parameter)
    {
        _action();
    }

    public bool CanExecute(object? parameter) => true;

    public event EventHandler? CanExecuteChanged;
}
