using NEngineEditor.Commands;
using SFML.System;
using System.Windows.Input;

namespace NEngineEditor.ViewModel;
public class SceneEditViewModel : ViewModelBase
{
    public SceneEditViewModel()
    {
        ActiveGizmos = ActiveGizmoSet.POSITION;
    }
    public enum DraggingGizmo
    {
        X_POS,
        Y_POS,
        XY_POS,
        ROT,
        X_SCALE,
        Y_SCALE,
        XY_SCALE
    }
    public record SceneObjectDrag(Vector2i startDragPoint, Vector2i currentDragPoint, DraggingGizmo draggingGizmo);
    public SceneObjectDrag? CurrentSceneObjectDrag { get; set; }
    public bool IsDraggingSceneObject => CurrentSceneObjectDrag is not null;

    public enum ActiveGizmoSet
    {
        POSITION,
        ROTATION,
        SCALE
    }

    private ActiveGizmoSet _activeGizmoSet;
    public ActiveGizmoSet ActiveGizmos
    {
        get => _activeGizmoSet;
        set
        {
            _activeGizmoSet = value;
            OnPropertyChanged(nameof(ActiveGizmos));
        }
    }

    private ICommand? _activatePositionGizmoSet;
    public ICommand ActivatePositionGizmoSet => _activatePositionGizmoSet ??= new ActionCommand(() => SetActiveGizmoSet(ActiveGizmoSet.POSITION));

    private ICommand? _activateRotationGizmoSet;
    public ICommand ActivateRotationGizmoSet => _activateRotationGizmoSet ??= new ActionCommand(() => SetActiveGizmoSet(ActiveGizmoSet.ROTATION));

    private ICommand? _activateScaleGizmoSet;
    public ICommand ActivateScaleGizmoSet => _activateScaleGizmoSet ??= new ActionCommand(() => SetActiveGizmoSet(ActiveGizmoSet.SCALE));

    private void SetActiveGizmoSet(ActiveGizmoSet activeGizmoSet)
    {
        if (!IsDraggingSceneObject)
        {
            ActiveGizmos = activeGizmoSet;
        }
    }
}
