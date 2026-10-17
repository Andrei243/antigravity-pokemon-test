using System.Reflection;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace ContentCheck;

/// <summary>
/// Everything the content check holds the hand-written files to (plan 16 · T8). <see cref="All"/> is what the tool
/// prints and what <c>ContentTests</c> asserts is empty for the game's own files; each part can be asked alone.
/// <para>
/// What it looks for is, above all, what the game's readers pass over without a word: an overlay's entry for someone
/// the area hasn't, a door numbered past the area's warps, a field misspelt, a script nothing has, a flag nothing
/// sets, a line that runs out of its box. What would stop the game as it loads is found too, and told of first.
/// </para>
/// </summary>
public static class Checks
{
    private static readonly Lazy<Findings> own = new(() => All(GameContent.OfTheGame));

    /// <summary>What is found in the game's own files, looked for once: what the tests of the repository share.</summary>
    public static Findings OfTheGame => own.Value;

    /// <summary>Every finding in a folder's files, in the files' order.</summary>
    public static Findings All(GameContent content)
    {
        var found = new Findings(content.Unreadable);
        var flags = new Flags(content.Scripts);
        foreach (var region in content.Regions)
        {
            foreach (var overlay in region.Overlays) Overlays.Check(content, region, overlay, flags, found);
            Overlays.CheckOpenAreas(content, region, found);
            Overlays.CheckWays(content, region, found);
        }
        foreach (var room in content.Rooms) Rooms.Check(content, room, flags, found);
        Scripts.Check(content, found);
        Translations.Check(content, found);
        flags.Stale(found);
        return found;
    }

    /// <summary>
    /// The flags something sets: every flag a script sets or clears, and those the game sets by itself. A flag that
    /// hides or shows someone, or opens a way, and is none of these is a misspelling, or a scene nobody has written.
    /// </summary>
    public sealed class Flags
    {
        private readonly HashSet<string> written;

        public Flags(ScriptLibrary scripts)
        {
            written = new HashSet<string>(scripts.FlagsWritten, StringComparer.Ordinal);
            // What the game sets as the player goes: a region's Hall of Fame, a first arrival in a town or at a
            // landmark of the region's map, the field moves in force, the Pokédex and the Running Shoes, the
            // Underground Man's missions, the Villa's furniture, the Contest Hall visited, the Cycling Road's gates
            written.UnionWith(RegionDatabase.All.Select(r => r.StoryCompleteFlag));
            written.UnionWith(SpawnLocations.All.Select(s => s.ArrivalFlag));
            written.UnionWith(SpawnLocations.All.Select(SpawnLocations.FlyFlag));
            written.UnionWith(new[] { FieldMoveRules.StrengthFlag, FieldMoveRules.FlashFlag, FieldMoveRules.DefogFlag, StoryState.PokedexFlag, StoryState.RunningShoesFlag });
            written.UnionWith(GameEngine.UndergroundMissionFlags);
            written.UnionWith(GameEngine.PastimeFlags);
            written.Add(GameEngine.ContestHallVisitedFlag);
            written.Add(BicycleRules.ForcedInGateFlag);
        }

        public bool IsSet(string flag) => written.Contains(flag);

        /// <summary>
        /// Flags that are named in the files on purpose though nothing sets them, each with its reason: the only
        /// exceptions the check makes. One that something comes to set is told of (<see cref="Stale"/>), so the list
        /// can only shrink.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> NeverSetOnPurpose = new Dictionary<string, string>
        {
            // maps/EternaGym.json, gardenia
            ["FLAG_HIDE_ETERNA_CITY_GYM_GARDENIA"] = "the original's own flag of Gardenia inside her Gym, which keeps her out of it while she waits at its door. "
                + "Here she stands on the tile before the door until she is spoken to and goes in (scripts/eterna_city.txt, Gardenia), so nobody comes into the Gym before her and no scene sets it"
        };

        /// <summary>A flag someone or some way goes by: named, it must be one something sets.</summary>
        public void Check(List<Finding> found, string file, string name, string? flag, string does)
        {
            if (string.IsNullOrEmpty(flag) || IsSet(flag) || NeverSetOnPurpose.ContainsKey(flag)) return;
            found.Add(new Finding(Kind.Flag, file, name, $"{does} the flag {flag}, which no script sets or clears and the game doesn't set by itself"));
        }

        /// <summary>The exceptions that are exceptions no longer: a flag of <see cref="NeverSetOnPurpose"/> that something sets.</summary>
        public void Stale(List<Finding> found)
        {
            foreach (string flag in NeverSetOnPurpose.Keys.Where(IsSet))
                found.Add(new Finding(Kind.Flag, "tools/ContentCheck/Checks.cs", flag, "is listed as a flag nothing sets, and something sets it now: take it off NeverSetOnPurpose"));
        }
    }

    // ------------------------------------------------------------------ what people and rooms share

    /// <summary>A script someone or something starts must be one the place's file or the common file has.</summary>
    internal static void Script(GameContent content, List<Finding> found, string file, string name, string? script, string place, bool needed = false)
    {
        if (string.IsNullOrEmpty(script))
        {
            if (needed) found.Add(new Finding(Kind.Script, file, name, "names no script, so nothing happens there"));
            return;
        }
        if (content.Scripts.Find(script, place) == null)
            found.Add(new Finding(Kind.Script, file, name,
                script.Contains('.') ? $"starts the script '{script}', which no file has" : $"starts the script '{script}', which is in neither scripts/{place}.txt nor scripts/{ScriptLibrary.Common}.txt"));
    }

    /// <summary>
    /// A sign's words: one line of the box. A sign that runs a script of its own may be written with none, unless the
    /// script says them (<c>sayown</c>): then the box would come up empty.
    /// </summary>
    internal static void SignText(GameContent content, List<Finding> found, string file, string name, string text, string? script, string place)
    {
        bool said = string.IsNullOrEmpty(script) || content.Scripts.Find(script, place) is not { } own || own.Everything().Any(i => i.Op == Op.SayOwn);
        if (said) Lines.Check(found, file, name, text, "Sign");
    }

    /// <summary>A tile somewhere lands on: it must be on the map and open (furniture, a wall and a fence never stand where somebody arrives).</summary>
    internal static void Landing(GameContent content, List<Finding> found, string file, string name, string mapName, int x, int y)
    {
        if (content.MapNamed(mapName) is not { } target)
        {
            found.Add(new Finding(Kind.Way, file, name, $"leads to '{mapName}', which is no map (the game would put the player on the map of Sinnoh)"));
            return;
        }
        if (!target.InBounds(x, y)) found.Add(new Finding(Kind.Way, file, name, $"leads to {x},{y} of {mapName}, which is {target.Width} by {target.Height} tiles"));
        else if (target.IsSolid(x, y)) found.Add(new Finding(Kind.Way, file, name, $"puts the player down on a blocked tile of {mapName} ({x},{y})"));
    }

    // What stands on a map as something other than a person: an item's ball, a Pokémon, an obstacle, a Gym's piece, a
    // door of the League (NPC's own names for them)
    private static readonly Lazy<HashSet<string>> things = new(() => typeof(NPC).GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(f => f.IsLiteral && f.FieldType == typeof(string) && f.Name.EndsWith("Type", StringComparison.Ordinal))
        .Select(f => (string)f.GetRawConstantValue()!).ToHashSet(StringComparer.Ordinal));

    /// <summary>
    /// Who someone is drawn as (<c>npcType</c>) must be a look of <c>characters.json</c>, the player or the assistant, a
    /// rig of its own (the briefcase, a rift) or a thing of the map. Anything else is drawn as the plain default, a
    /// boy in a green shirt, without a word.
    /// </summary>
    internal static void Look(List<Finding> found, string file, string name, string? npcType)
    {
        if (npcType == null || things.Value.Contains(npcType) || CharacterModels.KindOf(npcType) != RigKind.Humanoid) return;
        if (CharacterStyles.Default.Has(PlayerIdentity.CharacterFor(npcType, PlayerLook.Boy))) return;
        string? near = CharacterStyles.Default.Names.Concat(things.Value)
            .FirstOrDefault(known => string.Equals(known.Replace("_", ""), npcType.Replace("_", "").Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
        found.Add(new Finding(Kind.Data, file, name, $"is drawn as '{npcType}', which is no look of {CharacterStyles.FileName} and no thing of the map: they would stand there as the plain default"
            + (near != null ? $" ('{near}'?)" : "")));
    }

    /// <summary>
    /// Someone written out in full, as a room's file and an overlay's <c>npcs</c> write them: their look, their script,
    /// their counter, their team, their flags and their lines.
    /// </summary>
    internal static void Person(GameContent content, Flags flags, List<Finding> found, string file, string name, MapFile.NpcRecord n, string place)
    {
        Look(found, file, name, n.NpcType);
        Script(content, found, file, name, n.Script, place);
        if (n.Mart is { Length: > 0 } mart && !MartDatabase.Specialties.ContainsKey(mart))
            found.Add(new Finding(Kind.Data, file, name, $"keeps the counter '{mart}', which marts.json lacks"));
        // An item's ball is hidden by the flag of its own finding, which the ball's script sets
        if (n.NpcType != NPC.ItemBallType) flags.Check(found, file, name, n.HiddenBy, "is hidden by");
        flags.Check(found, file, name, n.ShownBy, "waits for");
        if (n.Item != null && ItemDatabase.Get(n.Item) == null) found.Add(new Finding(Kind.Data, file, name, $"holds '{n.Item}', which is no item"));
        if (n.Species != null && PokemonDatabase.Get(n.Species) == null && PokemonDatabase.SpeciesOfForm(n.Species) == null)
            found.Add(new Finding(Kind.Data, file, name, $"is the Pokémon '{n.Species}', which is no species or form"));

        Lines.CheckName(found, file, name, n.Name);
        foreach (string line in n.Dialog ?? new()) Lines.Check(found, file, name, line, n.Name);
        if (n.Trainer is not { } t) return;

        var record = TrainerDatabase.Get(t.Id);
        if (t.Party.Count == 0 && record == null)
            found.Add(new Finding(Kind.Data, file, name, $"is the trainer '{t.Id}', whom trainers.json lacks, and types no team: there is nobody to battle"));
        foreach (var member in t.Party)
        {
            if (PokemonDatabase.Get(member.Species) == null) found.Add(new Finding(Kind.Data, file, name, $"has a '{member.Species}', which is no species"));
            foreach (string move in member.Moves ?? new())
                if (MoveDatabase.Get(move) == null) found.Add(new Finding(Kind.Data, file, name, $"'s {member.Species} knows '{move}', which is no move"));
        }
        TrainerLines(found, file, name, t.DialogueBefore, t.DialogueAfter, n.Name, t.Name, t.TrainerClass, t.Party.Count == 0 ? record : null);
    }

    /// <summary>
    /// A trainer's two lines. They are said under the trainer's whole title where they begin with it, and under the
    /// person's name otherwise: either is left off the line as it is written (<c>trainerline</c>, <c>DialogueManager</c>).
    /// </summary>
    internal static void TrainerLines(List<Finding> found, string file, string name, string before, string after, string person, string? own, string? ownClass, TrainerRecord? record)
    {
        var speakers = new List<string?> { person };
        if (!string.IsNullOrEmpty(own)) speakers.Add($"{ownClass} {own}");
        if (record != null)
        {
            // The table's name and class, as TrainerDatabase.Fill gives them to someone who writes none of their own
            string trainer = string.IsNullOrEmpty(own) ? record.Class == TrainerDatabase.RivalClass ? "{rival}" : record.Name : own;
            string trainerClass = string.IsNullOrEmpty(ownClass) || ownClass == "Trainer" ? record.Class : ownClass;
            speakers.Add($"{trainerClass} {trainer}");
        }
        if (before.Length > 0) Lines.Check(found, file, name, before, speakers.ToArray());
        if (after.Length > 0) Lines.Check(found, file, name, after, speakers.ToArray());
    }

    /// <summary>The song a place plays must be one of the songs.</summary>
    internal static void Song(List<Finding> found, string file, string name, string? track)
    {
        if (!string.IsNullOrEmpty(track) && !MusicLibrary.Exists(track))
            found.Add(new Finding(Kind.Music, file, name, $"plays '{track}', which is no song of Data/music"));
    }

    private static readonly Lazy<HashSet<string>> evolutionSites = new(() =>
        PokemonDatabase.GetAll().SelectMany(s => s.Evolutions ?? new()).Select(e => e.Location).OfType<string>().ToHashSet(StringComparer.Ordinal));

    /// <summary>A place some Pokémon evolve at must be one an evolution of the data names, or nothing ever evolves there.</summary>
    internal static void EvolutionSites(List<Finding> found, string file, string name, IEnumerable<string>? sites)
    {
        foreach (string site in sites ?? Enumerable.Empty<string>())
            if (!evolutionSites.Value.Contains(site))
                found.Add(new Finding(Kind.Data, file, name, $"is the evolution site '{site}', which no species' evolution names (they name: {string.Join(", ", evolutionSites.Value.Order())})"));
    }
}
