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
    private const string INVALID_APPLICATION_OPERATION = "Invalid Application Operation. Application has not been Initialized";
    private const string DEFAULT_APPLICATION_TITLE = "Default Application Title";

    private List<Scene> sceneList = [];
    private int curSceneIndex = 0;
    private Scene? LoadedScene => curSceneIndex < sceneList.Count ? sceneList[curSceneIndex] : null;
    private SortedDictionary<RenderLayer, List<GameObject>> GameObjects
    {
        get => LoadedScene?.GameObjects ?? [];
    }
    /// <summary>
    /// Not really to be used externally to the engine. 
    /// The GameObjects to process their Attach methods of for the next Game Loop.
    /// </summary>
    public Queue<GameObject> AttachQueue { get; private set; } = [];
    private readonly CollisionSystem collisionSystem = new();
    /// <summary>
    /// The GameWindow instance attached to the Application. 
    /// Access if you wish to operate on it directly.
    /// </summary>
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
    /// <summary>
    /// A reference to the most recently created Application instance or 
    /// null if one hasn't been created or if the most recently created Application instance has been destroyed.
    /// </summary>
    public static Application? Instance => instance;

    /// <summary>
    /// Instantiates a new Application instance with the provided RenderWindow and windowTitle.
    /// </summary>
    /// <param name="renderWindow">The RenderWindow for the Application to use.</param>
    /// <param name="windowTitle">The title for the Application to display.</param>
    public Application(RenderWindow renderWindow, string windowTitle)
    {
        instance = this;
        GameWindow = new GameWindow(renderWindow);
        lastSetWindowTitle = windowTitle;
    }
    /// <summary>
    /// Instantiates a new Application instance with the provided RenderWindow and default title <see cref="DEFAULT_APPLICATION_TITLE"/>.
    /// </summary>
    /// <param name="renderWindow">The RenderWindow for the Application to use.</param>
    public Application(RenderWindow renderWindow) : this(renderWindow, DEFAULT_APPLICATION_TITLE) { }
    /// <summary>
    /// Instantiates a new default Application instance with a RenderWindow with aspect ratio 1200x800 and title <see cref="DEFAULT_APPLICATION_TITLE"/>.
    /// </summary>
    public Application() : this(new RenderWindow(new VideoMode(1200, 800), DEFAULT_APPLICATION_TITLE), DEFAULT_APPLICATION_TITLE) { }

    #region Window pass-along methods
    private static string lastSetWindowTitle = "";
    /// <summary>
    /// Gets or sets the current window title.
    /// </summary>
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
    /// A shortener for the common Instance.RenderWindow.Size get.
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
    /// <summary>
    /// Checks if the currently loaded scene on the provided RenderLayer contains the provided GameObject instance.
    /// </summary>
    /// <param name="renderLayer">The RenderLayer to test on.</param>
    /// <param name="gameObject">The GameObject instance to test membership of.</param>
    /// <returns>true if the currently loaded scene on the provided RenderLayer contains the provided GameObject instance, false otherwise.</returns>
    public static bool Contains(RenderLayer renderLayer, GameObject gameObject) => Instance?.LoadedScene?.Contains(renderLayer, gameObject) ?? false;

    /// <summary>
    /// Checks if the currently loaded scene contains the provided GameObject instance.
    /// </summary>
    /// <param name="gameObject">The GameObject instance to test membership of.</param>
    /// <returns>true if the currently loaded scene contains the provided GameObject instance, false otherwise.</returns>
    public static bool Contains(GameObject gameObject) => Instance?.LoadedScene?.Contains(gameObject) ?? false;

    /// <summary>
    /// Adds the provided GameObject instance to the currently loaded scene on the provided RenderLayer.
    /// </summary>
    /// <param name="renderLayer">The RenderLayer the GameObject should be rendered on.</param>
    /// <param name="gameObject">The GameObject instance to add to the currently loaded scene.</param>
    /// <exception cref="InvalidOperationException">
    /// thrown if there is no currently loaded scene to add the GameObject to or if an Application instance does not currently exist.
    /// thrown if there is no currently loaded scene to add the GameObject to or if a valid Application instance does not currently exist.
    /// </exception>
    public static void Add(RenderLayer renderLayer, GameObject gameObject)
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        if (Instance.LoadedScene == null)
        {
            const string exceptionString = "No scene was loaded to add the provided GameObject to";
            Console.Error.WriteLine(exceptionString);
            throw new InvalidOperationException(exceptionString);
        }
        Instance.LoadedScene?.Add(renderLayer, gameObject);
    }

    /// <summary>
    /// Adds a list of (RenderLayer, GameObject) tuple to the currently loaded scene.
    /// </summary>
    /// <param name="layeredGameObjects">The (RenderLayer, GameObject) tuples to add to the currently loaded scene.</param>
    /// <exception cref="InvalidOperationException">
    /// thrown if there is no currently loaded scene to add the layered GameObjects to or if an Application instance does not currently exist.
    /// thrown if there is no currently loaded scene to add the layered GameObjects to or if a valid Application instance does not currently exist.
    /// </exception>
    public static void Add(List<(RenderLayer renderLayer, GameObject gameObject)> layeredGameObjects)
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        if (Instance.LoadedScene == null)
        {
            const string exceptionString = "No scene was loaded to add the provided GameObject to";
            Console.Error.WriteLine(exceptionString);
            throw new InvalidOperationException(exceptionString);
        }
        Instance.LoadedScene?.Add(layeredGameObjects);
    }
    /// <summary>
    /// Finds all GameObjects of the given type in the currently loaded scene.
    /// </summary>
    /// <typeparam name="T">A derived class of GameObject to search the active scene for.</typeparam>
    /// <returns>All GameObjects of the provided type.</returns>
    public static List<T> FindObjectsOfType<T>() where T : GameObject
        => Instance?.LoadedScene?.FindObjectsOfType<T>() ?? [];
    /// <summary>
    /// Finds the first GameObject in the currently loaded scene which matches the type and name provided or null if not found.
    /// Generally only use this without a provided name if you know there is exactly one derived GameObject matching the type you provided in the Scene.
    /// Otherwise, provide a name that is unique.
    /// </summary>
    /// <typeparam name="T">A derived class of GameObject to search the active scene for.</typeparam>
    /// <param name="name">The optional name to search for the GameObject with.</param>
    /// <returns>The first GameObject in the currently loaded scene which matches the type and name provided or null if not found.</returns>
    public static T? FindObjectOfType<T>(string? name = null) where T : GameObject
        => Instance?.LoadedScene?.FindObjectOfType<T>(name);

    /// <summary>
    /// Finds the first GameObject in the currently loaded scene on the given RenderLayer which matches the type and name provided or null if not found.
    /// </summary>
    /// <typeparam name="T">A derived class of GameObject to search the active scene for.</typeparam>
    /// <param name="renderLayer">The RenderLayer the GameObject you are searching for is on.</param>
    /// <param name="name">The optional name to search for the GameObject with.</param>
    /// <returns>The first GameObject in the currently loaded scene which matches the RenderLayer and name provided or null if not found.</returns>
    public static T? FindObjectOfType<T>(RenderLayer renderLayer, string? name = null) where T : GameObject
        => Instance?.LoadedScene?.FindObjectOfType<T>(renderLayer, name);

    /// <summary>
    /// Finds the first GameObject in the currently loaded scene which matches the name provided or null if not found.
    /// </summary>
    /// <param name="name">The optional name to search for the GameObject with.</param>
    /// <returns>The first GameObject in the currently loaded scene which matches the name provided or null if not found.</returns>
    public static GameObject? FindObject(string name) => Instance?.LoadedScene?.FindObject(name);
    /// <summary>
    /// Attempts to remove the provided GameObject instance from the currently loaded scene on the provided RenderLayer.
    /// </summary>
    /// <param name="renderLayer">The RenderLayer to remove the GameObject from.</param>
    /// <param name="gameObject">The GameObject instance to remove.</param>
    /// <returns>true if the GameObject was successfully removed from the currently loaded scene on the provided RenderLayer, false otherwise.</returns>
    public static bool TryRemove(RenderLayer renderLayer, GameObject gameObject) => Instance?.LoadedScene?.TryRemove(renderLayer, gameObject) ?? false;
    /// <summary>
    /// Attempts to remove the provided GameObject instance from the currently loaded scene.
    /// </summary>
    /// <param name="gameObject">The GameObject instance to remove.</param>
    /// <returns>true if the GameObject was successfully removed from the currently loaded scene, false otherwise.</returns>
    public static bool TryRemove(GameObject gameObject) => Instance?.LoadedScene?.TryRemove(gameObject) ?? false;
    #endregion

    /// <summary>
    /// The name of the currently loaded scene.
    /// </summary>
    public static string? LoadedSceneName => Instance?.LoadedScene?.Name;

    /// <summary>
    /// Adds a Scene instance with a name not currently in the scene list to the current Scene List at the last index.
    /// Generally loaded by the editor but may be used to build scenes in code.
    /// </summary>
    /// <param name="scene">The Scene instance to add.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if there exists a scene with the same name or if an Application instance does not currently exist.
    /// Thrown if there exists a scene with the same name or if a valid Application instance does not currently exist.
    /// </exception>
    public static void AddScene(Scene scene)
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        if (Instance.sceneList.Any(s => s.Name == scene.Name)) // Contains(scene) case logically covered here
        {
            throw new InvalidOperationException($"Scene with name: {scene} was already in the list");
        }
        Instance.sceneList.Add(scene);
    }

    /// <summary>
    /// Loads the next scene as given in the selected build order.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if there is no active scene loaded or next scene to load or if an Application instance does not currently exist.
    /// Thrown if there is no active scene loaded or next scene to load or if a valid Application instance does not currently exist.
    /// </exception>
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

    /// <summary>
    /// Loads a scene from the selected build order by name (Case Sensitive).
    /// </summary>
    /// <param name="name">The name (Case Sensitive) of the scene to load.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if there is no Scene by the provided name in the selected build order or an Application instance does not currently exist.
    /// Thrown if there is no Scene by the provided name in the selected build order or a valid Application instance does not currently exist.
    /// </exception>
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

    /// <summary>
    /// Checks if there exists a next scene to load as per the selected build order.
    /// </summary>
    /// <returns>True if there is a next scene after the currently loaded one as per the build order, false otherwise</returns>
    /// <exception cref="InvalidOperationException">Thrown if an Application instance does not currently exist.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a valid Application instance does not currently exist.
    /// </exception>
    public static bool HasNextScene()
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        return Instance.curSceneIndex + 1 < Instance.sceneList.Count;
    }

    /// <summary>
    /// Creates and returns a Coroutine from the provided IEnumerator. 
    /// The Coroutine is associated with the provided GameObject, passed to the CoroutineManager, and started on the next available Update.
    /// </summary>
    /// <param name="gameObject">The GameObject to associate the Coroutine with.</param>
    /// <param name="routine">The IEnumerator to create the Coroutine from.</param>
    /// <returns>The created Coroutine from the provided IEnumerator.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a valid Application instance does not currently exist.
    /// </exception>
    public static Coroutine? StartCoroutine(GameObject gameObject, IEnumerator routine)
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        return Instance.LoadedScene?.StartCoroutine(gameObject, routine);
    }

    /// <summary>
    /// Stops the passed Coroutine on the passed GameObject.
    /// The passed Coroutine associated with the passed GameObject will no longer be processed by the CoroutineManager starting on the next available Update.
    /// </summary>
    /// <param name="gameObject">The GameObject the Coroutine is associated with.</param>
    /// <param name="coroutine">The Coroutine to stop.</param>
    /// <returns>true if the Coroutine was successfully stopped, false otherwise.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a valid Application instance does not currently exist.
    /// </exception>
    public static bool StopCoroutine(GameObject gameObject, Coroutine coroutine)
    {
        if (Instance is null) throw new InvalidOperationException(INVALID_APPLICATION_OPERATION);
        return Instance.LoadedScene?.StopCoroutine(gameObject, coroutine) ?? false;
    }

    /// <summary>
    /// Stops ALL Coroutines associated with the passed GameObject.
    /// All Coroutines associated with the passed GameObject will no longer be processed by the CoroutineManager starting on the next available Update.
    /// </summary>
    /// <param name="gameObject">The GameObject to stop all associated Coroutines of.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a valid Application instance does not currently exist.
    /// </exception>
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

    /// <summary>
    /// Tells the Application to close the Game Window.
    /// </summary>
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
