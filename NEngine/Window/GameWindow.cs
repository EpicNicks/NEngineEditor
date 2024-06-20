using System.Collections;

using SFML.Graphics;
using SFML.System;
using SFML.Window;

using NEngine.GameObjects;
using NEngine.Scheduling.Coroutines;

namespace NEngine.Window;

public class GameWindow
{
    /// <summary>
    /// Used by GameObjects not written to the UI or NONE RenderLayers.
    /// Move around to move the "main camera".
    /// </summary>
    public View MainView { get; private set; }
    /// <summary>
    /// Used by GameObjects written to the UI RenderLayer.
    /// Not really meant to be moved about like MainView but can be.
    /// </summary>
    public View UiView { get; private set; }

    /// <summary>
    /// The underlying SFML.NET RenderWindow object
    /// </summary>
    public RenderWindow RenderWindow { get; private set; }
    /// <summary>
    /// The Color that is set when the window is cleared
    /// </summary>
    public static Color WindowBackgroundColor { get; set; } = CORNFLOWER_BLUE;
    private static readonly Color CORNFLOWER_BLUE = new(147, 204, 234);

    private readonly Clock deltaClock = new();
    private readonly Clock timeClock = new();
    private readonly CollisionSystem collisionSystem = new();
    private SortedDictionary<RenderLayer, List<GameObject>> GameObjects
    {
        // TODO: handle scene loading next
        get => LoadedScene?.GameObjects ?? [];
    }

    private List<Scene> sceneList = [];
    private int curSceneIndex = 0;
    // TODO - Unhandled: Initialization when there are no scenes in the list
    // same goes for Add, TryRemove, and all others
    private Scene? LoadedScene => curSceneIndex < sceneList.Count ? sceneList[curSceneIndex] : null;

    public Queue<GameObject> AttachQueue { get; private set; } = [];

    private static string windowTitle;
    public static string WindowTitle
    {
        get => windowTitle;
        set => Instance.RenderWindow.SetTitle(windowTitle = value);
    }
    /// <summary>
    /// A shortener for the common Instance.RenderWindow.Size get
    /// </summary>
    public static Vector2u Size { get => Instance.RenderWindow.Size; set => Instance.RenderWindow.Size = value; }
    /// <summary>
    /// The Aspect Ratio of the window (Width / Height)
    /// </summary>
    public static float AspectRatio => Size.X / Size.Y;
    /// <summary>
    /// The time elapsed since the previous frame was drawn
    /// </summary>
    public static Time DeltaTime { get; private set; } = default;

    /// <summary>
    /// The time elapsed since GameWindow.Run() has been called
    /// </summary>
    public static Time Time => Instance.timeClock.ElapsedTime;

    private static GameWindow? instance;
    public static GameWindow Instance => instance ??= new GameWindow();

    static GameWindow()
    {
        windowTitle = "My Window";
    }

    private GameWindow()
    {
        (uint width, uint height) = (1200, 800);
        RenderWindow = new RenderWindow(new VideoMode(width, height), windowTitle);
        MainView = new View(new FloatRect(0, 0, width, height))
        {
            Viewport = new FloatRect(0, 0, 1, 1)
        };
        UiView = RenderWindow.DefaultView;
    }

    public static bool Contains(RenderLayer renderLayer, GameObject gameObject) => Instance.LoadedScene?.Contains(renderLayer, gameObject) ?? false;
    public static bool Contains(GameObject gameObject) => Instance.LoadedScene?.Contains(gameObject) ?? false;
    public static void Add(RenderLayer renderLayer, GameObject gameObject)
    {
        if (Instance.LoadedScene == null)
        {
            Console.Error.WriteLine("No scene was loaded to add the provided GameObject to");
        }
        Instance.LoadedScene?.Add(renderLayer, gameObject);
    }
    public static void Add(List<(RenderLayer renderLayer, GameObject gameObject)> layeredGameObjects)
    {
        if (Instance.LoadedScene == null)
        {
            Console.Error.WriteLine("No scene was loaded to add the provided GameObject to");
        }
        Instance.LoadedScene?.Add(layeredGameObjects);
    }
    public static List<T> FindObjectsOfType<T>() where T : GameObject 
        => Instance.LoadedScene?.FindObjectsOfType<T>() ?? [];
    public static T? FindObjectOfType<T>(string? name = null) where T : GameObject 
        => Instance.LoadedScene?.FindObjectOfType<T>(name);
    public static T? FindObjectOfType<T>(RenderLayer renderLayer) where T : GameObject
        => Instance.LoadedScene?.FindObjectOfType<T>(renderLayer);
    public static GameObject? FindObject(string name) => Instance.LoadedScene?.FindObject(name);
    public static bool TryRemove(RenderLayer renderLayer, GameObject gameObject) => Instance.LoadedScene?.TryRemove(renderLayer, gameObject) ?? false;
    public static bool TryRemove(GameObject gameObject) => Instance.LoadedScene?.TryRemove(gameObject) ?? false;

    public static string? LoadedSceneName => Instance.LoadedScene?.Name;

    public static void AddScene(Scene scene)
    {
        if (Instance.sceneList.Any(s => s.Name == scene.Name)) // Contains(scene) case logically covered here
        {
            throw new InvalidOperationException($"Scene with name: {scene} was already in the list");
        }
        Instance.sceneList.Add(scene);
    }

    public static void LoadNextScene()
    {
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
        int namedSceneIndex = Instance.sceneList.FindIndex(scene => scene.Name == name);
        if (namedSceneIndex == -1)
        {
            throw new InvalidOperationException($"No scene was found in the Scene List with the name: {name}");
        }
        var persistentGameObjects = Instance.LoadedScene?.Unload() ?? [];
        Instance.curSceneIndex = namedSceneIndex;
        Instance.LoadedScene?.Init(persistentGameObjects);
    }

    public static bool HasNextScene() => Instance.curSceneIndex + 1 < Instance.sceneList.Count;

    public static Coroutine? StartCoroutine(GameObject gameObject, IEnumerator routine)
    {
        return Instance.LoadedScene?.StartCoroutine(gameObject, routine);
    }

    public static bool StopCoroutine(GameObject gameObject, Coroutine coroutine)
    {
        return Instance.LoadedScene?.StopCoroutine(gameObject, coroutine) ?? false;
    }

    public static void StopAllCoroutines(GameObject gameObject)
    {
        Instance.LoadedScene?.StopAllCoroutines(gameObject);
    }

    public static void Run()
    {
        Init();
        while (Instance.RenderWindow != null && Instance.RenderWindow.IsOpen)
        {
            DeltaTime = Instance.deltaClock.Restart();
            ProcessAttachQueue();
            Update();
            Instance.collisionSystem.HandleCollisions(ActiveGameObjects);
            Render();
        }
    }

    public static void Quit()
    {
        HandleQuit();
    }
    private static void Init()
    {
        Instance.RenderWindow.SetFramerateLimit(120);
        Instance.timeClock.Restart();
        InitStandardEvents();
        if (Instance.LoadedScene is null)
        {
            Console.Error.WriteLine("No initial scene to load provided to GameWindow. Consider calling AddScene with a Scene to load.");
        }
        else
        {
            Instance.LoadedScene.Init([]);
        }
    }
    private static void InitStandardEvents()
    {
        // window click close
        Instance.RenderWindow.Closed += (sender, eventArgs) =>
        {
            HandleQuit();
        };
        Instance.RenderWindow.KeyPressed += (sender, keyEvent) =>
        {
            if (keyEvent.Code == Keyboard.Key.Escape)
            {
                HandleQuit();
            }
        };
        Instance.RenderWindow.Resized += (sender, sizeEvent) =>
        {
            // Update the viewport of the main view to maintain aspect ratio or full scale
            Instance.MainView.Size = new Vector2f(sizeEvent.Width, sizeEvent.Height);  // Optional: maintain aspect ratio
            Instance.MainView.Viewport = new FloatRect(0, 0, 1, 1);  // Always render the world view across the entire window

            // Update the UI view to match the new window size for direct screen space mapping
            Instance.UiView.Reset(new FloatRect(0, 0, sizeEvent.Width, sizeEvent.Height));

            // Update the window's view to the modified main view after resizing
            Instance.RenderWindow.SetView(Instance.MainView);
        };
    }

    private static void ProcessAttachQueue()
    {
        while (Instance.AttachQueue.Count > 0)
        {
            GameObject dequeuedGameObject = Instance.AttachQueue.Dequeue();
            if (dequeuedGameObject.IsActive)
            {
                dequeuedGameObject.Attach();
            }
        }
    }

    private static void Update()
    {
        Instance.RenderWindow.DispatchEvents();
        Instance.LoadedScene?.UpdateCoroutines();
        OnEachGameObject((gameObject) => gameObject.Update());
    }

    private static void Render()
    {
        Instance.RenderWindow.Clear(WindowBackgroundColor);
        OnEachGameObject((renderLayer, gameObject) => gameObject.Drawables.ForEach(drawable =>
        {
            if (renderLayer == RenderLayer.NONE)
            {
                return;
            }
            else if (renderLayer != RenderLayer.UI)
            {
                // convert screen space to world space on non-UI objects?
                Instance.RenderWindow.SetView(Instance.MainView);
            }
            else
            {
                Instance.RenderWindow.SetView(Instance.UiView);
            }
            Instance.RenderWindow.Draw(drawable);
        }));
        Instance.RenderWindow.Display();
    }

    private static List<GameObject> ActiveGameObjects => Instance.GameObjects.Keys.SelectMany(key => Instance.GameObjects[key]).Where(gameObject => gameObject.IsActive).ToList();
    private static List<(RenderLayer renderLayer, GameObject gameObject)> ActiveLayeredGameObjects => 
        Instance.GameObjects.Keys
            .SelectMany(key => 
                Instance.GameObjects[key]
                    .Where(gameObject => gameObject.IsActive)
                    .Select(gameObject => (key, gameObject))
            ).ToList();

    // layers should be iterated over in the correct order due to the SortedDictionary calling Render on lower layers first
    /// <summary>
    /// Calls doOnEach for each GameObject in Instance.gameObjects in order of keys 
    /// with respect to the sorting method given to Instance.gameObjects SortedDictionary.
    /// </summary>
    /// <param name="doOnEach">The callback function taking the GameObject to perform an action on</param>
    private static void OnEachGameObject(Action<GameObject> doOnEach)
    {
        ActiveGameObjects.ForEach(doOnEach);
    }

    private static void OnEachGameObject(Action<RenderLayer, GameObject> doOnEach)
    {
        ActiveLayeredGameObjects.ForEach(renderLayerGoTuple => doOnEach(renderLayerGoTuple.renderLayer, renderLayerGoTuple.gameObject));
    }

    private static Action<Action<GameObject>> OnEachGameObjectWhere(Func<GameObject, bool> predicate)
    {
        List<GameObject> gameObjects = ActiveGameObjects.Where(predicate).ToList();
        return gameObjects.ForEach;
    }

    private static void HandleQuit()
    {
        Console.WriteLine("closed window");
        Instance.RenderWindow.Close();
    }
}
