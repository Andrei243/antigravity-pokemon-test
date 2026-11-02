using PokemonPlatinumEngine.Data;

namespace RoomEditor;

/// <summary>Where an area of a data folder's worlds is: its file, read, and the path of its overlay (there or not).</summary>
public static class EditorAreas
{
    public static (WorldAreaFile Area, string OverlayPath)? Find(string dataFolder, string key)
    {
        string root = Path.Combine(dataFolder, World.Folder);
        if (!Directory.Exists(root)) return null;
        foreach (string region in Directory.GetDirectories(root).OrderBy(d => d, StringComparer.Ordinal))
        {
            string file = Path.Combine(region, "areas", key + ".json");
            if (File.Exists(file))
                return (GameDataFiles.Deserialize<WorldAreaFile>(File.ReadAllText(file)), Path.Combine(region, "overlays", key + ".json"));
        }
        return null;
    }
}
