using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// What a randomised run touches (plan 13 · V6), chosen once at NEW GAME and kept whole in <see cref="RunRules.Random"/>:
/// the seed and which halves are on. A seed (0 to <see cref="MaxSeed"/>) makes the same game on every machine.
/// </summary>
public sealed record RandomizerOptions
{
    /// <summary>The largest seed: nine digits, which the title's keyboard can type.</summary>
    public const int MaxSeed = 999_999_999;

    public int Seed { get; init; }

    /// <summary>Wild Pokémon, trainers' teams, gifts, static encounters and the starters are other species.</summary>
    public bool Species { get; init; } = true;

    /// <summary>Items on the ground and in the hands of people who give them are other items.</summary>
    public bool Items { get; init; } = true;
}

/// <summary>
/// The randomiser (plan 13 · V6; <c>docs/mechanics/rulings.md</c>, "The randomiser"): a deterministic map from the game's
/// species and items to others, decided by hashing the seed with the thing's number or place, <b>never by chance</b>. It
/// edits no data, draws nothing from <see cref="Dice"/> and has no state a run can change, so one seed is one mapping
/// on every machine, a Normal run never builds one (<see cref="For"/> answers null) and the golden battles are as they
/// were. GPU-free.
///
/// <para><b>Species.</b> A species goes to another of the same kind of place in its line (a lone species, a first, a
/// middle or a last stage), legendaries among legendaries, within a tenth of its base-stat total, the margin widening
/// until a few are found. A line is mapped from its first stage so it stays a line: the evolution of a species goes to
/// one of the evolutions of what its pre-evolution went to, where there is one. The three Pokémon of each briefcase go
/// to three first stages of three different lines. What the data holds is never changed: the map is asked at the
/// point of use (<c>StartWildBattle</c>, <c>StartTrainerBattle</c>, a script's gift or static battle, the briefcase).</para>
///
/// <para><b>Items.</b> An item on the ground, hidden or handed over goes to another of the same pocket and about the same
/// price, decided for each ball by its own flag. Key items, HMs, anything a script asks the bag for and the later games'
/// Mega Stones and Z-Crystals are never swapped nor given instead.</para>
/// </summary>
public sealed class Randomizer
{
    // ------------------------------------------------------------------ one randomiser to a seed

    private static readonly object Gate = new();
    private static (RandomizerOptions Options, Randomizer Made)? last;

    /// <summary>The randomiser of a run: null for a run that randomises nothing, which is every run but a chosen one.</summary>
    public static Randomizer? For(RunRules? run) => run?.Random is { } options ? Of(options) : null;

    /// <summary>The randomiser of these options (built once for the options last asked).</summary>
    public static Randomizer Of(RandomizerOptions options)
    {
        lock (Gate)
        {
            if (last is { } held && held.Options == options) return held.Made;
            var made = new Randomizer(options);
            last = (options, made);
            return made;
        }
    }

    private Randomizer(RandomizerOptions options)
    {
        Options = options;
        seed = SplitMix.Fnv("randomizer:" + options.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    public RandomizerOptions Options { get; }

    private readonly uint seed;

    public bool MapsSpecies => Options.Species;
    public bool MapsItems => Options.Items;

    /// <summary>The seed as the title and the cards write it: nine digits at most, no separators.</summary>
    public static string SeedText(int seed) => Math.Clamp(seed, 0, RandomizerOptions.MaxSeed).ToString(System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>A seed typed on the keyboard: its digits (anything else is passed over), at most <see cref="RandomizerOptions.MaxSeed"/>; null for none.</summary>
    public static int? ParseSeed(string? typed)
    {
        if (string.IsNullOrEmpty(typed)) return null;
        long value = 0;
        bool any = false;
        foreach (char c in typed)
        {
            if (c < '0' || c > '9') continue;
            any = true;
            value = Math.Min(RandomizerOptions.MaxSeed, value * 10 + (c - '0'));
        }
        return any ? (int)value : null;
    }

    // ------------------------------------------------------------------ the species

    /// <summary>Where a species stands in its line.</summary>
    private enum Place { Lone, First, Middle, Last }

    private sealed record Entry(PokemonSpecies Species, int Total, Place Place, bool Special, string? Pre, IReadOnlyList<string> Next)
    {
        public string Name => Species.Name;
        public int Dex => Species.DexNumber;
    }

    /// <summary>The games' species with the facts the mapping goes by, in National order. Independent of the rules in force: Platinum's base stats.</summary>
    private static readonly Lazy<IReadOnlyList<Entry>> Everything = new(BuildEntries);

    private static readonly Lazy<Dictionary<string, Entry>> ByName = new(() => Everything.Value.ToDictionary(e => e.Name, StringComparer.OrdinalIgnoreCase));

    private static IReadOnlyList<Entry> BuildEntries()
    {
        var games = PokemonDatabase.GetAll().OrderBy(s => s.DexNumber).ToList();
        var names = games.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        // Who evolves from whom, among the games' own (a species of ours that evolves past 100 is nobody's stage here)
        var pre = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var next = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in games)
            foreach (var e in s.Evolutions ?? new())
            {
                if (!names.Contains(e.TargetSpecies) || string.Equals(e.TargetSpecies, s.Name, StringComparison.OrdinalIgnoreCase)) continue;
                var list = next.TryGetValue(s.Name, out var l) ? l : next[s.Name] = new List<string>();
                if (!list.Contains(e.TargetSpecies, StringComparer.OrdinalIgnoreCase)) list.Add(e.TargetSpecies);
                pre.TryAdd(e.TargetSpecies, s.Name);
            }
        var entries = new List<Entry>();
        foreach (var s in games)
        {
            var platinum = s.Under(Ruleset.Platinum);
            int total = platinum.BaseHP + platinum.BaseAttack + platinum.BaseDefense + platinum.BaseSpAttack + platinum.BaseSpDefense + platinum.BaseSpeed;
            bool hasPre = pre.ContainsKey(s.Name), hasNext = next.ContainsKey(s.Name);
            var place = hasPre ? hasNext ? Place.Middle : Place.Last : hasNext ? Place.First : Place.Lone;
            var after = next.TryGetValue(s.Name, out var targets)
                ? targets.OrderBy(t => PokemonDatabase.Get(t)!.DexNumber).ToList() : new List<string>();
            entries.Add(new Entry(s, total, place, s.Legendary || s.Mythical, hasPre ? pre[s.Name] : null, after));
        }
        return entries;
    }

    private readonly Dictionary<string, string> mapped = new(StringComparer.OrdinalIgnoreCase);
    private Dictionary<string, string>? starters;
    private Dictionary<string, List<string>>? sources;

    /// <summary>
    /// The species a species is, in this run: itself when species aren't randomised, when the name is not one of the games'
    /// species or when nothing else fits. Always an existing species.
    /// </summary>
    public string Species(string name)
    {
        if (!Options.Species || !ByName.Value.ContainsKey(name)) return name;
        lock (mapped)
        {
            return MapOf(ByName.Value[name].Name, 0);
        }
    }

    /// <summary>The species as a species.</summary>
    public PokemonSpecies Species(PokemonSpecies species) =>
        Options.Species && PokemonDatabase.Get(Species(species.Name)) is { } to ? to : species;

    /// <summary>A gift or a static battle's species: as <see cref="Species(string)"/>, unless a script asks the team for it (<see cref="ScriptLibrary.SpeciesAsked"/>), which could then never be answered.</summary>
    public string Given(string name) => ScriptLibrary.Default.SpeciesAsked.Contains(name) ? name : Species(name);

    private string MapOf(string name, int depth)
    {
        if (mapped.TryGetValue(name, out var done)) return done;
        var e = ByName.Value[name];
        string? result = null;
        if (StarterMap.TryGetValue(e.Name, out var starter)) result = starter;
        // A line stays a line: an evolution is one of the evolutions of what its pre-evolution went to
        else if (e.Pre != null && depth < 4)
        {
            var gone = ByName.Value[MapOf(e.Pre, depth + 1)];
            if (gone.Next.Count > 0) result = gone.Next[(int)(SplitMix.Mix(seed, (uint)e.Dex, 0x45564Fu) % (uint)gone.Next.Count)];
        }
        result ??= Choose(e, Array.Empty<string>());
        return mapped[name] = result;
    }

    /// <summary>The first of the line a species is a stage of.</summary>
    private static Entry RootOf(Entry e)
    {
        for (int guard = 0; e.Pre != null && guard < 6; guard++) e = ByName.Value[e.Pre];
        return e;
    }

    /// <summary>
    /// A species of the same place in its line and kind, of about the same total: the margin starts at a tenth and widens by
    /// a tenth until at least four are found (or there is no more room), then one is taken by the seed. None of
    /// <paramref name="avoid"/> nor its own line's, and not the species itself unless nothing else is left.
    /// </summary>
    private string Choose(Entry e, IReadOnlyCollection<string> avoid)
    {
        var lines = avoid.Select(a => RootOf(ByName.Value[a]).Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (bool samePlace in new[] { true, false })
        {
            var pool = Everything.Value.Where(c => c.Special == e.Special && (!samePlace || c.Place == e.Place)
                && !string.Equals(c.Name, e.Name, StringComparison.OrdinalIgnoreCase) && !lines.Contains(RootOf(c).Name)).ToList();
            for (int margin = 10; margin <= 100; margin += 10)
            {
                int low = e.Total * (100 - margin) / 100, high = e.Total * (100 + margin) / 100;
                var near = pool.Where(c => c.Total >= low && c.Total <= high).ToList();
                if (near.Count < 4 && margin < 100) continue;
                if (near.Count == 0) break;
                return near[(int)(SplitMix.Mix(seed, (uint)e.Dex, 0x53504Fu) % (uint)near.Count)].Name;
            }
        }
        return e.Name;
    }

    /// <summary>
    /// The species each briefcase's slots go to: three first stages of three different lines for the three, whichever
    /// region's briefcase it is. Decided once, in the regions' order, so what the rival and the assistant take follows.
    /// </summary>
    private Dictionary<string, string> StarterMap
    {
        get
        {
            if (starters != null) return starters;
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var region in RegionDatabase.All)
            {
                var taken = new List<string>();
                foreach (string slot in region.Starters)
                {
                    if (!ByName.Value.TryGetValue(slot, out var e) || map.ContainsKey(e.Name)) continue;
                    // A first stage that has a line to grow into, of about the slot's total
                    var start = e with { Place = Place.First };
                    string pick = Choose(start, taken);
                    taken.Add(pick);
                    map[e.Name] = pick;
                }
            }
            return starters = map;
        }
    }

    /// <summary>A briefcase's slots as they are in this run: the same species when species aren't randomised.</summary>
    public IReadOnlyList<string> Starters(IReadOnlyList<string> slots) =>
        slots.Select(s => Options.Species && StarterMap.TryGetValue(s, out var to) ? to : s).ToList();

    /// <summary>
    /// The species of the games that become this one: the reverse of <see cref="Species(string)"/>, for the Pokédex's area
    /// page, which shows where a species lives by where the species that stand for it live. Includes the species itself
    /// where nothing changes it.
    /// </summary>
    public IReadOnlyList<string> Sources(string species)
    {
        if (!Options.Species) return new[] { species };
        lock (mapped)
        {
            if (sources == null)
            {
                sources = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in Everything.Value)
                {
                    string to = MapOf(e.Name, 0);
                    if (!sources.TryGetValue(to, out var list)) sources[to] = list = new List<string>();
                    list.Add(e.Name);
                }
            }
            return sources.TryGetValue(species, out var from) ? from : Array.Empty<string>();
        }
    }

    /// <summary>
    /// How many species of Sinnoh's Pokédex the professor counts as seen: the Pokédex's own count, except that a run that
    /// randomises species (which meets other species than the ones Sinnoh's Pokédex lists, so can never see them all) is
    /// counted as having seen every one once its Hall of Fame is entered, which is what opens the National Pokédex.
    /// </summary>
    public static int SinnohSeen(RunRules? run, Pokedex pokedex, StoryState story)
    {
        int seen = pokedex.SeenIn(Pokedex.Sinnoh);
        return For(run) is { MapsSpecies: true } && story.IsRegionComplete(RegionDatabase.Sinnoh)
            ? Math.Max(seen, Pokedex.Entries(Pokedex.Sinnoh).Count) : seen;
    }

    // ------------------------------------------------------------------ the trainers' teams

    /// <summary>Whether a trainer's team is left as it is: a Master Trainer's is one species by design, and an empty team has nothing to change.</summary>
    private static bool Unchanged(Trainer trainer) => trainer.Party.Count == 0 || trainer.Id.StartsWith("master_", StringComparison.Ordinal);

    /// <summary>
    /// The trainer as a battle with them is fought in this run: the very object when species aren't randomised or no member
    /// changes, otherwise a copy whose Pokémon are the mapped species at the same levels, with the same personality,
    /// IVs, held item and capsule, knowing what their level teaches (the moves chosen for the original don't fit another
    /// species). The trainer of the map keeps their team.
    /// </summary>
    public Trainer ForTrainer(Trainer trainer)
    {
        if (!Options.Species || Unchanged(trainer)) return trainer;
        if (trainer.Party.Members.All(p => Species(p.Species.Name) == p.Species.Name)) return trainer;
        return trainer.WithTeamOf(Reborn);
    }

    /// <summary>A Pokémon as the species it goes to: itself when nothing changes it.</summary>
    public Pokemon Reborn(Pokemon p)
    {
        if (p.IsEgg || !Options.Species) return p.Clone();
        var to = Species(p.Species);
        if (ReferenceEquals(to, p.Species) || to.Name == p.Species.Name) return p.Clone();
        var rng = new Random((int)(SplitMix.Mix(seed, (uint)p.Species.DexNumber, (uint)p.Level) & 0x7FFFFFFF));
        var born = new Pokemon(to, p.Level, p.Personality, rng)
        {
            IsShiny = false,
            IvHP = p.IvHP, IvAttack = p.IvAttack, IvDefense = p.IvDefense, IvSpAttack = p.IvSpAttack, IvSpDefense = p.IvSpDefense, IvSpeed = p.IvSpeed,
            HeldItem = p.HeldItem,
            CapsuleId = p.CapsuleId,
            Capsule = p.Capsule
        };
        born.RecalculateStats();
        born.CurrentHP = born.MaxHP;
        return born;
    }

    // ------------------------------------------------------------------ the items

    private HashSet<string>? protectedItems;
    private Dictionary<ItemPocket, List<ItemData>>? itemPools;
    private readonly Dictionary<string, ItemData> itemsMapped = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>The last item of Platinum's own table: what is handed out in place of another is one of these, which the game runs in full.</summary>
    public const int LastPlatinumItem = 467;

    /// <summary>Whether an item is left alone, in either direction: a key item, an HM, a Mega Stone or Z-Crystal, anything a script asks the bag for, and anything that can't be tossed.</summary>
    public bool Protected(ItemData item)
    {
        protectedItems ??= ScriptLibrary.Default.ItemsAsked.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return item.Pocket == ItemPocket.KeyItems || item.CantBeTossed || item.MegaStone != null || item.ZCrystal != null
            || IsHm(item) || protectedItems.Contains(item.Name);
    }

    private static bool IsHm(ItemData item) => item.Pocket == ItemPocket.TMsAndHMs && item.Name.StartsWith("HM", StringComparison.OrdinalIgnoreCase);

    private Dictionary<ItemPocket, List<ItemData>> Pools
    {
        get
        {
            if (itemPools != null) return itemPools;
            var pools = new Dictionary<ItemPocket, List<ItemData>>();
            foreach (var item in ItemDatabase.GetAll().Where(i => i.Id <= LastPlatinumItem && !Protected(i)).OrderBy(i => i.Id))
            {
                if (!pools.TryGetValue(item.Pocket, out var list)) pools[item.Pocket] = list = new List<ItemData>();
                list.Add(item);
            }
            return itemPools = pools;
        }
    }

    /// <summary>
    /// What an item found or given at a place is, in this run: the item itself when items aren't randomised or it is
    /// <see cref="Protected"/>, otherwise another of its pocket and about its price (a TM another TM, the margin a half to
    /// twice, widening until a few are found), decided by the seed and <paramref name="place"/> (a ball's own flag, a line of a
    /// script), so the same ball is the same item every time it is asked and a different ball another.
    /// </summary>
    public ItemData Item(ItemData item, string place)
    {
        if (!Options.Items || Protected(item) || !Pools.TryGetValue(item.Pocket, out var pool)) return item;
        string key = place + "|" + item.Name;
        lock (itemsMapped)
        {
            if (itemsMapped.TryGetValue(key, out var done)) return done;
            var others = pool.Where(c => c.Id != item.Id).ToList();
            List<ItemData> near = others;
            if (item.Pocket != ItemPocket.TMsAndHMs)
            {
                near = new List<ItemData>();
                foreach (double margin in new[] { 2.0, 3.0, 5.0, 10.0, 1000.0 })
                {
                    near = others.Where(c => item.Price == 0 ? c.Price == 0 : c.Price > 0 && c.Price >= item.Price / margin && c.Price <= item.Price * margin).ToList();
                    if (near.Count >= 4) break;
                }
            }
            var chosen = near.Count == 0 ? item : near[(int)(SplitMix.Mix(seed, SplitMix.Fnv(key), (uint)item.Id) % (uint)near.Count)];
            return itemsMapped[key] = chosen;
        }
    }

    /// <summary>How many of the item turn up when an item is swapped for another: the same number, except one TM at a time.</summary>
    public static int CountOf(ItemData original, ItemData swapped, int count) =>
        swapped.Pocket == ItemPocket.TMsAndHMs && original.Pocket != ItemPocket.TMsAndHMs ? 1 : count;
}
