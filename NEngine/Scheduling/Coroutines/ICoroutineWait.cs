using NEngine.Window;

namespace NEngine.Scheduling.Coroutines;
/// <summary>
/// The base interface for all Coroutines's IEnumerator's current yield.
/// Provides bool Wait() to tell the Coroutine Scheduler how it should wait based on the pattern-matched instance returned.s
/// </summary>
public interface ICoroutineWait
{
    /// <summary>
    /// To be called once per frame in Update
    /// </summary>
    /// <returns>true if the caller should wait, false if the caller should continue</returns>
    bool Wait();
}

/// <summary>
/// Used in IEnumerators wrapped by Coroutines to tell it to delay execution to the next frame.
/// Equivalent to any yield return that is not a ICoroutineWait derived yield return but with more explicit intent.
/// (This is true for Unity's Coroutine system too even though Unity only specifies null as being equivalent in its docs)
/// </summary>
public class WaitForNextFrame : ICoroutineWait
{
    public bool Wait() => false;
}

/// <summary>
/// Used in IEnumerators wrapped by Coroutines to tell it to delay execution to nth frame after the current frame.
/// Unsure of what the usecase of this would be given a dynamic framerate aside from directly interacting with the framerate itself.
/// </summary>
public class WaitForFrames(uint waitFrames) : ICoroutineWait
{
    private readonly uint waitFrames = waitFrames;
    private uint waitCalledCount = 0;

    public bool Wait()
    {
        if (waitCalledCount < waitFrames - 1)
        {
            waitCalledCount++;
            return true;
        }
        return false;
    }
}

/// <summary>
/// Used in IEnumerators wrapped by Coroutines to tell it to delay execution until waitSeconds has elapsed.
/// </summary>
/// <param name="waitSeconds">The number of seconds to wait</param>
public class WaitForSeconds(float waitSeconds) : ICoroutineWait
{
    private readonly float waitSeconds = waitSeconds;
    private float elapsedSeconds = 0f;

    public bool Wait()
    {
        elapsedSeconds += GameWindow.DeltaTime.AsSeconds();
        if (elapsedSeconds < waitSeconds)
        {
            return true;
        }
        return false;
    }
}

/// <summary>
/// Used in IEnumerators wrapped by Coroutines to tell it to delay forever, 
///     effectively running the Coroutine until it has been explicitly stopped in the Scheduler (GameObject.StopCoroutine).
/// </summary>
public class WaitForever : ICoroutineWait
{
    public bool Wait() => true;
}
