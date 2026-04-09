using System.Reflection;

namespace LaunchGuard;

internal static class ResourceHelper
{
    private static Stream GetResourceStream(string filename)
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetManifestResourceStream($"LaunchGuard.media.{filename}")
            ?? throw new Exception($"Embedded resource not found: {filename}");
    }

    public static Image LoadImage(string filename)
    {
        return Image.FromStream(GetResourceStream(filename));
    }

    public static Icon LoadIcon(string filename)
    {
        return new Icon(GetResourceStream(filename));
    }
}