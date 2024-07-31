using NEngineEditor.Model;

namespace NEngineEditor.Managers;
public class EditorActionHistory
{
    private readonly int _maxCapacity;

    private Stack<EditorAction> _undoStack = [];
    private Stack<EditorAction> _redoStack = [];

    public EditorActionHistory(int maxCapacity)
    {
        _maxCapacity = maxCapacity;
    }
    public EditorActionHistory() : this(50) { }

    public int UndoActionCount => _undoStack.Count;
    public int RedoActionCount => _redoStack.Count;

    public void PerformAction(EditorAction editorAction)
    {
        if (_undoStack.Count == _maxCapacity)
        {
            _undoStack = new Stack<EditorAction>(_undoStack.TakeLast(_maxCapacity - 1));
        }
        _undoStack.Push(editorAction);
        _redoStack.Clear();
    }

    public bool UndoAction()
    {
        if (_undoStack.Count == 0)
        {
            return false;
        }
        _undoStack.Peek().UndoAction?.Invoke();
        _redoStack.Push(_undoStack.Pop());
        return true;
    }

    public bool RedoAction()
    {
        if (_redoStack.Count == 0)
        {
            return false;
        }
        _redoStack.Peek().DoAction?.Invoke();
        _undoStack.Push(_redoStack.Pop());
        return true;
    }

    public void ClearHistory()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }
}
