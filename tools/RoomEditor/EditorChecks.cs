using System.Text.Json;
using ContentCheck;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace RoomEditor;

/// <summary>
/// The content check (plan 16 · T8) asked of one file as it is edited: the room's rules of <see cref="Rooms"/> and an
/// overlay's of <see cref="Overlays"/>, held against the rest of a data folder as it is (its scripts, its maps, the
/// flags its scripts set). Never a check of its own: the findings are the tool's, and a rule added there is held
/// here too.
/// </summary>
public static class EditorChecks
{
    /// <summary>What the content check finds in a room as the editor has it, against the folder's other files.</summary>
    public static List<Finding> Room(GameContent content, EditorDocument document)
    {
        var found = new List<Finding>();
        string name = document.File.Name, file = $"{MapDatabase.Folder}/{name}.json";
        string text;
        MapFile? source = null;
        Map? map = null;
        try
        {
            text = document.SaveText();
            source = GameDataFiles.Deserialize<MapFile>(text);
            map = source.ToMap();
        }
        catch (Exception e) when (e is JsonException or InvalidDataException or ArgumentException or IndexOutOfRangeException or ArgumentOutOfRangeException)
        {
            found.Add(new Finding(Kind.Unreadable, file, name, "the game would not load it: " + e.Message));
            return found;
        }
        Rooms.Check(content, new GameContent.Room(name, file, text, source, map), new Checks.Flags(content.Scripts), found);
        return found;
    }

    /// <summary>What the content check finds in an overlay as the editor has it.</summary>
    public static List<Finding> Overlay(GameContent content, OverlayDocument document)
    {
        var found = new List<Finding>();
        var region = content.Regions.FirstOrDefault(r => r.World.Area(document.Key) != null);
        string file = region != null ? $"{region.Folder}/overlays/{document.Key}.json" : $"overlays/{document.Key}.json";
        WorldOverlayFile? overlay = null;
        try
        {
            overlay = GameDataFiles.Deserialize<WorldOverlayFile>(document.Text);
        }
        catch (JsonException e)
        {
            found.Add(new Finding(Kind.Unreadable, file, document.Key, "it can't be read: " + e.Message));
            return found;
        }
        if (region == null)
        {
            found.Add(new Finding(Kind.Misplaced, file, document.Key, "no region of the data folder has this area"));
            return found;
        }
        Overlays.Check(content, region, new GameContent.OverlaySource(document.Key, file, document.Text, overlay), new Checks.Flags(content.Scripts), found);
        return found;
    }
}
