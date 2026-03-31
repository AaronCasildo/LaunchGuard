using System.Text.Json;

namespace LaunchGuard;
/* This class is used to store the application configuration, 
 such as the list of locked processes and their associated passwords.*/
internal static class AppConfig
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static readonly string ConfigPath =
        Path.Combine(AppContext.BaseDirectory, "launchguard.config.json");

    public static Dictionary<string, string> LockedProcesses { get; private set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public static void Load()
    {
        if (!File.Exists(ConfigPath))
        {
            LockedProcesses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return;
        }

        try
        {
            string json = File.ReadAllText(ConfigPath);
            var loaded = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                ?? new Dictionary<string, string>();

            var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in loaded)
            {
                string process = entry.Key?.Trim() ?? string.Empty;
                string password = entry.Value?.Trim() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(process) && !string.IsNullOrWhiteSpace(password))
                {
                    normalized[process] = password;
                }
            }

            LockedProcesses = normalized;
        }
        catch
        {
            // Corrupt or unreadable config — start fresh
            LockedProcesses = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public static void Save()
    {
        string json = JsonSerializer.Serialize(LockedProcesses, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }
}