namespace LaunchGuard;
/* This class is used to store the application configuration, 
 such as the list of locked processes and their associated passwords.*/
internal static class AppConfig
{
    public static Dictionary<string, string> LockedProcesses {get;set;} = new();
}