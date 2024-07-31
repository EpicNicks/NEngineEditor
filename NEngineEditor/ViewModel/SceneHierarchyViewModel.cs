using System.Collections.ObjectModel;
using System.Windows.Input;

using NEngineEditor.Commands.Generic;

namespace NEngineEditor.ViewModel;
public class SceneHierarchyViewModel : ViewModelBase
{
    public MainViewModel.LayeredGameObject? SelectedGameObject
    {
        get => MainViewModel.Instance.SelectedGameObject;
        set => MainViewModel.Instance.SelectedGameObject = value;
    }
    public ObservableCollection<MainViewModel.LayeredGameObject> SceneGameObjects => MainViewModel.Instance.SceneGameObjects;

    public string LoadedSceneName => MainViewModel.Instance.LoadedSceneName;

    private ICommand? _deleteInstanceCommand;
    public ICommand DeleteInstanceCommand => _deleteInstanceCommand ??= new ActionCommand<MainViewModel.LayeredGameObject>(MainViewModel.Instance.DeleteInstanceCommand.Execute);

    private ICommand? _duplicateInstanceCommand;
    public ICommand DuplicateInstanceCommand => _duplicateInstanceCommand ??= new ActionCommand<MainViewModel.LayeredGameObject>(MainViewModel.Instance.DuplicateInstanceCommand.Execute);
}
