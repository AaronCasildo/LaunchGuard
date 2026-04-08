using Microsoft.Win32;

public static class StartupManager
{
    private const string AppName = "LaunchGuard";  
    private const string Runkey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

    public static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(Runkey);
        return key?.GetValue(AppName) != null;
    }

    public static void SetStartup(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(Runkey, writable: true);
        if (enable)
            key?.SetValue(AppName, Application.ExecutablePath);
        else
            key?.DeleteValue(AppName, throwOnMissingValue:false);
    }
}