using NEngineEditor.Commands;

using System.Windows.Input;

namespace NEngineEditor.ViewModel;
public class SceneEditViewModel : ViewModelBase
{
    public enum ActiveGizmoSet
    {
        POSITION,
        ROTATION,
        SCALE
    }

    private ActiveGizmoSet _activeGizmoSet = ActiveGizmoSet.POSITION;
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
    public ICommand ActivatePositionGizmoSet => _activatePositionGizmoSet ??= new ActionCommand(() => ActiveGizmos = ActiveGizmoSet.POSITION);

    private ICommand? _activateRotationGizmoSet;
    public ICommand ActivateRotationGizmoSet => _activateRotationGizmoSet ??= new ActionCommand(() => ActiveGizmos = ActiveGizmoSet.ROTATION);

    private ICommand? _activateScaleGizmoSet;
    public ICommand ActivateScaleGizmoSet => _activateScaleGizmoSet ??= new ActionCommand(() => ActiveGizmos = ActiveGizmoSet.SCALE);
}
