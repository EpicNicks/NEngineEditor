using NEngineEditor.Model;

namespace NEngineEditor.Managers;
public class EditorActionHistory
{
    private Stack<EditorAction> _undoStack = [];
    private Stack<EditorAction> _redoStack = [];

    public void PerformAction(EditorAction editorAction)
    {
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
}
