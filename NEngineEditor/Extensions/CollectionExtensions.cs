using System.Collections.ObjectModel;

namespace NEngineEditor.Extensions;
public static class CollectionExtensions
{
    /// <summary>
    /// Collection.IndexOf allowing a null parameter which would result in returning -1.
    /// </summary>
    /// <typeparam name="T">The type of item the Collection holds or null.</typeparam>
    /// <param name="collection">The collection to test.</param>
    /// <param name="item">The item to check against the collection</param>
    /// <returns></returns>
    public static int TryGetIndexOf<T>(this Collection<T> collection, T? item) where T : class
    {
        if (item is null)
        {
            return -1;
        }
        return collection.IndexOf(item);
    }
}
