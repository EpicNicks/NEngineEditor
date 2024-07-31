namespace NEngineEditor.Model;
public class EditorAction
{
    /// <summary>
    /// To be consumed on Redo, the action which was performed when this was created
    /// </summary>
    public required Action DoAction { get; init; }
    /// <summary>
    /// The inverse of the action performed when this was created, called to undo it
    /// </summary>
    public required Action UndoAction { get; init; }
}
