using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

/// <summary>
/// Who speaks the lines of a script that "the person spoken to" says (plan 26 · V5): the voice of such a line is decided
/// as the talk starts, so a take is needed in each voice of the people who may start the script. This walks every person
/// of every map, finds the script that talking to them runs (<see cref="FieldScripts.For"/>, in their place's file first and
/// <c>common</c> second) and the scripts that one calls, and gives each script's full name the voices of those people
/// (<see cref="ScriptRunner.VoiceOf"/>, the very function the game asks).
/// </summary>
public static class SelfVoices
{
    public static Dictionary<string, SortedSet<string>> Of(VoiceCast cast, string? region = null)
    {
        MapDatabase.Initialize();
        var library = ScriptLibrary.Default;
        var result = new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

        void Add(Script script, string voice, string file, HashSet<string> seen)
        {
            if (!seen.Add(script.FullName)) return;
            if (!result.TryGetValue(script.FullName, out var set)) result[script.FullName] = set = new SortedSet<string>(StringComparer.Ordinal);
            set.Add(voice);
            foreach (var i in script.Everything())
                if (i.Op == Op.Call && library.Find(i.Name, script.File) is { } called) Add(called, voice, file, seen);
        }

        foreach (string name in MapDatabase.MapNames.OrderBy(n => n, StringComparer.Ordinal))
        {
            // (a map no region lists is Sinnoh's)
            if (region != null && !string.Equals(RegionDatabase.RegionOfMap(name)?.Id ?? "Sinnoh", region, StringComparison.OrdinalIgnoreCase)) continue;
            var map = MapDatabase.TryGet(name);
            if (map == null) continue;
            foreach (var npc in map.Everyone)
            {
                if (npc.IsPCTerminal || npc.IsThing || npc.IsPokemon) continue;
                if (FieldScripts.For(npc) is not { } scriptName) continue;
                string file = npc.ScriptFile ?? map.ScriptFileAt(npc.GridX, npc.GridY);
                if (library.Find(scriptName, file) is not { } script) continue;
                if (ScriptRunner.VoiceOf(npc, file, cast) is not { } voice) continue;
                Add(script, voice, file, new HashSet<string>(StringComparer.Ordinal));
            }
        }
        return result;
    }

    /// <summary>The full name of the script a line of the catalogue belongs to (<c>script:common.Nurse#1.0</c> is <c>common.Nurse</c>), or null for a line of a person.</summary>
    public static string? ScriptOf(string key)
    {
        if (!key.StartsWith("script:", StringComparison.Ordinal)) return null;
        int hash = key.IndexOf('#');
        return hash < 0 ? null : key[7..hash];
    }
}
