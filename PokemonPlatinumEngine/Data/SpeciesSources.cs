using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// Every way a region's game has to come by each species with nobody else (plan 03 · D12a; decision 4: a region holds
/// every species of its own games, and the whole Pokédex can be completed with no link): the open areas' wild tables
/// (the grass at every hour, swarms, the Poké Radar, the water and the rods, and our own species of the overlays'
/// <c>national</c>), the rooms' tables, the tables that belong to no area (the honey trees, the Great Marsh, the
/// Trophy Garden, Feebas), what the scripts hand over or battle (gifts, Eggs, battles with a wild Pokémon, roamers,
/// the people's trades, the museum's fossils), and from those whatever evolves (with the items, places and partners
/// its method asks for, a trade's by a Linking Cord) or hatches from them. What is left over of the species in scope is
/// <see cref="PalParkPool"/>: Pal Park's Catching Show draws from it (D12a's stand-in for the migration, which the user
/// can overturn). GPU-free, and read from the data alone, so the test of every species and the park ask the same.
/// </summary>
public sealed class SpeciesSources
{
    /// <summary>The species Sinnoh must hold: Diamond's, Pearl's and Platinum's National Pokédex, 1 to 493.</summary>
    public const int HighestInSinnoh = 493;

    private readonly Dictionary<string, string> ways = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> items = new(StringComparer.Ordinal);
    private readonly HashSet<string> sites = new(StringComparer.Ordinal);

    /// <summary>Every species some way gives, by name, with the first way found (for a test's message).</summary>
    public IReadOnlyDictionary<string, string> Ways => ways;

    /// <summary>Every item some way gives, with the first way found.</summary>
    public IReadOnlyDictionary<string, string> Items => items;

    /// <summary>The places that let a Pokémon evolve there (<c>Magnetic Field</c>, <c>Moss Rock</c>, <c>Ice Rock</c>).</summary>
    public IReadOnlyCollection<string> Sites => sites;

    public bool Has(string species) => ways.ContainsKey(species);

    /// <summary>
    /// The species in scope that no way gives, in National order: what Pal Park's Catching Show draws from (plan 03 ·
    /// D12a). It shrinks by itself as a later session gives one of them a way of its own (the legendaries' quests, D12c).
    /// </summary>
    public IReadOnlyList<string> PalParkPool { get; }

    private SpeciesSources(IReadOnlyList<string> pool) => PalParkPool = pool;

    private static SpeciesSources? sinnoh;
    private static readonly object loading = new();

    /// <summary>Sinnoh's, read once from the game's own data and scripts.</summary>
    public static SpeciesSources Sinnoh
    {
        get
        {
            lock (loading)
            {
                if (sinnoh != null) return sinnoh;
                var worlds = World.LoadAll().ToList();
                var home = worlds.First(w => string.Equals(w.Index.Region, "Sinnoh", StringComparison.OrdinalIgnoreCase));
                // The scripts of another region's places give nothing in Sinnoh (Kanto's Mewtwo, plan 19 · K9)
                var elsewhere = worlds.Where(w => w != home).SelectMany(w => w.Index.Areas).ToHashSet(StringComparer.OrdinalIgnoreCase);
                elsewhere.ExceptWith(home.Index.Areas);
                // ... and nor do another region's hand-made rooms (Kalos's laboratory and its briefcase, plan 23 · Z2)
                var rooms = new List<MapFile>();
                foreach (var room in Rooms())
                {
                    var region = RegionDatabase.RegionOfMap(room.Name);
                    if (region == null || string.Equals(region.Id, "Sinnoh", StringComparison.OrdinalIgnoreCase)) rooms.Add(room);
                    else elsewhere.Add(room.Name);
                }
                return sinnoh = Of(home, ScriptLibrary.Default, rooms, HighestInSinnoh, file => !elsewhere.Contains(file));
            }
        }
    }

    /// <summary>The species Hoenn must hold: Ruby's, Sapphire's and Emerald's National Pokédex, 1 to 386.</summary>
    public const int HighestInHoenn = 386;

    /// <summary>
    /// Hoenn's (plan 21 · H12, part c, D12a's check for a region's own game): its open areas and their scripts alone, with
    /// the region's briefcase of three, read once. The tables of no area are Sinnoh's.
    /// </summary>
    public static SpeciesSources Hoenn => Of(RegionDatabase.Hoenn);

    /// <summary>FireRed's Kanto (plan 40 · GE1): the region's own game, as <see cref="Hoenn"/> is read.</summary>
    public static SpeciesSources Kanto => Of(RegionDatabase.Kanto);

    /// <summary>HeartGold's Johto, with HeartGold's Kanto that Johto's world holds (plan 40 · GE1).</summary>
    public static SpeciesSources Johto => Of(RegionDatabase.Johto);

    /// <summary>Black 2's Unova (plan 40 · GE1).</summary>
    public static SpeciesSources Unova => Of(RegionDatabase.Unova);

    private static readonly Dictionary<string, SpeciesSources> regions = new(StringComparer.OrdinalIgnoreCase);
    private static SpeciesSources? everywhere;
    private static List<World>? allWorlds;

    private static List<World> Worlds => allWorlds ??= World.LoadAll();

    /// <summary>The area the Bug-Catching Contest is held in (HeartGold's National Park).</summary>
    private const string BugContestPark = "national_park";

    /// <summary>
    /// A region's own game with nobody else (plan 40 · GE1, the shape plan 21 · H12c gave Hoenn's): its open areas, its
    /// rooms (a hand-made map of the region), their scripts and the common scripts named after the region
    /// (<c>common.HoennKecleonFound</c>), its professor's three, the counters its own scripts and rooms open, and no table
    /// of another region (Sinnoh's honey trees, Great Marsh, Underground, Game Corner and Battle Frontier). Sinnoh's is
    /// <see cref="Sinnoh"/>, read as plan 03 · D12a read it. Read once; what a later region's last session runs (plans 23,
    /// 33 and 34). Null-safe: a region with no world gives nothing.
    /// </summary>
    public static SpeciesSources Of(string region)
    {
        if (string.Equals(region, RegionDatabase.Sinnoh, StringComparison.OrdinalIgnoreCase)) return Sinnoh;
        lock (loading)
        {
            if (regions.TryGetValue(region, out var known)) return known;
            var world = Worlds.FirstOrDefault(w => string.Equals(w.Index.Region, region, StringComparison.OrdinalIgnoreCase));
            var own = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rooms = new List<MapFile>();
            if (world != null)
            {
                own.UnionWith(world.Index.Areas);
                own.UnionWith(world.Index.Maps.Select(m => m.Name));
                foreach (var room in Rooms())
                    if (string.Equals(RegionDatabase.RegionOfMap(room.Name)?.Id, region, StringComparison.OrdinalIgnoreCase))
                    {
                        rooms.Add(room);
                        own.Add(room.Name);
                    }
            }
            var starters = RegionDatabase.Get(region)?.Starters ?? Array.Empty<string>();
            var library = ScriptLibrary.Default;
            var scripts = Reached(library, library.All.Where(s => own.Contains(s.File) || OwnCommon(s, region)));
            var found = Gather(world == null ? new List<World>() : new List<World> { world }, null, library, rooms,
                scripts.Contains, starters.Select(s => (s, "the professor's three")), ownCounters: true);
            return regions[region] = Make(found, HighestOf(region));
        }
    }

    /// <summary>
    /// Every region's ways together (plan 40 · GE1): every world, every room, every script, Sinnoh's tables that belong to
    /// no area, every region's three and every counter. What <c>NationalSpeciesTests</c> holds the National Pokédex to.
    /// </summary>
    public static SpeciesSources Everywhere
    {
        get
        {
            lock (loading)
            {
                if (everywhere != null) return everywhere;
                var worlds = Worlds;
                var sinnohWorld = worlds.FirstOrDefault(w => string.Equals(w.Index.Region, RegionDatabase.Sinnoh, StringComparison.OrdinalIgnoreCase));
                var given = RegionDatabase.All.SelectMany(r => r.Starters.Select(s => (s, $"{r.Id}'s professor's three")));
                var found = Gather(worlds, sinnohWorld, ScriptLibrary.Default, Rooms().ToList(), _ => true, given, ownCounters: false);
                return everywhere = Make(found, PokemonDatabase.GetAll().Max(s => s.DexNumber));
            }
        }
    }

    /// <summary>
    /// The scripts given and every script they call, to the end (a region's gate house calls <c>common.BugContestDesk</c>):
    /// what a region's own scripts reach of the shared file.
    /// </summary>
    private static HashSet<Script> Reached(ScriptLibrary library, IEnumerable<Script> start)
    {
        var reached = new HashSet<Script>();
        var queue = new Queue<Script>(start);
        while (queue.Count > 0)
        {
            var script = queue.Dequeue();
            if (!reached.Add(script)) continue;
            foreach (var i in script.Everything())
                if (i.Op == Op.Call && library.Find(i.Name, script.File) is { } called && !reached.Contains(called))
                    queue.Enqueue(called);
        }
        return reached;
    }

    /// <summary>A common script that is one region's own: named after it (<c>HoennKecleonFound</c>, <c>UnovaCatchUp</c>).</summary>
    private static bool OwnCommon(Script s, string region) =>
        s.File == "common" && s.Name.StartsWith(region, StringComparison.OrdinalIgnoreCase);

    /// <summary>The highest National number of a region's own Pokédex (its pool's reach, which only Sinnoh's park reads).</summary>
    private static int HighestOf(string region)
    {
        var dexes = RegionDatabase.Get(region)?.Dexes ?? Array.Empty<string>();
        var numbers = PokemonDatabase.GetAll().Where(s => s.DexNumbers != null && dexes.Any(s.DexNumbers.ContainsKey)).Select(s => s.DexNumber).ToList();
        return numbers.Count == 0 ? 0 : numbers.Max();
    }

    /// <summary>
    /// The species of a region's own Pokédex (every regional Pokédex its file names, in their order, each species once),
    /// what its check holds to <see cref="Of(string)"/>.
    /// </summary>
    public static IReadOnlyList<string> DexOf(string region)
    {
        var list = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (string dex in RegionDatabase.Get(region)?.Dexes ?? Array.Empty<string>())
            foreach (var s in PokemonDatabase.GetAll().Where(s => s.DexNumbers != null && s.DexNumbers.ContainsKey(dex)).OrderBy(s => s.DexNumbers![dex]))
                if (seen.Add(s.Name)) list.Add(s.Name);
        return list;
    }

    /// <summary>The rooms' map files, read as the game reads them.</summary>
    public static IEnumerable<MapFile> Rooms()
    {
        string folder = GameDataFiles.PathOf(MapDatabase.Folder);
        if (!Directory.Exists(folder)) yield break;
        foreach (string path in Directory.GetFiles(folder, "*.json").OrderBy(p => p, StringComparer.Ordinal))
            yield return GameDataFiles.Load<MapFile>(Path.Combine(MapDatabase.Folder, Path.GetFileName(path)));
    }

    /// <summary>What a world, its scripts and the rooms give, for the species numbered up to <paramref name="highest"/>.</summary>
    public static SpeciesSources Of(World world, ScriptLibrary scripts, IEnumerable<MapFile> rooms, int highest = HighestInSinnoh,
        Func<string, bool>? scriptFile = null, IEnumerable<(string Species, string Way)>? given = null, bool noAreaTables = false)
    {
        var file = scriptFile ?? (_ => true);
        var found = Gather(new List<World> { world }, noAreaTables ? null : world, scripts, rooms.ToList(), s => file(s.File), given,
            ownCounters: false);
        return Make(found, highest);
    }

    private static SpeciesSources Make(Found found, int highest)
    {
        var pool = PokemonDatabase.GetAll().Where(s => s.DexNumber >= 1 && s.DexNumber <= highest && !found.ways.ContainsKey(s.Name))
            .OrderBy(s => s.DexNumber).Select(s => s.Name).ToList();
        var sources = new SpeciesSources(pool);
        foreach (var (k, v) in found.ways) sources.ways[k] = v;
        foreach (var (k, v) in found.items) sources.items[k] = v;
        foreach (string s in found.sites) sources.sites.Add(s);
        return sources;
    }

    private sealed class Found
    {
        public readonly Dictionary<string, string> ways = new(StringComparer.Ordinal);
        public readonly Dictionary<string, string> items = new(StringComparer.Ordinal);
        public readonly HashSet<string> sites = new(StringComparer.Ordinal);
        public readonly HashSet<string> wild = new(StringComparer.Ordinal);
        public bool Changed;

        public void Species(string? name, string way, bool wildOne = false)
        {
            if (string.IsNullOrEmpty(name)) return;
            var species = PokemonDatabase.Get(name) ?? PokemonDatabase.SpeciesOfForm(name);
            if (species == null) return;
            if (wildOne) wild.Add(species.Name);
            if (ways.TryAdd(species.Name, way)) Changed = true;
        }

        public void Item(string? name, string way)
        {
            if (string.IsNullOrEmpty(name)) return;
            if (items.TryAdd(name, way)) Changed = true;
        }
    }

    /// <summary>
    /// What the worlds, rooms and scripts give. <paramref name="tablesOf"/> is the world whose tables of no area count
    /// (Sinnoh's honey trees, Great Marsh, Trophy Garden and Feebas, with its Game Corner and Underground), null for none;
    /// <paramref name="ownCounters"/> counts only the counters the scripts and rooms read here open (and no Battle
    /// Points' exchange, which is Sinnoh's Battle Frontier), where false counts every counter of <c>marts.json</c>.
    /// </summary>
    private static Found Gather(IReadOnlyList<World> worlds, World? tablesOf, ScriptLibrary scripts, List<MapFile> rooms,
        Func<Script, bool> scriptOf, IEnumerable<(string Species, string Way)>? given, bool ownCounters)
    {
        var f = new Found();
        // What the region hands over by no script (its briefcase of three)
        foreach (var (species, way) in given ?? Array.Empty<(string, string)>()) f.Species(species, way);

        // The open areas: their wild tables, our own species once the National Pokédex is had, their items and places
        var openAreas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var world in worlds)
        {
            // A region's outbreaks, its game's television one and ours (plan 38 · EX1): a species met in the grass of the row's place
            foreach (var row in (world.Encounters.Outbreaks ?? new()).Concat(world.OwnOutbreaks.Rows))
                f.Species(row.Species, $"{row.Area}: an outbreak", true);
        }
        foreach (var world in worlds)
        foreach (string key in world.Index.Areas)
        {
            openAreas.Add(key);
            var area = world.Area(key);
            var overlay = world.Overlay(key);
            if (area != null)
            {
                foreach (var e in area.Land ?? new()) f.Species(e.Species, $"{key}: grass", true);
                // (HeartGold's morning slot is the entry's own species; its day and night ones are beside it, plan 20 · J2)
                foreach (var e in area.Land ?? new()) f.Species(e.Day, $"{key}: grass by day", true);
                foreach (var e in area.Land ?? new()) f.Species(e.Night, $"{key}: grass at night", true);
                // Black 2's dark grass (plan 22 · U4) and the seasons' tables; the shaking grass and rippling water wait
                // for plan 10 · F12's patches, so they give nothing yet
                foreach (var e in area.DarkGrass ?? new()) f.Species(e.Species, $"{key}: dark grass", true);
                foreach (var s in area.Seasons ?? new())
                {
                    foreach (var e in s.Land ?? new()) f.Species(e.Species, $"{key}: grass in {s.Season}", true);
                    foreach (var e in s.DarkGrass ?? new()) f.Species(e.Species, $"{key}: dark grass in {s.Season}", true);
                    foreach (var e in s.Water ?? new()) f.Species(e.Species, $"{key}: surfing in {s.Season}", true);
                    foreach (var e in s.SuperRod ?? new()) f.Species(e.Species, $"{key}: the Super Rod in {s.Season}", true);
                }
                // HeartGold's swarms on the water and on a rod, and the Pokégear's two Sounds (plan 20 · J3, J13)
                f.Species(area.SurfSwarm, $"{key}: a swarm on the water", true);
                f.Species(area.FishSwarm, $"{key}: a swarm on a rod", true);
                foreach (string s in area.HoennSounds ?? new()) f.Species(s, $"{key}: the Hoenn Sound", true);
                foreach (string s in area.SinnohSounds ?? new()) f.Species(s, $"{key}: the Sinnoh Sound", true);
                foreach (var e in (area.LandSets ?? new()).SelectMany(s => s)) f.Species(e.Species, $"{key}: grass on its day", true);
                foreach (string s in area.Day ?? new()) f.Species(s, $"{key}: grass by day", true);
                foreach (string s in area.Night ?? new()) f.Species(s, $"{key}: grass at night", true);
                foreach (string s in area.Swarm ?? new()) f.Species(s, $"{key}: a swarm", true);
                foreach (string s in area.Radar ?? new()) f.Species(s, $"{key}: the Poké Radar", true);
                foreach (var e in area.Water ?? new()) f.Species(e.Species, $"{key}: surfing", true);
                foreach (var e in area.OldRod ?? new()) f.Species(e.Species, $"{key}: the Old Rod", true);
                foreach (var e in area.GoodRod ?? new()) f.Species(e.Species, $"{key}: the Good Rod", true);
                foreach (var e in area.SuperRod ?? new()) f.Species(e.Species, $"{key}: the Super Rod", true);
                // (a rock smashed: Hoenn's, plan 21 · H4; plan 21 · H12, part c counts it)
                foreach (var e in area.RockSmash ?? new()) f.Species(e.Species, $"{key}: a rock smashed", true);
                var heldBack = new HashSet<string>(overlay?.HeldBack ?? new(), StringComparer.Ordinal);
                foreach (var o in area.Objects)
                    if (o.Item != null && !heldBack.Contains(o.Id)) f.Item(o.Item, $"{key}: on the ground");
                foreach (var s in area.Signs)
                    if (s.Item != null) f.Item(s.Item, $"{key}: hidden");
            }
            foreach (string s in overlay?.National?.Land ?? new()) f.Species(s, $"{key}: grass with the National Pokédex", true);
            foreach (string s in overlay?.National?.SuperRod ?? new()) f.Species(s, $"{key}: the Super Rod with the National Pokédex", true);
            foreach (string site in overlay?.EvolutionSites ?? new()) f.sites.Add(site);
            foreach (var n in overlay?.Npcs ?? new())
                if (n.Item != null && n.NpcType == "ItemBall") f.Item(n.Item, $"{key}: on the ground");
        }

        // The rooms: their own wild Pokémon, items and places
        foreach (var room in rooms)
        {
            foreach (var e in room.WildEncounters) f.Species(e.SpeciesName, $"{room.Name}: wild", true);
            foreach (var n in room.Npcs)
                if (n.Item != null && n.NpcType == "ItemBall") f.Item(n.Item, $"{room.Name}: on the ground");
            foreach (var h in room.HiddenItems ?? new()) f.Item(h.Item, $"{room.Name}: hidden");
            foreach (string site in room.EvolutionSites ?? new()) f.sites.Add(site);
        }

        // Black 2's Hidden Grottoes (plan 22 · U11), in the open areas they open from
        if (worlds.Any(w => string.Equals(w.Index.Region, RegionDatabase.Unova, StringComparison.OrdinalIgnoreCase)))
            foreach (var g in HiddenGrottoes.All.Where(g => openAreas.Contains(g.Area)))
                foreach (var e in g.Table)
                {
                    if (e.IsPokemon) f.Species(e.Species, $"{g.Area}: a Hidden Grotto", true);
                    else f.Item(e.Item, $"{g.Area}: a Hidden Grotto");
                }

        // The tables that belong to no one area (plan 06 · R13)
        if (tablesOf != null)
        {
            var tables = tablesOf.Encounters;
            foreach (string s in tables.HoneyTrees.Common.Concat(tables.HoneyTrees.Uncommon).Concat(tables.HoneyTrees.Rare)) f.Species(s, "a honey tree", true);
            foreach (string s in tables.GreatMarsh.Local.Concat(tables.GreatMarsh.National)) f.Species(s, "the Great Marsh", true);
            foreach (string s in tables.TrophyGarden) f.Species(s, "the Trophy Garden", true);
            f.Species(tables.Feebas.Species, "Feebas's tiles", true);
        }

        // What the scripts hand over, battle or sell
        bool revives = false;
        var counters = new HashSet<string>(StringComparer.Ordinal);
        foreach (var script in scripts.All.Where(scriptOf))
            foreach (var i in script.Everything())
            {
                string where = script.FullName;
                switch (i.Op)
                {
                    case Op.GivePokemon: f.Species(i.Name, $"{where}: a gift"); break;
                    case Op.GiveEgg: f.Species(i.Name, $"{where}: an Egg"); break;
                    case Op.WildBattle: f.Species(i.Name, $"{where}: met"); break;
                    case Op.Roamer: f.Species(i.Name, $"{where}: roaming"); break;
                    case Op.Trade: f.Species(NpcTrades.Get(i.Name)?.Gives, $"{where}: a trade"); break;
                    case Op.Give or Op.AddItem or Op.Find: f.Item(i.Name, $"{where}: given"); break;
                    case Op.Fossil when i.Name == "revive": revives = true; break;
                    case Op.Shop when !string.IsNullOrEmpty(i.Name): counters.Add(i.Name); break;
                    // The later battle places' prize counters, for Battle Points (plan 23's Kalos: the Battle Maison's)
                    case Op.Frontier when i.Name == "prize" && i.Other != null && Models.Facilities.FacilityPrizes.Places.Contains(i.Other):
                        foreach (var p in Models.Facilities.FacilityPrizes.Of(i.Other)) f.Item(p.Item, $"{where}: the prize counter");
                        break;
                    // Join Avenue's shops, at every rank (plan 22 · U11)
                    case Op.JoinAvenue when i.Name == "shop":
                        for (int rank = 0; rank <= JoinAvenue.TopRank; rank++)
                            if (JoinAvenue.StockOf(i.Other ?? "", rank) is { } stock) counters.Add(stock);
                        break;
                    // The Bug-Catching Contest's catch is kept (plan 15 · E5), every day's table, where its park is open
                    // (the desk is a script of the shared file, which Sinnoh's own game reads whole)
                    case Op.BugContest when openAreas.Contains(BugContestPark):
                        foreach (var slot in BugContestVenue.NationalPark.Encounters.SelectMany(t => t))
                            f.Species(slot.Species, $"{where}: the Bug-Catching Contest", true);
                        // ... and its prizes (plan 40 · GE3): the first prize's stones (the Sun Stone, and one of ten drawn
                        // once the National Pokédex is had) and the three below it, BugContest.PrizeFor
                        for (int stone = 0; stone < BugContest.Stones.Length; stone++)
                            f.Item(BugContest.PrizeFor(0, stone), $"{where}: the Bug-Catching Contest's first prize");
                        for (int placement = 1; placement <= 3; placement++)
                            f.Item(BugContest.PrizeFor(placement, 0), $"{where}: the Bug-Catching Contest's prizes");
                        break;
                }
            }
        foreach (var room in rooms)
            foreach (var n in room.Npcs)
                if (!string.IsNullOrEmpty(n.Mart)) counters.Add(n.Mart);

        // The shops, the prize counters and the Underground's walls (a thing either trainer number can dig)
        foreach (var m in MartDatabase.Common) f.Item(m.Item, "a Poké Mart");
        foreach (var (counter, list) in MartDatabase.Specialties)
            if (!ownCounters || counters.Contains(counter))
                foreach (string item in list) f.Item(item, $"the counter {counter}");
        if (!ownCounters) foreach (var prize in FrontierDatabase.ExchangeItems) f.Item(prize.Item, "the Battle Points' exchange");
        // (Veilstone's Game Corner and the Underground are Sinnoh's: a region's own game asked without them has none)
        if (tablesOf != null) foreach (var (item, _) in Models.GameCornerPrizes.All) f.Item(item, "the Game Corner's prizes");
        if (tablesOf != null) foreach (var o in UndergroundDatabase.Objects)
            if (o.Odd + o.OddNational > 0 && o.Even + o.EvenNational > 0
                && Enumerable.Range(1, UndergroundDatabase.TreasureEnd - 1).Select(UndergroundDatabase.Treasure).FirstOrDefault(t => t?.Id == o.Id) is { Item: { } dug })
                f.Item(dug, "the Underground's walls");

        // Whatever evolves, hatches, is revived from a fossil or is carried by a wild Pokémon, until nothing more comes
        do
        {
            f.Changed = false;
            if (revives)
                foreach (var (item, species) in Fossils.All)
                    if (f.items.ContainsKey(item)) f.Species(species, $"the museum: {item}");
            foreach (string name in f.wild.ToList())
                if (PokemonDatabase.Get(name)?.WildItems is { } held)
                {
                    f.Item(held.Common, $"held by a wild {name}");
                    f.Item(held.Rare, $"held by a wild {name}");
                }
            foreach (string name in f.ways.Keys.ToList())
            {
                var species = PokemonDatabase.Get(name);
                if (species == null) continue;
                foreach (var e in species.Evolutions ?? new())
                    if (e.Region == null && e.FromForm == null && CanEvolve(e, f))
                        f.Species(e.TargetSpecies, $"evolves from {name} ({e.Method})");
                if (species.Offspring != null && Breeds(species, f)) f.Species(species.Offspring, $"hatches from {name}'s Egg");
                // Manaphy's own Egg is a Phione's, as Breeding.EggSpecies has it (plan 29 · LS11a gives Manaphy a way)
                if (name == "Manaphy" && Breeds(species, f)) f.Species("Phione", "hatches from Manaphy's Egg");
            }
        } while (f.Changed);
        return f;
    }

    /// <summary>Whether an evolution can happen with nobody else: what its method asks for is to be had (a trade's is a Linking Cord).</summary>
    private static bool CanEvolve(EvolutionData e, Found f)
    {
        bool Item() => e.Item != null && f.items.ContainsKey(e.Item);
        bool Cord() => f.items.ContainsKey(Evolution.LinkingCord);
        return e.Method switch
        {
            EvolutionMethod.UseItem or EvolutionMethod.UseItemMale or EvolutionMethod.UseItemFemale or EvolutionMethod.UseItemNight
                or EvolutionMethod.LevelHoldingItemDay or EvolutionMethod.LevelHoldingItemNight or EvolutionMethod.SpinHoldingItem
                or EvolutionMethod.LevelWithItemsInBag => Item(),
            EvolutionMethod.Trade => Cord(),
            EvolutionMethod.TradeHoldingItem => Cord() && Item(),
            EvolutionMethod.TradeWithSpecies => Cord() && e.Species != null && f.ways.ContainsKey(e.Species),
            EvolutionMethod.LevelWithSpeciesInParty => e.Species != null && f.ways.ContainsKey(e.Species)
                || e.AnyOfSpecies?.Any(f.ways.ContainsKey) == true,
            EvolutionMethod.LevelAtLocation => e.Location != null && f.sites.Contains(e.Location),
            EvolutionMethod.Other => false,
            _ => true
        };
    }

    /// <summary>Whether a species lays Eggs: in some group but the Undiscovered one, with a Ditto to hand where it needs one.</summary>
    private static bool Breeds(PokemonSpecies species, Found f)
    {
        var groups = species.EggGroups ?? new();
        if (groups.Count == 0 || groups.Contains("Undiscovered")) return false;
        if (groups.Contains("Ditto")) return false;
        // A species of one gender or none lays Eggs only beside a Ditto (a female alone carries its line)
        bool needsDitto = species.GenderRatio is -1 or 0;
        return !needsDitto || f.ways.ContainsKey("Ditto");
    }
}
