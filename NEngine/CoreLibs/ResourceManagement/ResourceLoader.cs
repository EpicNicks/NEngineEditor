namespace NEngine.CoreLibs.ResourceManagement;
public static class ResourceLoader
{
    private static readonly string _defaultBaseDirectory = AppContext.BaseDirectory;

    public static string BaseDirectory { get; set; } = _defaultBaseDirectory;

    public static void ResetBaseDirectory()
    {
        BaseDirectory = _defaultBaseDirectory;
    }

    public static byte[] LoadBytes(string fileName)
    {
        fileName = fileName.TrimStart('\\', '/');
        return File.ReadAllBytes(Path.Combine(BaseDirectory, fileName));
    }

    public static string ResolveAssetPath(string fileName)
    {
        fileName = fileName.TrimStart('\\', '/');
        return Path.Combine(BaseDirectory, fileName);
    }

    public static byte[] Find(string filePath, SearchOption searchOption = SearchOption.AllDirectories)
    {
        string[] files = Directory.GetFiles(BaseDirectory, filePath, searchOption);
        if (files.Length == 0)
            throw new FileNotFoundException($"Resource '{filePath}' not found in {BaseDirectory} searched with {searchOption}");

        string path = files.First();

        return File.ReadAllBytes(path);
    }
}
