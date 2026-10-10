using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Story;

/// <summary>Sinnoh's eight badges, in the order the Trainer Card shows them and the original numbers them.</summary>
public enum Badge { Coal, Forest, Cobble, Fen, Relic, Mine, Icicle, Beacon }

/// <summary>
/// Everything the story remembers, saved with the game (plan 02 · S1): flags that are set or not, variables that
/// count, the trainers beaten, the items picked up off the ground, the badges, and which starter the player and
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
    public const int CurrentVersion = 7;

    /// <summary>The Pokédex is the player's (the original's own flag, set as Rowan hands it over).</summary>
    public const string PokedexFlag = "FLAG_HAS_POKEDEX";

    /// <summary>Mom has given the Running Shoes (in the original a flag of the player's data; plan 02 · S4).</summary>
    public const string RunningShoesFlag = "FLAG_HAS_RUNNING_SHOES";

    private readonly HashSet<string> flags = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> variables = new(StringComparer.Ordinal);
    private readonly HashSet<string> defeated = new(StringComparer.Ordinal);
    private readonly HashSet<string> taken = new(StringComparer.Ordinal);

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
    /// Forgets the flags and variables that last only while the player stays in one place, as the original does
    /// whenever the map's header changes (<c>FieldSystem_ClearLocalFlags</c>): on walking into another area and on
    /// every warp. A tree that was cut grows back, a rock that was smashed is whole again.
    /// </summary>
    public void ClearLocal()
    {
        bool changed = flags.RemoveWhere(f => f.StartsWith(LocalFlagPrefix, StringComparison.Ordinal)) > 0;
        foreach (var name in variables.Keys.Where(v => v.StartsWith(LocalVariablePrefix, StringComparison.Ordinal)).ToList())
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

    // ------------------------------------------------------------------ people spoken to

    private readonly HashSet<string> greeted = new(StringComparer.Ordinal);

    /// <summary>
    /// Everyone the player has spoken to in the field, each once, by a key the engine gives them (plan 08 · P12): what
    /// stands in for the original's count of people spoken to in the Underground, which wakes the Hallowed Tower's
    /// Spiritomb. Scripts read how many as the built-in variable <c>GREETINGS</c>.
    /// </summary>
    public IReadOnlyCollection<string> Greeted => greeted;

    /// <summary>Counts someone as spoken to; true the first time.</summary>
    public bool Greet(string person)
    {
        if (person.Length == 0 || !greeted.Add(person)) return false;
        Revision++;
        return true;
    }

    // ------------------------------------------------------------------ badges

    /// <summary>The badges as the save and the Trainer Card keep them: one bit each, in <see cref="Badge"/>'s order.</summary>
    public int BadgeMask { get; private set; }

    public bool HasBadge(Badge badge) => (BadgeMask & (1 << (int)badge)) != 0;

    public int BadgeCount => System.Numerics.BitOperations.PopCount((uint)BadgeMask);

    public void GiveBadge(Badge badge)
    {
        if (HasBadge(badge)) return;
        BadgeMask |= 1 << (int)badge;
        Revision++;
    }

    public void SetBadges(int mask)
    {
        mask &= 0xFF;
        if (mask == BadgeMask) return;
        BadgeMask = mask;
        Revision++;
    }

    // ------------------------------------------------------------------ the starters

    /// <summary>The three Pokémon Professor Rowan's briefcase holds.</summary>
    public static readonly string[] Starters = { "Turtwig", "Chimchar", "Piplup" };

    /// <summary>The species the player took from the briefcase; null until they have.</summary>
    public string? PlayerStarter { get; private set; }

    /// <summary>The species the rival took: as in the original, the one that is strong against the player's.</summary>
    public string? RivalStarter { get; private set; }

    /// <summary>The species the professor's assistant has: the one left in the briefcase (the original's counterpart's starter); null until the player has chosen.</summary>
    public string? AssistantStarter => PlayerStarter == null ? null : Starters.First(s => s != PlayerStarter && s != RivalStarter);

    /// <summary>The starter the rival takes when the player takes this one (grass is answered with fire, fire with water, water with grass).</summary>
    public static string RivalStarterFor(string playerStarter)
    {
        int i = Array.IndexOf(Starters, playerStarter);
        return Starters[(Math.Max(i, 0) + 1) % Starters.Length];
    }

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
        BadgeMask = 0;
        PlayerStarter = RivalStarter = null;
        Revision++;
    }

    /// <summary>What a save keeps of the story.</summary>
    public StorySnapshot Snapshot() => new(
        flags.Order(StringComparer.Ordinal).ToList(),
        variables.OrderBy(v => v.Key, StringComparer.Ordinal).ToDictionary(v => v.Key, v => v.Value),
        defeated.Order(StringComparer.Ordinal).ToList(),
        taken.Order(StringComparer.Ordinal).ToList(),
        BadgeMask, PlayerStarter, RivalStarter,
        greeted.Order(StringComparer.Ordinal).ToList());

    public void Restore(StorySnapshot saved)
    {
        Clear();
        foreach (string f in saved.Flags) flags.Add(f);
        foreach (var (name, value) in saved.Variables)
            if (value != 0) variables[name] = value;
        foreach (string t in saved.DefeatedTrainers) defeated.Add(t);
        foreach (string i in saved.TakenItems) taken.Add(i);
        foreach (string g in saved.Greeted ?? new List<string>()) greeted.Add(g);
        BadgeMask = saved.Badges & 0xFF;
        PlayerStarter = saved.PlayerStarter;
        RivalStarter = saved.RivalStarter ?? (saved.PlayerStarter != null ? RivalStarterFor(saved.PlayerStarter) : null);
        Revision++;
    }

    /// <summary>Only the flags, as saves from before <see cref="StoryState"/> kept them.</summary>
    public void Restore(IEnumerable<string> savedFlags) =>
        Restore(new StorySnapshot(savedFlags.ToList(), new Dictionary<string, int>(), new List<string>(), new List<string>(), 0, null, null));
}

/// <summary>The story as a save writes it: plain lists and numbers.</summary>
public sealed record StorySnapshot(
    List<string> Flags,
    Dictionary<string, int> Variables,
    List<string> DefeatedTrainers,
    List<string> TakenItems,
    int Badges,
    string? PlayerStarter,
    string? RivalStarter,
    List<string>? Greeted = null);
