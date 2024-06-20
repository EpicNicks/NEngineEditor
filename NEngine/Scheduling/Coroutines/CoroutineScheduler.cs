using NEngine.GameObjects;
using System.Collections;

namespace NEngine.Scheduling.Coroutines;
/// <summary>
/// Simple Dictionary-based scheduler.
/// You probably don't have enough concurrently scheduled Coroutines to warrant a data structure with more overhead but better big O time complexity for large N.
/// </summary>
public class CoroutineScheduler
{
    private Dictionary<GameObject, List<Coroutine>> Coroutines { get; set; } = [];

    /// <summary>
    /// Called by the GameWindow/Application on each frame
    /// </summary>
    public void Update()
    {
        List<(GameObject, Coroutine)> coroutinesToRemove = [];
        var gameObjects = Coroutines.Keys.ToList();

        foreach (GameObject gameObject in gameObjects)
        {
            foreach (Coroutine coroutine in Coroutines[gameObject].ToList())
            {
                bool isComplete = AdvanceCoroutine(coroutine);
                if (isComplete)
                {
                    coroutinesToRemove.Add((gameObject, coroutine));
                }
            }
        }
        CleanupCompleted(coroutinesToRemove);
    }

    /// <summary>
    /// Starts a Coroutine from an IEnumerator on the specified GameObject.
    /// </summary>
    /// <param name="gameObject">The GameObject to attach the Coroutine to</param>
    /// <param name="routine">The IEnumerator routine to start as a Coroutine</param>
    /// <returns>The created Coroutine object the caller may store to reference it in StopCoroutine</returns>
    public Coroutine StartCoroutine(GameObject gameObject, IEnumerator routine)
    {
        Coroutine coroutine = new Coroutine(routine);
        if (Coroutines.TryGetValue(gameObject, out List<Coroutine>? value))
        {
            value.Add(coroutine);
        }
        else
        {
            Coroutines[gameObject] = [coroutine];
        }
        return coroutine;
    }

    /// <summary>
    /// Stops the running Coroutine by removing it from the CoroutineScheduler's Update loop.
    /// </summary>
    /// <param name="gameObject">The GameObject the Coroutine is attached to</param>
    /// <param name="coroutine">The Coroutine to stop</param>
    /// <returns>true if the Couroutine was successfully removed from the CoroutineScheduler, false otherwise</returns>
    public bool StopCoroutine(GameObject gameObject, Coroutine coroutine)
    {
        if (Coroutines.TryGetValue(gameObject, out List<Coroutine>? value))
        {
            return value.Remove(coroutine);
        }
        return false;
    }

    /// <summary>
    /// Stops all running Coroutines on the specified GameObject
    /// </summary>
    /// <param name="gameObject">The GameObject to stop all executing Coroutines on</param>
    public void StopAllCoroutines(GameObject gameObject)
    {
        if (Coroutines.TryGetValue(gameObject, out List<Coroutine>? value))
        {
            value.Clear();
        }
    }

    /// <summary>
    /// Removes the passed GameObject from the CoroutineScheduler.
    /// This stops all running Coroutines on it.
    /// </summary>
    /// <param name="gameObject">The GameObject to remove</param>
    /// <returns>true if the GameObject was successfully removed, false otherwise</returns>
    public bool RemoveGameObject(GameObject gameObject)
    {
        return Coroutines.Remove(gameObject);
    }

    /// <summary>
    /// Advances the passed Coroutine
    /// </summary>
    /// <param name="coroutine">The Coroutine to advance</param>
    /// <returns>true if the coroutine has completed execution, false otherwise</returns>
    private static bool AdvanceCoroutine(Coroutine coroutine)
    {
        return !coroutine.MoveNext();
    }

    private void CleanupCompleted(List<(GameObject, Coroutine)> coroutinesToRemove)
    {
        foreach ((GameObject gameObject, Coroutine coroutine) in coroutinesToRemove)
        {
            Coroutines[gameObject].Remove(coroutine);
            if (Coroutines[gameObject].Count == 0)
            {
                RemoveGameObject(gameObject);
            }
        }
    }
}
