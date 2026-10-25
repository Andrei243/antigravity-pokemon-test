using PokemonPlatinumEngine.Graphics;

namespace BlenderAnim;

/// <summary>
/// The clips the tool keys, one to each of the game's roles, and the names it gives them: each name is one the game's
/// own matcher (<see cref="ImportedModels.MatchClips"/>) gives that role, which <c>BlenderAnimTests</c> holds.
/// </summary>
internal static class BattleClips
{
    /// <summary>The roles in the order the scripts key them, with the clip's name and the script's own word for the role.</summary>
    public static readonly (string Role, string Clip)[] All =
    {
        ("idle", "idle_breath"),
        ("physical", "attack01_lunge"),
        ("special", "attack02_cast"),
        ("status", "status_buff"),
        ("hit", "damage"),
        ("faint", "faint"),
        ("entry", "appear"),
        ("dodge", "dodge"),
        ("tired", "tired"),
        ("cheer", "victory"),
    };

    public static ClipRole RoleOf(string role) => Enum.Parse<ClipRole>(role, ignoreCase: true);

    /// <summary>The clips of <paramref name="only"/>'s roles (all when it is empty), or an error naming an unknown role.</summary>
    public static (Dictionary<string, string> Clips, string? Error) Chosen(IReadOnlyCollection<string> only)
    {
        foreach (var r in only)
            if (!All.Any(c => c.Role.Equals(r, StringComparison.OrdinalIgnoreCase)))
                return (new(), $"no role \"{r}\": the roles are {string.Join(", ", All.Select(c => c.Role))}");
        var clips = All.Where(c => only.Count == 0 || only.Any(r => r.Equals(c.Role, StringComparison.OrdinalIgnoreCase)))
                       .ToDictionary(c => c.Role, c => c.Clip);
        return (clips, null);
    }

    /// <summary>
    /// The file's own clips that would play in place of one the tool writes: each is dropped, until the matcher gives
    /// every role written its new clip. A clip of the file's that takes a role the tool doesn't write is kept.
    /// </summary>
    public static List<string> ToDrop(IReadOnlyList<string> existing, IReadOnlyDictionary<string, string> written)
    {
        var kept = existing.Where(n => !written.Values.Contains(n, StringComparer.OrdinalIgnoreCase)).ToList();
        var drop = existing.Except(kept).ToList();   // a clip of the same name is written over
        while (true)
        {
            var names = kept.Concat(written.Values).ToList();
            var match = ImportedModels.MatchClips(names);
            var taker = written.Keys.Select(r => match.TryGetValue(RoleOf(r), out int i) ? names[i] : null)
                               .FirstOrDefault(n => n != null && kept.Contains(n));
            if (taker == null) return drop;
            kept.Remove(taker);
            drop.Add(taker);
        }
    }

    /// <summary>Which clip of <paramref name="names"/> the game plays for each role, as it will read the file.</summary>
    public static IEnumerable<(ClipRole Role, string? Clip)> AsTheGameReads(IReadOnlyList<string> names)
    {
        var match = ImportedModels.MatchClips(names);
        foreach (var role in Enum.GetValues<ClipRole>())
            yield return (role, match.TryGetValue(role, out int i) ? names[i] : null);
    }
}
