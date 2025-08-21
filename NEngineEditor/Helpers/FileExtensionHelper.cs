namespace NEngineEditor.Helpers;
public static class FileExtensionHelper
{
    public static bool IsScene(string fileName)
    {
        return EndsWithHelper(fileName, ".scene");
    }

    public static bool IsImage(string fileName)
    {
        return EndsWithHelper(fileName, [ ".png", ".bmp", ".tga", ".jpg", ".gif", ".psd", ".hdr", ".pic" ]);
    }

    public static bool IsScript(string fileName)
    {
        return EndsWithHelper(fileName, ".cs");
    }

    private static bool EndsWithHelper(string fileName, string extension)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(extension))
        {
            return false;
        }
        return fileName.EndsWith(extension);
    }

    private static bool EndsWithHelper(string fileName, IEnumerable<string> extensions)
    {
        if (!extensions.Any() || string.IsNullOrEmpty(fileName))
        {
            return false;
        }
        foreach (string extension in extensions)
        {
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }
            if (fileName.EndsWith(extension))
            {
                return true;
            }
        }
        return false;
    }
}
