using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Story;

/// <summary>
/// Everything the story remembers, saved with the game (plan 02 · S1): flags that are set or not, variables that
/// count, the trainers beaten, the items picked up off the ground, each region's badges, and which starter the player and
/// the rival took. Flags and variables are named after the original's (<c>FLAG_...</c>, <c>VAR_...</c>) so its
/// scripts stay easy to follow; a region's Hall of Fame is a flag too (<see cref="Region.StoryCompleteFlag"/>).
/// It draws nothing and knows nothing of the screen: scripts change it and the engine asks it.
/// </summary>
public sealed class StoryState
{
    /// <summary>
    /// How much of the story a save knows about. A save from before story state existed is 0; every chapter that
    /// needs older saves brought up to date raises it and adds a step to <see cref="Story.StoryMigration"/>.
    /// </summary>
    public const int CurrentVersion = 26;

    /// <summary>The Pokédex is the player's (the original's own flag, set as Rowan hands it over).</summary>
    public const string PokedexFlag = "FLAG_HAS_POKEDEX";

    /// <summary>Mom has given the Running Shoes (in the original a flag of the player's data; plan 02 · S4).</summary>
    public const string RunningShoesFlag = "FLAG_HAS_RUNNING_SHOES";

    private readonly HashSet<string> flags = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> variables = new(StringComparer.Ordinal);
    private readonly HashSet<string> defeated = new(StringComparer.Ordinal);
    private readonly HashSet<string> taken = new(StringComparer.Ordinal);
    private readonly HashSet<string> greeted = new(StringComparer.Ordinal);

    /// <summary>Counts every change, so whoever shows the world can tell when to look again (who is hidden by a flag).</summary>
    public int Revision { get; private set; }

    // ------------------------------------------------------------------ flags

    public IReadOnlyCollection<string> Flags => flags;

    public bool Has(string flag) => flags.Contains(flag);

    public void Set(string flag)
    {
        if (flags.Add(flag)) Revision++;
    }

    public void Unset(string flag)
    {
        if (flags.Remove(flag)) Revision++;
    }

    /// <summary>The beginning of the name of a flag that lasts only while the player stays in one place (the original's map-local flags).</summary>
    public const string LocalFlagPrefix = "FLAG_MAP_LOCAL_";

    /// <summary>The same for a variable.</summary>
    public const string LocalVariablePrefix = "VAR_MAP_LOCAL_";

    /// <summary>
    /// The GBA games' temporary variables (<c>VAR_TEMP_0</c> to <c>VAR_TEMP_F</c>), which FireRed and Emerald clear as
    /// every map loads (<c>ClearTempFieldEventData</c>): they are forgotten with the local ones (plan 19 · K6, after the
    /// Pokémon Tower's fifth floor left <c>VAR_TEMP_1</c> set and the Cycling Road's gates and the Safari Zone's counter,
    /// whose triggers ask it to be 0, never fired again).
    /// </summary>
    public const string TemporaryVariablePrefix = "VAR_TEMP_";

    /// <summary>
    /// Forgets the flags and variables that last only while the player stays in one place, as the original does
    /// whenever the map's header changes (<c>FieldSystem_ClearLocalFlags</c>): on walking into another area and on
    /// every warp. A tree that was cut grows back, a rock that was smashed is whole again.
    /// </summary>
    public void ClearLocal()
    {
        bool changed = flags.RemoveWhere(f => f.StartsWith(LocalFlagPrefix, StringComparison.Ordinal)) > 0;
        foreach (var name in variables.Keys.Where(v => v.StartsWith(LocalVariablePrefix, StringComparison.Ordinal) || v.StartsWith(TemporaryVariablePrefix, StringComparison.Ordinal)).ToList())
        {
            variables.Remove(name);
            changed = true;
        }
        if (changed) Revision++;
    }

    /// <summary>Flags that last only until the day changes (the original's daily flags, <c>FLAG_DAILY_...</c>).</summary>
    public const string DailyFlagPrefix = "FLAG_DAILY_";

    /// <summary>
    /// A new day has come (<c>FieldSystem_ClearDailyFlags</c>, plan 06 · R13): every daily flag is cleared, so what
    /// was done once today (Mr. Backlot's news of the Trophy Garden) can be done again.
    /// </summary>
    public void ClearDaily()
    {
        if (flags.RemoveWhere(f => f.StartsWith(DailyFlagPrefix, StringComparison.Ordinal)) > 0) Revision++;
    }

    // ------------------------------------------------------------------ variables

    /// <summary>The variables that aren't nought, which is what a save keeps.</summary>
    public IReadOnlyDictionary<string, int> Variables => variables;

    /// <summary>A variable's value; one never set is nought, as in the original.</summary>
    public int Var(string name) => variables.GetValueOrDefault(name);

    public void SetVar(string name, int value)
    {
        if (Var(name) == value) return;
        if (value == 0) variables.Remove(name);
        else variables[name] = value;
        Revision++;
    }

    public void AddVar(string name, int amount) => SetVar(name, Var(name) + amount);

    // ------------------------------------------------------------------ trainers and items

    public IReadOnlyCollection<string> DefeatedTrainers => defeated;

    public bool HasDefeated(string trainerId) => defeated.Contains(trainerId);

    public void Defeat(string trainerId)
    {
        if (trainerId.Length > 0 && defeated.Add(trainerId)) Revision++;
    }

    /// <summary>The items picked up off the ground and the hidden ones found, each by its own id.</summary>
    public IReadOnlyCollection<string> TakenItems => taken;

    public bool HasTaken(string itemId) => taken.Contains(itemId);

    public void Take(string itemId)
    {
        if (taken.Add(itemId)) Revision++;
    }

    // ------------------------------------------------------------------ greetings

    /// <summary>
    /// The people spoken to since the Hallowed Tower last stirred, each once, by their place and key (plan 08 · P12):
    /// our stand-in for the original's Spiritomb counter, which counts people spoken to in the Underground. Scripts
    /// read the count as <c>GREETINGS</c>; <c>greetings clear</c> starts it again, as the original's
    /// <c>ClearSpiritombCounter</c> does.
    /// </summary>
    public IReadOnlyCollection<string> Greeted => greeted;

    public int Greetings => greeted.Count;

    public void Greet(string who)
    {
        if (who.Length > 0) greeted.Add(who);
    }

    public void ClearGreetings() => greeted.Clear();

    /// <summary>The greetings a save kept (<c>SaveData.GreetedPeople</c>).</summary>
    public void RestoreGreetings(IEnumerable<string> saved)
    {
        greeted.Clear();
        foreach (string who in saved) Greet(who);
    }

    // ------------------------------------------------------------------ scenes seen

    private readonly HashSet<string> scenes = new(StringComparer.Ordinal);

    /// <summary>
    /// The scenes this game has played to their end (plan 12 · Q20), each by its key: the file it is written in and its
    /// own name (<see cref="SceneSkip.Key"/>). They change nothing the story does and nobody is put anywhere for them:
    /// they only say what may be skipped under the options' <c>Seen</c>, so seeing one is no change that
    /// <see cref="Revision"/> counts.
    /// </summary>
    public IReadOnlyCollection<string> ScenesSeen => scenes;

    public bool HasSeenScene(string key) => scenes.Contains(key);

    /// <summary>Notes a scene as played to its end; true when it hadn't been.</summary>
    public bool SeeScene(string key) => key.Length > 0 && scenes.Add(key);

    /// <summary>The scenes a save kept (<c>SaveData.ScenesSeen</c>): none in a save from before they were kept.</summary>
    public void RestoreScenes(IEnumerable<string> saved)
    {
        scenes.Clear();
        foreach (string key in saved) SeeScene(key);
    }

    // ------------------------------------------------------------------ badges

    // Each region's badges as the save and the Trainer Card keep them (plan 18 · W2): one bit each, in the order of the
    // region's own list (Region.BadgeNames, from its file), under the region's id
    private readonly Dictionary<string, int> badges = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every region's badges, by the region's id: a mask each, none for a region with no badge won.</summary>
    public IReadOnlyDictionary<string, int> RegionBadges => badges;

    /// <summary>A region's badges, one bit each in the order of its Trainer Card.</summary>
    public int BadgeMask(string region) => badges.GetValueOrDefault(RegionKey(region));

    public bool HasBadge(string region, int index) => index >= 0 && (BadgeMask(region) & (1 << index)) != 0;

    /// <summary>Whether the player has a region's badge, by its name in the region's list (<c>Coal</c>, in any case).</summary>
    public bool HasBadge(string region, string name) => HasBadge(region, BadgeIndex(region, name));

    /// <summary>How many of a region's badges the player has: what obedience, the Marts and a script's <c>BADGE_COUNT</c> read of the region the player is in.</summary>
    public int BadgeCount(string region) => System.Numerics.BitOperations.PopCount((uint)BadgeMask(region));

    /// <summary>Every badge of every region (the Trainer Card's score counts each one won).</summary>
    public int TotalBadges => badges.Values.Sum(m => System.Numerics.BitOperations.PopCount((uint)m));

    public void GiveBadge(string region, int index)
    {
        if (index < 0 || index >= BadgeSlots(region)) throw new ArgumentOutOfRangeException(nameof(index), $"{region} has no badge {index}");
        if (HasBadge(region, index)) return;
        badges[RegionKey(region)] = BadgeMask(region) | 1 << index;
        Revision++;
    }

    public void GiveBadge(string region, string name) => GiveBadge(region, BadgeIndex(region, name));

    /// <summary>Sets a region's badges at once (a save, a tool, a test); bits past the region's own badges are dropped.</summary>
    public void SetBadges(string region, int mask)
    {
        mask &= (1 << BadgeSlots(region)) - 1;
        if (mask == BadgeMask(region)) return;
        if (mask == 0) badges.Remove(RegionKey(region));
        else badges[RegionKey(region)] = mask;
        Revision++;
    }

    /// <summary>Where a badge stands in its region's list, by its name in any case; -1 when the region has no such badge.</summary>
    public static int FindBadge(string region, string name)
    {
        var names = RegionDatabase.Get(region)?.BadgeNames ?? Array.Empty<string>();
        for (int k = 0; k < names.Count; k++)
            if (string.Equals(names[k], name, StringComparison.OrdinalIgnoreCase)) return k;
        return -1;
    }

    /// <summary>Where a badge stands in its region's list; a name the region doesn't have is the caller's mistake, and throws.</summary>
    public static int BadgeIndex(string region, string name) =>
        FindBadge(region, name) is var k and >= 0 ? k : throw new ArgumentException($"{region} has no badge called '{name}'", nameof(name));

    private static int BadgeSlots(string region) => Math.Min(RegionDatabase.Get(region)?.BadgeNames.Count ?? 0, 31);

    private static string RegionKey(string region) => RegionDatabase.Get(region)?.Id ?? region;

    // ------------------------------------------------------------------ the starters

    /// <summary>
    /// The three Pokémon Professor Rowan's briefcase holds: Sinnoh's, which Sinnoh's story is written on (the rival's
    /// battles, the assistant's). A region's own are <see cref="Region.Starters"/> (plan 18 · W1).
    /// </summary>
    public static IReadOnlyList<string> Starters => RegionDatabase.Get(RegionDatabase.Sinnoh)!.Starters;

    /// <summary>The species the player took from the briefcase; null until they have.</summary>
    public string? PlayerStarter { get; private set; }

    /// <summary>The species the rival took: as in the original, the one that is strong against the player's.</summary>
    public string? RivalStarter { get; private set; }

    /// <summary>The species the professor's assistant has: the one left in the briefcase (the original's counterpart's starter); null until the player has chosen.</summary>
    public string? AssistantStarter => PlayerStarter == null ? null : StartersOf(PlayerStarter).First(s => s != PlayerStarter && s != RivalStarter);

    /// <summary>
    /// The starter the rival takes when the player takes this one (grass is answered with fire, fire with water, water
    /// with grass), from the briefcase the species was in: Sinnoh's, or Kanto's on Oak's table (plan 19 · K3).
    /// </summary>
    public static string RivalStarterFor(string playerStarter) => RegionOfStarter(playerStarter).RivalStarterFor(playerStarter);

    /// <summary>The three a starter was chosen from: the first region whose briefcase holds it, else Sinnoh's.</summary>
    public static IReadOnlyList<string> StartersOf(string starter) => RegionOfStarter(starter).Starters;

    private static Region RegionOfStarter(string starter) =>
        RegionDatabase.All.FirstOrDefault(r => r.Starters.Contains(starter)) ?? RegionDatabase.Get(RegionDatabase.Sinnoh)!;

    public void ChooseStarter(string species)
    {
        PlayerStarter = species;
        RivalStarter = RivalStarterFor(species);
        Revision++;
    }

    // ------------------------------------------------------------------ regions

    public bool IsRegionComplete(string regionId) => RegionDatabase.Get(regionId) is { } r && Has(r.StoryCompleteFlag);

    /// <summary>Marks a region's story as finished.</summary>
    public void CompleteRegion(string regionId)
    {
        if (RegionDatabase.Get(regionId) is { } r) Set(r.StoryCompleteFlag);
    }

    // ------------------------------------------------------------------ a new game, a save

    public void Clear()
    {
        flags.Clear();
        variables.Clear();
        defeated.Clear();
        taken.Clear();
        greeted.Clear();
        scenes.Clear();
        badges.Clear();
        PlayerStarter = RivalStarter = null;
        Revision++;
    }

    /// <summary>What a save keeps of the story.</summary>
    public StorySnapshot Snapshot() => new(
        flags.Order(StringComparer.Ordinal).ToList(),
        variables.OrderBy(v => v.Key, StringComparer.Ordinal).ToDictionary(v => v.Key, v => v.Value),
        defeated.Order(StringComparer.Ordinal).ToList(),
        taken.Order(StringComparer.Ordinal).ToList(),
        badges.Where(b => b.Value != 0).OrderBy(b => b.Key, StringComparer.Ordinal).ToDictionary(b => b.Key, b => b.Value),
        PlayerStarter, RivalStarter);

    public void Restore(StorySnapshot saved)
    {
        Clear();
        foreach (string f in saved.Flags) flags.Add(f);
        foreach (var (name, value) in saved.Variables)
            if (value != 0) variables[name] = value;
        foreach (string t in saved.DefeatedTrainers) defeated.Add(t);
        foreach (string i in saved.TakenItems) taken.Add(i);
        foreach (var (region, mask) in saved.Badges)
        {
            int kept = mask & ((1 << BadgeSlots(region)) - 1);
            if (kept != 0) badges[RegionKey(region)] = kept;
        }
        PlayerStarter = saved.PlayerStarter;
        RivalStarter = saved.RivalStarter ?? (saved.PlayerStarter != null ? RivalStarterFor(saved.PlayerStarter) : null);
        Revision++;
    }

    /// <summary>Only the flags, as saves from before <see cref="StoryState"/> kept them.</summary>
    public void Restore(IEnumerable<string> savedFlags) =>
        Restore(new StorySnapshot(savedFlags.ToList(), new Dictionary<string, int>(), new List<string>(), new List<string>(),
            new Dictionary<string, int>(), null, null));
}

/// <summary>The story as a save writes it: plain lists and numbers. <see cref="Badges"/> is each region's mask by its id (plan 18 · W2).</summary>
public sealed record StorySnapshot(
    List<string> Flags,
    Dictionary<string, int> Variables,
    List<string> DefeatedTrainers,
    List<string> TakenItems,
    Dictionary<string, int> Badges,
    string? PlayerStarter,
    string? RivalStarter)
{
    /// <summary>One region's badges as the snapshot has them.</summary>
    public int BadgesOf(string region) => Badges.FirstOrDefault(b => string.Equals(b.Key, region, StringComparison.OrdinalIgnoreCase)).Value;
}
