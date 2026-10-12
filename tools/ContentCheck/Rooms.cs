using PokemonPlatinumEngine.Data;
using static ContentCheck.Checks;

namespace ContentCheck;

/// <summary>
/// A room's file (<c>maps/&lt;Name&gt;.json</c>) held to the rules <c>DataFileTests</c> has kept for map files (it loads,
/// it reads back as it is written, its warps land on open ground, its wild Pokémon are species) and to those nothing
/// held: what stands on it stands inside it, its scripts are scripts, its flags are set somewhere, its lines fit.
/// </summary>
public static class Rooms
{
    public static void Check(GameContent content, GameContent.Room room, Flags flags, List<Finding> found)
    {
        string file = room.File;
        void Add(Kind kind, string name, string what) => found.Add(new Finding(kind, file, name, what));

        foreach (var (path, what) in JsonShape.UnknownFields(room.Text, typeof(MapFile))) Add(Kind.UnknownField, path, what);
        if (room.Source is not { } source) return;
        bool Inside(int x, int y) => x >= 0 && y >= 0 && x < source.Width && y < source.Height;

        // The file is what the game would write of the map it makes: nothing in it is lost on the way, and a tool
        // that writes rooms (MapFile.FromMap) writes what a hand would
        if (room.Map != null && FirstDifference(room.Text.Replace("\r\n", "\n"), GameDataFiles.Serialize(MapFile.FromMap(room.Map))) is { } differs)
            Add(Kind.RoundTrip, $"line {differs.Line}", $"the file doesn't read back as it is written: it has `{differs.Mine.Trim()}` where the game writes `{differs.Theirs.Trim()}`");

        Song(found, file, "bgmTrack", source.BgmTrack);
        EvolutionSites(found, file, "evolutionSites", source.EvolutionSites);

        foreach (var warp in source.Warps)
        {
            string name = $"the warp at {warp.SourceX},{warp.SourceY}";
            if (!Inside(warp.SourceX, warp.SourceY)) Add(Kind.Way, name, $"stands outside the room, which is {source.Width} by {source.Height} tiles");
            Landing(content, found, file, name, warp.TargetMap, warp.TargetX, warp.TargetY);
            flags.Check(found, file, name, warp.Until, "is there until");
            flags.Check(found, file, name, warp.From, "is there from");
        }

        foreach (var sign in source.Signboards)
        {
            string name = $"the sign at {sign.X},{sign.Y}";
            if (!Inside(sign.X, sign.Y)) Add(Kind.Place, name, "stands outside the room");
            Script(content, found, file, name, sign.Script, room.Name);
            SignText(content, found, file, name, sign.Text, sign.Script, room.Name);
        }

        foreach (var trigger in source.Triggers ?? new())
        {
            string name = $"the trigger at {trigger.X},{trigger.Y}";
            if (!Inside(trigger.X, trigger.Y) || !Inside(trigger.X + trigger.Width - 1, trigger.Y + trigger.Depth - 1)) Add(Kind.Place, name, "reaches outside the room");
            Script(content, found, file, name, trigger.Script, room.Name, needed: true);
        }

        foreach (var prop in source.Props)
            if (!Inside(prop.X, prop.Y) || !Inside(prop.X + prop.Width - 1, prop.Y + prop.Depth - 1))
                Add(Kind.Place, $"the {prop.Type} at {prop.X},{prop.Y}", "reaches outside the room");

        foreach (var hidden in source.HiddenItems ?? new())
            if (!Inside(hidden.X, hidden.Y)) Add(Kind.Place, $"the hidden {hidden.Item} at {hidden.X},{hidden.Y}", "lies outside the room");

        foreach (var npc in source.Npcs)
        {
            string name = $"{npc.Id ?? npc.Name} at {npc.X},{npc.Y}";
            if (!Inside(npc.X, npc.Y)) Add(Kind.Place, name, "stands outside the room");
            Person(content, flags, found, file, name, npc, room.Name);
        }

        foreach (var wild in source.WildEncounters)
            if (PokemonDatabase.Get(wild.SpeciesName) == null) Add(Kind.Data, "wildEncounters", $"'{wild.SpeciesName}' is no species");
    }

    /// <summary>The first line two texts part at, or null when they are the same.</summary>
    private static (int Line, string Mine, string Theirs)? FirstDifference(string mine, string theirs)
    {
        if (mine == theirs) return null;
        string[] a = mine.Split('\n'), b = theirs.Split('\n');
        for (int i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            string left = i < a.Length ? a[i] : "(the end of the file)", right = i < b.Length ? b[i] : "(the end of the file)";
            if (left != right) return (i + 1, left, right);
        }
        return null;
    }
}
