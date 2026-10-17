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
                return sinnoh ??= Of(World.LoadAll().First(w => string.Equals(w.Index.Region, "Sinnoh", StringComparison.OrdinalIgnoreCase)),
                    ScriptLibrary.Default, Rooms(), HighestInSinnoh);
        }
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
    public static SpeciesSources Of(World world, ScriptLibrary scripts, IEnumerable<MapFile> rooms, int highest = HighestInSinnoh)
    {
        var found = Gather(world, scripts, rooms.ToList());
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

    private static Found Gather(World world, ScriptLibrary scripts, List<MapFile> rooms)
    {
        var f = new Found();

        // The open areas: their wild tables, our own species once the National Pokédex is had, their items and places
        foreach (string key in world.Index.Areas)
        {
            var area = world.Area(key);
            var overlay = world.Overlay(key);
            if (area != null)
            {
                foreach (var e in area.Land ?? new()) f.Species(e.Species, $"{key}: grass", true);
                foreach (string s in area.Day ?? new()) f.Species(s, $"{key}: grass by day", true);
                foreach (string s in area.Night ?? new()) f.Species(s, $"{key}: grass at night", true);
                foreach (string s in area.Swarm ?? new()) f.Species(s, $"{key}: a swarm", true);
                foreach (string s in area.Radar ?? new()) f.Species(s, $"{key}: the Poké Radar", true);
                foreach (var e in area.Water ?? new()) f.Species(e.Species, $"{key}: surfing", true);
                foreach (var e in area.OldRod ?? new()) f.Species(e.Species, $"{key}: the Old Rod", true);
                foreach (var e in area.GoodRod ?? new()) f.Species(e.Species, $"{key}: the Good Rod", true);
                foreach (var e in area.SuperRod ?? new()) f.Species(e.Species, $"{key}: the Super Rod", true);
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

        // The tables that belong to no one area (plan 06 · R13)
        var tables = world.Encounters;
        foreach (string s in tables.HoneyTrees.Common.Concat(tables.HoneyTrees.Uncommon).Concat(tables.HoneyTrees.Rare)) f.Species(s, "a honey tree", true);
        foreach (string s in tables.GreatMarsh.Local.Concat(tables.GreatMarsh.National)) f.Species(s, "the Great Marsh", true);
        foreach (string s in tables.TrophyGarden) f.Species(s, "the Trophy Garden", true);
        f.Species(tables.Feebas.Species, "Feebas's tiles", true);

        // What the scripts hand over, battle or sell
        bool revives = false;
        foreach (var script in scripts.All)
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
                }
            }

        // The shops, the prize counters and the Underground's walls (a thing either trainer number can dig)
        foreach (var m in MartDatabase.Common) f.Item(m.Item, "a Poké Mart");
        foreach (var (counter, list) in MartDatabase.Specialties)
            foreach (string item in list) f.Item(item, $"the counter {counter}");
        foreach (var prize in FrontierDatabase.ExchangeItems) f.Item(prize.Item, "the Battle Points' exchange");
        foreach (var (item, _) in Models.GameCornerPrizes.All) f.Item(item, "the Game Corner's prizes");
        foreach (var o in UndergroundDatabase.Objects)
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
            EvolutionMethod.LevelWithSpeciesInParty => e.Species != null && f.ways.ContainsKey(e.Species),
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
