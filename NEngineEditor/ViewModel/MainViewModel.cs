using System.Collections.ObjectModel;

using SFML.Graphics;
using SFML.System;

using NEngine.GameObjects;
using NEngine.Window;
using NEngineEditor.Model;

namespace NEngineEditor.ViewModel;
public class MainViewModel : ViewModelBase
{
    private string _projectDirectory = "";
    public string ProjectDirectory
    {
        get => _projectDirectory;
        set
        {
            _projectDirectory = value;
            OnPropertyChanged(nameof(ProjectDirectory));
        }
    }
    private (string name, string filepath) _loadedScene;
    public (string name, string filepath) LoadedScene
    {
        get => _loadedScene;
        set
        {
            _loadedScene = value;
            OnPropertyChanged(nameof(LoadedScene));
        }
    }

    // For the inspector (null when none is selected)
    private LayeredGameObject? _selectedGameObject;
    public LayeredGameObject? SelectedGameObject
    {
        get => _selectedGameObject;
        set
        {
            _selectedGameObject = value;
            OnPropertyChanged(nameof(SelectedGameObject));
        }
    }

    private ObservableCollection<LayeredGameObject> _sceneGameObjects;
    public ObservableCollection<LayeredGameObject> SceneGameObjects
    {
        get => _sceneGameObjects;
        set
        {
            _sceneGameObjects = value;
            OnPropertyChanged(nameof(SceneGameObjects));
        }
    }

    public ObservableCollection<LogEntry> _logs = [];
    public ObservableCollection<LogEntry> Logs
    {
        get => _logs;
        set
        {
            _logs = value;
            OnPropertyChanged(nameof(Logs));
        }
    }

    private static MainViewModel? instance;
    public static MainViewModel Instance => instance ??= new MainViewModel();

    public class LayeredGameObject
    {
        public required RenderLayer RenderLayer { get; set; }
        public required GameObject GameObject { get; set; }
        public void Deconstruct(out RenderLayer renderLayer, out GameObject gameObject)
        {
            renderLayer = RenderLayer;
            gameObject = GameObject;
        }
        public override string ToString()
        {
            return GameObject?.Name ?? "Nameless GO (should be auto-renamed by the scene manager to avoid naming collisions)";
        }
    }

    // for testing
    class Sid : Positionable
    {
        public string? greetingText;
        private RectangleShape rectangle;

        public Sid()
        {
            rectangle = new RectangleShape { Size = new Vector2f(100, 100), Rotation = 45f, FillColor = Color.Red };
        }

        public override List<Drawable> Drawables => [rectangle];
    }

    private MainViewModel()
    {
        Logs.CollectionChanged += Logs_CollectionChanged;
        _loadedScene = ("Unnamed Scene", "");
        _sceneGameObjects =
        [
            // TODO: (DONE) modify this to use raw game objects which the inspector will have an interface
            // to modify by creating a GUI representation of public fields to use reflection
            // to modify on the instance itself. This avoids regenerating the same GameObjects
            // from metadata each render frame in the Editor
            
            // for testing, should be empty normally

            // RenderLayer can be set in a Generated Pseudo-Property in the inspector
            new() { RenderLayer = RenderLayer.BASE, GameObject = new Sid { Name = "Diamond Sid", Position = new(100, 100) } },
            new() { RenderLayer = RenderLayer.BASE, GameObject = new Sid { Name = "Squared Sid", Position = new(10, 10) } },
        ];
    }

    private void Logs_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        int MAX_LOGS = 10_000;
        if (e.NewItems is not null)
        {
            while (Logs.Count > MAX_LOGS)
            {
                Logs.RemoveAt(Logs.Count - 1);
            }
        }
    }
}
