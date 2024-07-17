using System.Collections;

using SFML.System;
using SFML.Window;
using SFML.Graphics;

using NEngine.GameObjects;
using NEngine.Scheduling.Coroutines;
using NEngine.Window;

namespace NEngine;
public class Application
{
    private const string INVALID_APPLICATION_OPERATION = "";
    private const string DEFAULT_APPLICATION_TITLE = "Default Application Title";

    private List<Scene> sceneList = [];
    private int curSceneIndex = 0;
    // TODO - Unhandled: Initialization when there are no scenes in the list
    // same goes for Add, TryRemove, and all others
    private Scene? LoadedScene => curSceneIndex < sceneList.Count ? sceneList[curSceneIndex] : null;
    private SortedDictionary<RenderLayer, List<GameObject>> GameObjects
    {
        get => LoadedScene?.GameObjects ?? [];
    }
    public Queue<GameObject> AttachQueue { get; private set; } = [];
    private readonly CollisionSystem collisionSystem = new();
    public GameWindow GameWindow { get; private set; }

    private readonly Clock deltaClock = new();
    private readonly Clock timeClock = new();

    /// <summary>
    /// The time elapsed since the previous frame was drawn
    /// </summary>
    public static Time DeltaTime { get; private set; } = default;

    /// <summary>
    /// The time elapsed since GameWindow.Run() has been called
    /// </summary>
    public static Time Time => Instance?.timeClock.ElapsedTime ?? throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);

    private static Application? instance;
    public static Application? Instance => instance;

    public Application(RenderWindow renderWindow, string windowTitle)
    {
        instance = this;
        GameWindow = new GameWindow(renderWindow);
        lastSetWindowTitle = windowTitle;
    }
    public Application(RenderWindow renderWindow) : this(renderWindow, DEFAULT_APPLICATION_TITLE) { }
    public Application() : this(new RenderWindow(new VideoMode(1200, 800), DEFAULT_APPLICATION_TITLE), DEFAULT_APPLICATION_TITLE) { }

    #region Window pass-along methods
    private static string lastSetWindowTitle = "";
    public static string WindowTitle
    {
        get => lastSetWindowTitle;
        set
        {
            if (Instance is null)
            {
                throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
            }
            Instance.GameWindow.RenderWindow.SetTitle(value);
            lastSetWindowTitle = value;
        }
    }
    /// <summary>
    /// A shortener for the common Instance.RenderWindow.Size get
    /// </summary>
    public static Vector2u WindowSize 
    { 
        get => Instance?.GameWindow.RenderWindow.Size ?? throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        set
        {
            if (Instance is not null) Instance.GameWindow.RenderWindow.Size = value;
        }
    }
    /// <summary>
    /// The Aspect Ratio of the window (Width / Height)
    /// </summary>
    public static float AspectRatio => WindowSize.X / WindowSize.Y;
    #endregion

    #region Scene API pass-along methods
    public static bool Contains(RenderLayer renderLayer, GameObject gameObject) => Instance?.LoadedScene?.Contains(renderLayer, gameObject) ?? false;
    public static bool Contains(GameObject gameObject) => Instance?.LoadedScene?.Contains(gameObject) ?? false;
    public static void Add(RenderLayer renderLayer, GameObject gameObject)
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        if ( Instance.LoadedScene == null)
        {
            Console.Error.WriteLine("No scene was loaded to add the provided GameObject to");
        }
        Instance.LoadedScene?.Add(renderLayer, gameObject);
    }
    public static void Add(List<(RenderLayer renderLayer, GameObject gameObject)> layeredGameObjects)
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        if (Instance.LoadedScene == null)
        {
            Console.Error.WriteLine("No scene was loaded to add the provided GameObject to");
        }
        Instance.LoadedScene?.Add(layeredGameObjects);
    }
    public static List<T> FindObjectsOfType<T>() where T : GameObject
        => Instance?.LoadedScene?.FindObjectsOfType<T>() ?? [];
    public static T? FindObjectOfType<T>(string? name = null) where T : GameObject
        => Instance?.LoadedScene?.FindObjectOfType<T>(name);
    public static T? FindObjectOfType<T>(RenderLayer renderLayer) where T : GameObject
        => Instance?.LoadedScene?.FindObjectOfType<T>(renderLayer);
    public static GameObject? FindObject(string name) => Instance?.LoadedScene?.FindObject(name);
    public static bool TryRemove(RenderLayer renderLayer, GameObject gameObject) => Instance?.LoadedScene?.TryRemove(renderLayer, gameObject) ?? false;
    public static bool TryRemove(GameObject gameObject) => Instance?.LoadedScene?.TryRemove(gameObject) ?? false;
    #endregion

    public static string? LoadedSceneName => Instance?.LoadedScene?.Name;

    public static void AddScene(Scene scene)
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        if (Instance.sceneList.Any(s => s.Name == scene.Name)) // Contains(scene) case logically covered here
        {
            throw new InvalidOperationException($"Scene with name: {scene} was already in the list");
        }
        Instance.sceneList.Add(scene);
    }

    public static void LoadNextScene()
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        if (Instance.LoadedScene is null)
        {
            throw new InvalidOperationException("There is no scene loaded currently of which the next should be loaded.");
        }
        if (!HasNextScene())
        {
            throw new InvalidOperationException("There was no next scene. Index is at or greater than Count of Scene List");
        }
        var persistentGameObjects = Instance.LoadedScene?.Unload() ?? [];
        Instance.curSceneIndex++;
        Instance.LoadedScene?.Init(persistentGameObjects);
    }

    public static void LoadScene(string name)
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        int namedSceneIndex = Instance.sceneList.FindIndex(scene => scene.Name == name);
        if (namedSceneIndex == -1)
        {
            throw new InvalidOperationException($"No scene was found in the Scene List with the name: {name}");
        }
        var persistentGameObjects = Instance.LoadedScene?.Unload() ?? [];
        Instance.curSceneIndex = namedSceneIndex;
        Instance.LoadedScene?.Init(persistentGameObjects);
    }

    public static bool HasNextScene()
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        return Instance.curSceneIndex + 1 < Instance.sceneList.Count;
    }

    public static Coroutine? StartCoroutine(GameObject gameObject, IEnumerator routine)
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        return Instance.LoadedScene?.StartCoroutine(gameObject, routine);
    }

    public static bool StopCoroutine(GameObject gameObject, Coroutine coroutine)
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        return Instance.LoadedScene?.StopCoroutine(gameObject, coroutine) ?? false;
    }

    public static void StopAllCoroutines(GameObject gameObject)
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        Instance.LoadedScene?.StopAllCoroutines(gameObject);
    }

    /// <summary>
    /// Call this to call standard setup methods and begin the Game Loop.
    /// </summary>
    public void Run()
    {
        Init();
        InitStandardEvents();
        InitLoadedScene();
        while (GameWindow.RenderWindow != null && GameWindow.RenderWindow.IsOpen)
        {
            DeltaTime = deltaClock.Restart();
            ProcessAttachQueue();
            GameWindow.RenderWindow.DispatchEvents();
            Update();
            collisionSystem.HandleCollisions(ActiveGameObjects);
            GameWindow.Render(ActiveLayeredGameObjects);
        }
    }

    public static void Quit()
    {
        HandleQuit();
    }
    private void Init()
    {
        GameWindow.RenderWindow.SetFramerateLimit(120);
        timeClock.Restart();
    }

    private void InitLoadedScene()
    {
        if (LoadedScene is null)
        {
            Console.Error.WriteLine("No initial scene to load provided to GameWindow. Consider calling AddScene with a Scene to load.");
        }
        else
        {
            LoadedScene.Init([]);
        }
    }

    private void InitStandardEvents()
    {
        GameWindow.InitStandardWindowEvents();
        // window click close
        GameWindow.RenderWindow.Closed += (sender, eventArgs) =>
        {
            HandleQuit();
        };
        GameWindow.RenderWindow.KeyPressed += (sender, keyEvent) =>
        {
            if (keyEvent.Code == Keyboard.Key.Escape)
            {
                HandleQuit();
            }
        };
    }

    private void ProcessAttachQueue()
    {
        while (AttachQueue.Count > 0)
        {
            GameObject dequeuedGameObject = AttachQueue.Dequeue();
            if (dequeuedGameObject.IsActive)
            {
                dequeuedGameObject.Attach();
            }
        }
    }

    private void Update()
    {
        LoadedScene?.UpdateCoroutines();
        OnEachGameObject((gameObject) => gameObject.Update());
    }

    private List<GameObject> ActiveGameObjects => GameObjects.Keys.SelectMany(key => GameObjects[key]).Where(gameObject => gameObject.IsActive).ToList();
    private List<(RenderLayer renderLayer, GameObject gameObject)> ActiveLayeredGameObjects =>
        GameObjects.Keys
            .SelectMany(key =>
                GameObjects[key]
                    .Where(gameObject => gameObject.IsActive)
                    .Select(gameObject => (key, gameObject))
            ).ToList();

    // layers should be iterated over in the correct order due to the SortedDictionary calling Render on lower layers first
    /// <summary>
    /// Calls doOnEach for each GameObject in Instance.gameObjects in order of keys 
    /// with respect to the sorting method given to Instance.gameObjects SortedDictionary.
    /// </summary>
    /// <param name="doOnEach">The callback function taking the GameObject to perform an action on</param>
    private void OnEachGameObject(Action<GameObject> doOnEach)
    {
        ActiveGameObjects.ForEach(doOnEach);
    }

    private static void HandleQuit()
    {
        if (Instance is null)
        {
            Console.Error.WriteLine("Application Instance was null somehow and its associated RenderWindow could not be closed!");
            return;
        }
        Console.WriteLine("Closing Window...");
        Instance.GameWindow.RenderWindow.Close();
        Console.WriteLine("Closed Window");
    }
}
