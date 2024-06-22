using System.Collections;

using NEngine.Scheduling.Coroutines;
using NEngine.GameObjects;

namespace NEngine.Window;

/// <summary>
/// A GameObject container that handles creation and cleanup of GameObjects into the GameWindow
/// </summary>
public class Scene
{
    private readonly string name;
    public string Name => name;

    public delegate void AddCallback(params (RenderLayer renderLayer, GameObject gameObject)[] layeredGameObjects);

    private readonly Action<AddCallback> initFunction;
    private CoroutineScheduler coroutineScheduler = new CoroutineScheduler();
    private List<GameObject> persistRefs = [];
    private SortedDictionary<RenderLayer, List<GameObject>> gameObjects = new(Comparer<RenderLayer>.Create((l, r) => l - r));
    public SortedDictionary<RenderLayer, List<GameObject>> GameObjects => gameObjects;

    public Scene(string name, Action<AddCallback> initFunction)
    {
        this.name = name;
        this.initFunction = initFunction;
    }

    /// <summary>
    /// Initializes the scene with the initFunction provided, giving it the "Add" method.
    /// Adds all objects passed to the callback function to the gameObjects List.
    /// This allows the scene to be recreated in the state the objects were originally constructed in.
    /// example usage:
    /// new Scene("some scene", (add) =>
    /// {
    ///     add((RenderLayerEnumValue, new SomeGameObjectDerivedClass));
    /// });
    /// </summary>
    public void Init(List<(RenderLayer renderLayer, GameObject gameObject)> persistentGameObjects)
    {
        AddPersistent(persistentGameObjects);
        initFunction(Add);
    }

    public bool Contains(RenderLayer renderLayer, GameObject gameObject)
    {
        return GameObjects.ContainsKey(renderLayer) && GameObjects[renderLayer].Contains(gameObject);
    }
    public bool Contains(GameObject gameObject)
        => GameObjects.Keys.Any(renderLayer => Contains(renderLayer, gameObject));

    private void AddPersistent(List<(RenderLayer renderLayer, GameObject gameObject)> persistentGameObjects)
    {
        foreach ((RenderLayer renderLayer, GameObject gameObject) in persistentGameObjects)
        {
            if (GameObjects.TryGetValue(renderLayer, out List<GameObject>? value))
            {
                value.Add(gameObject);
            }
            else
            {
                GameObjects[renderLayer] = [gameObject];
            }
        }
    }

    public void Add(RenderLayer renderLayer, GameObject gameObject)
    {
        // handle home of Persistent Game Objects
        if (gameObject.PersistanceInfo.persistOnSceneTransition)
        {
            if (persistRefs.Where(go => go.PersistanceInfo.persistId == gameObject.PersistanceInfo.persistId).Any())
            {
                // no need to continue adding
                return;
            }
            else
            {
                // add for next load and proceed to add as normal
                persistRefs.Add(gameObject);
            }
        }
        if (Contains(gameObject))
        {
            throw new InvalidOperationException("GameObject was already added to window!");
        }
        if (GameObjects.TryGetValue(renderLayer, out List<GameObject>? value))
        {
            value.Add(gameObject);
        }
        else
        {
            GameObjects[renderLayer] = [gameObject];
        }
        Application.Instance.AttachQueue.Enqueue(gameObject);
    }
    public void Add(IEnumerable<(RenderLayer renderLayer, GameObject gameObject)> layeredGameObjects)
    {
        foreach (var (renderLayer, gameObject) in layeredGameObjects)
        {
            Add(renderLayer, gameObject);
        }
    }

    public List<T> FindObjectsOfType<T>() where T : GameObject
    {
        List<T> result = [];
        foreach (var gameObject in GameObjects.Keys.SelectMany(key => GameObjects[key]))
        {
            if (gameObject is T t)
            {
                result.Add(t);
            }
        }
        return result;
    }

    public T? FindObjectOfType<T>(RenderLayer renderLayer) where T : GameObject
    {
        foreach (var gameObject in GameObjects[renderLayer])
        {
            if (gameObject is T t)
            {
                return t;
            }
        }
        return null;
    }

    public T? FindObjectOfType<T>(string? name = null) where T : GameObject
    {
        foreach (var gameObject in GameObjects.Keys.SelectMany(key => GameObjects[key]))
        {
            if (gameObject is T t && (name == null || name == t.Name))
            {
                return t;
            }
        }
        return null;
    }

    public GameObject? FindObject(string name)
    {
        foreach (var gameObject in GameObjects.Keys.SelectMany(key => GameObjects[key]))
        {
            if (gameObject.Name == name)
            {
                return gameObject;
            }
        }
        return null;
    }

    // Call the Coroutine Scheduler to remove all Coroutines associated with the GameObject
    public bool TryRemove(RenderLayer renderLayer, GameObject gameObject) 
    {
        if (GameObjects.TryGetValue(renderLayer, out List<GameObject>? value))
        {
            gameObject.OnDestroy();
            return value.Remove(gameObject);
        }
        return false;
    }
    public bool TryRemove(GameObject gameObject)
    {
        foreach (RenderLayer key in GameObjects.Keys)
        {
            if (TryRemove(key, gameObject))
            {
                gameObject.OnDestroy();
                return true;
            }
        }
        return false;
    }

    public Coroutine? StartCoroutine(GameObject gameObject, IEnumerator routine)
    {
        if (gameObjects.Keys.SelectMany(key => gameObjects[key]).Contains(gameObject))
        {
            return coroutineScheduler.StartCoroutine(gameObject, routine);
        }
        return null;
    }

    public bool StopCoroutine(GameObject gameObject, Coroutine coroutine)
    {
        if (gameObjects.Keys.SelectMany(key => gameObjects[key]).Contains(gameObject))
        {
            return coroutineScheduler.StopCoroutine(gameObject, coroutine);
        }
        return false;
    }

    public void StopAllCoroutines(GameObject gameObject)
    {
        if (gameObjects.Keys.SelectMany(key => gameObjects[key]).Contains(gameObject))
        {
            coroutineScheduler.StopAllCoroutines(gameObject);
        }
    }

    public void UpdateCoroutines()
    {
        coroutineScheduler.Update();
    }

    /// <summary>
    /// Destroys all non-persistent GameObjects in the Scene
    /// </summary>
    /// <returns>All persistent GameObjects in the Scene to pass forward</returns>
    public List<(RenderLayer renderLayer, GameObject gameObject)> Unload()
    {
        List<(RenderLayer renderLayer, GameObject gameObject)> passForward = [];
        foreach (RenderLayer key in GameObjects.Keys)
        {
            foreach (GameObject gameObject in GameObjects[key])
            {
                if (gameObject.PersistanceInfo.persistOnSceneTransition)
                {
                    passForward.Add((key, gameObject));
                }
                else
                {
                    gameObject.OnDestroy();
                }
            }
        }
        gameObjects.Clear();
        return passForward;
    }
}
