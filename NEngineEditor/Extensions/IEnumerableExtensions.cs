namespace NEngineEditor.Extensions;
public static class IEnumerableExtensions
{
    public static void ForEach<T>(this IEnumerable<T> ts, Action<T> onEach)
    {
        foreach (T t in ts)
        {
            onEach(t);
        }
    }
}
