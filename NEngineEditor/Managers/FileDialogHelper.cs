using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NEngineEditor.Managers;

public partial class FileDialogHelper
{
#if WINDOWS
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct SHELLEXECUTEINFO
    {
        public int cbSize;
        public uint fMask;
        public IntPtr hwnd;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpVerb;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpFile;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpParameters;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpDirectory;
        public int nShow;
        public IntPtr hInstApp;
        public IntPtr lpIDList;
        [MarshalAs(UnmanagedType.LPTStr)]
        public string lpClass;
        public IntPtr hkeyClass;
        public uint dwHotKey;
        public IntPtr hIcon;
        public IntPtr hProcess;
    }

    public const uint SEE_MASK_INVOKEIDLIST = 12;
    public const int SW_SHOW = 5;
#endif

    public static void ShowOpenWithDialog(string filePath)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                SHELLEXECUTEINFO sei = new SHELLEXECUTEINFO();
                sei.cbSize = Marshal.SizeOf(sei);
                sei.lpVerb = "openas";
                sei.lpFile = filePath;
                sei.nShow = SW_SHOW;
                sei.fMask = SEE_MASK_INVOKEIDLIST;

                if (!ShellExecuteEx(ref sei))
                {
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                }
            }
            else if (OperatingSystem.IsLinux())
            {
                try
                {
                    using var _ = Process.Start("xdg-open", $"\"{filePath}\"");
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Failed to open file: {ex.Message}");
                }
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"An unexpected error occurred when loading the file: {filePath}");
        }
    }
}
