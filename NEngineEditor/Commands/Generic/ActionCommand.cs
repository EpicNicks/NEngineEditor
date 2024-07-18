using System.Windows.Input;

namespace NEngineEditor.Commands.Generic;
public class ActionCommand<T> : ICommand
{
    private readonly Action<T> _action;

    public ActionCommand(Action<T> action)
    {
        _action = action;
    }

    public void Execute(object? parameter)
    {
        T t = parameter is T ? (T)parameter : throw new InvalidOperationException($"Passed parameter was not of type {typeof(T)}");
        _action(t);
    }

    public bool CanExecute(object? parameter) => true;

    public event EventHandler? CanExecuteChanged;
}
