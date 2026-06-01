using Godot;

namespace TouhouAncients.Scripts;

public static class TouhouAncientCmd
{
    public static string? CheckPathExists(string path) => ResourceLoader.Exists(path) ? path : null;

    public static string CheckPathExistsWithFallback(string path, string alternative) =>
        ResourceLoader.Exists(path) ? path : alternative;
}