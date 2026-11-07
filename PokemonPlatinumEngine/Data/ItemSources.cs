using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Frontier;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Data;

/// <summary>The kinds of way the data and the scripts give an item (plan 36 · TR3, <see cref="ItemSources"/>).</summary>
public enum ItemSourceKind
{
    /// <summary>A counter sells it for money: the Poké Marts' common list, a specialty counter's (<c>marts.json</c>).</summary>
    Stock,
    /// <summary>A prize list trades it for points or coins: the Battle Points lists, the Game Corner's, the Battle Subway's.</summary>
    Prize,
    /// <summary>It lies in a ball: an area's object, an overlay's or a room's.</summary>
    Ball,
    /// <summary>It is hidden in the ground: an area's sign, a room's hidden item.</summary>
    Hidden,
    /// <summary>A script hands it over (<c>give</c>, <c>additem</c>, a designer's <c>charm give</c>: plan 40 · GE5).</summary>
    Given,
    /// <summary>A script finds it (<c>find</c>).</summary>
    Found,
    /// <summary>
    /// A Pokémon a script hands over holds it (plan 40 · GE11): <c>givepokemon … holding</c> (the Tower of Mastery's
    /// Lucario and its Lucarionite). A Pokémon met holding one (<c>wildbattle … holding</c>) is no sure way: it must be caught.
    /// </summary>
    Held,
    /// <summary>A person's in-game trade sends it with its Pokémon (<c>trades.json</c>'s <c>item</c>, by the script's <c>trade</c>).</summary>
    Traded
}

/// <summary>One way an item is had: its kind and where (an area, a room, a script, a counter, a list).</summary>
public sealed record ItemSource(ItemSourceKind Kind, string Where);

/// <summary>
/// Every way the data and the scripts give each item (plan 36 · TR3, the seam plan 40 · GE11 widens to every item the rules
/// run): the counters' stocks, the prize lists the scripts open, the balls of the areas, overlays and rooms, what is hidden
/// in them, and what the scripts give or find. <see cref="In(string)"/> is one region's own game: its open areas, its
/// rooms, the scripts of its places with every script they call and the common scripts its people name (a training
/// hall's clerk, <c>common.TrainingClerk</c>), the counters they open and the Poké Marts' common list; <see cref="Everywhere"/>
/// is every world, room, script and list. What gives an item only by chance (a wild Pokémon's held item, the Underground's
/// walls, a Hidden Grotto) is not counted: a tool's test asks for a sure way. Plan 40 · GE11, which holds every item the
/// rules run to a way (<c>ItemSourcesTests</c>), added two sure kinds: what a Pokémon given holds
/// (<see cref="ItemSourceKind.Held"/>) and what a trade sends (<see cref="ItemSourceKind.Traded"/>). Plan 36 · TR9's later
/// machines needed no kind of their own: their shelf is a counter's stock (<c>marts.json</c>'s <c>training_hall_machines</c>)
/// that the training clerk's <c>shop</c> opens, read as any other. GPU-free, read from the data alone, once.
/// </summary>
public sealed class ItemSources
{
    private readonly Dictionary<string, List<ItemSource>> ways = new(StringComparer.Ordinal);

    /// <summary>Every item some way gives.</summary>
    public IReadOnlyCollection<string> Items => ways.Keys;

    /// <summary>Every way the item is had, in the order found; none for an item nothing gives.</summary>
    public IReadOnlyList<ItemSource> Of(string item) => ways.TryGetValue(item, out var list) ? list : Array.Empty<ItemSource>();

    public bool Has(string item) => ways.ContainsKey(item);

    public bool Has(string item, ItemSourceKind kind) => Of(item).Any(s => s.Kind == kind);

    private void Add(string? item, ItemSourceKind kind, string where)
    {
        if (string.IsNullOrEmpty(item) || ItemDatabase.Get(item) == null) return;
        if (!ways.TryGetValue(item, out var list)) ways[item] = list = new List<ItemSource>();
        if (!list.Any(s => s.Kind == kind && s.Where == where)) list.Add(new ItemSource(kind, where));
    }

    private static readonly object loading = new();
    private static readonly Dictionary<string, ItemSources> regions = new(StringComparer.OrdinalIgnoreCase);
    private static ItemSources? everywhere;
    private static List<World>? worlds;
    private static List<MapFile>? rooms;

    private static List<World> Worlds => worlds ??= World.LoadAll();
    private static List<MapFile> Rooms => rooms ??= SpeciesSources.Rooms().ToList();

    /// <summary>One region's own game, read once (Sinnoh's, Kanto's...); a region with no world gives nothing.</summary>
    public static ItemSources In(string region)
    {
        lock (loading)
        {
            if (regions.TryGetValue(region, out var known)) return known;
            var world = Worlds.FirstOrDefault(w => string.Equals(w.Index.Region, region, StringComparison.OrdinalIgnoreCase));
            var sources = new ItemSources();
            if (world != null)
            {
                var own = new HashSet<string>(world.Index.Areas, StringComparer.OrdinalIgnoreCase);
                own.UnionWith(world.Index.Maps.Select(m => m.Name));
                var ownRooms = Rooms.Where(r => string.Equals(RegionDatabase.RegionOfMap(r.Name)?.Id, region, StringComparison.OrdinalIgnoreCase)).ToList();
                own.UnionWith(ownRooms.Select(r => r.Name));
                // The common scripts the region's own people run (a training hall's clerk, a counter's attendant)
                var named = new HashSet<string>(StringComparer.Ordinal);
                foreach (string key in world.Index.Areas)
                    if (world.Overlay(key) is { } overlay)
                    {
                        foreach (var p in overlay.People?.Values ?? Enumerable.Empty<OverlayPerson>()) if (p.Script != null) named.Add(p.Script);
                        foreach (var n in overlay.Npcs ?? new()) if (n.Script != null) named.Add(n.Script);
                    }
                foreach (var room in ownRooms)
                    foreach (var n in room.Npcs) if (n.Script != null) named.Add(n.Script);
                var library = ScriptLibrary.Default;
                var start = library.All.Where(s => own.Contains(s.File)
                    || s.File == "common" && (named.Contains(s.Name) || s.Name.StartsWith(region, StringComparison.OrdinalIgnoreCase)));
                var scripts = Reached(library, start);
                sources.Read(new[] { world }, ownRooms, library.All.Where(scripts.Contains));
            }
            return regions[region] = sources;
        }
    }

    /// <summary>Every world, room, script and list together.</summary>
    public static ItemSources Everywhere
    {
        get
        {
            lock (loading)
            {
                if (everywhere != null) return everywhere;
                var sources = new ItemSources();
                sources.Read(Worlds, Rooms, ScriptLibrary.Default.All);
                return everywhere = sources;
            }
        }
    }

    /// <summary>
    /// What the worlds, rooms and scripts given give, read afresh: a test's own world (a way taken away, an owed one given).
    /// </summary>
    public static ItemSources From(IEnumerable<World> worldsRead, IReadOnlyList<MapFile> roomsRead, IEnumerable<Script> scripts)
    {
        var sources = new ItemSources();
        sources.Read(worldsRead, roomsRead, scripts);
        return sources;
    }

    /// <summary>The scripts given and every script they call, to the end.</summary>
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

    private void Read(IEnumerable<World> worldsRead, IReadOnlyList<MapFile> roomsRead, IEnumerable<Script> scripts)
    {
        bool any = false;
        // The open areas: the balls lying in them and what is hidden, the overlays' own balls
        foreach (var world in worldsRead)
        {
            any = true;
            foreach (string key in world.Index.Areas)
            {
                var area = world.Area(key);
                var overlay = world.Overlay(key);
                var heldBack = new HashSet<string>(overlay?.HeldBack ?? new(), StringComparer.Ordinal);
                if (area != null)
                {
                    foreach (var o in area.Objects)
                        if (o.Item != null && !heldBack.Contains(o.Id)) Add(o.Item, ItemSourceKind.Ball, key);
                    foreach (var s in area.Signs)
                        if (s.Item != null) Add(s.Item, ItemSourceKind.Hidden, key);
                }
                foreach (var p in overlay?.People?.Values ?? Enumerable.Empty<OverlayPerson>())
                    if (p.Item != null && p.NpcType == "ItemBall") Add(p.Item, ItemSourceKind.Ball, key);
                foreach (var n in overlay?.Npcs ?? new())
                    if (n.Item != null && n.NpcType == "ItemBall") Add(n.Item, ItemSourceKind.Ball, key);
            }
        }

        // The rooms: their balls, what is hidden in them and the counters their people keep
        var counters = new HashSet<string>(StringComparer.Ordinal);
        foreach (var room in roomsRead)
        {
            any = true;
            foreach (var n in room.Npcs)
            {
                if (n.Item != null && n.NpcType == "ItemBall") Add(n.Item, ItemSourceKind.Ball, room.Name);
                if (!string.IsNullOrEmpty(n.Mart)) counters.Add(n.Mart);
            }
            foreach (var h in room.HiddenItems ?? new()) Add(h.Item, ItemSourceKind.Hidden, room.Name);
        }

        // What the scripts hand over, find, sell and trade for points
        foreach (var script in scripts)
            foreach (var i in script.Everything())
            {
                string where = script.FullName;
                switch (i.Op)
                {
                    case Op.Give or Op.AddItem: Add(i.Name, ItemSourceKind.Given, where); break;
                    case Op.Find: Add(i.Name, ItemSourceKind.Found, where); break;
                    case Op.Shop when !string.IsNullOrEmpty(i.Name): counters.Add(i.Name); break;
                    case Op.Frontier when i.Name == "prize":
                        foreach (var p in FrontierExchange.Stock(i.Other ?? FrontierExchange.Items)) Add(p.Item, ItemSourceKind.Prize, $"{where}: Battle Points ({i.Other})");
                        break;
                    case Op.Subway when i.Name == "prize":
                        foreach (var p in SubwayRiders.Prizes) Add(p.Item, ItemSourceKind.Prize, $"{where}: Battle Points");
                        break;
                    case Op.GameCorner when i.Name == "prize":
                        foreach (var (item, _) in i.Other == "mauville" ? GameCornerPrizes.Mauville : GameCornerPrizes.All)
                            Add(item, ItemSourceKind.Prize, $"{where}: coins");
                        break;
                    // A designer hands over whichever charm a Pokédex filled has earned (plan 40 · GE5): every charm of the table
                    case Op.Charm when i.Name == "give":
                        foreach (string charm in Charms.Table.Regional.Append(Charms.Table.National)) Add(charm, ItemSourceKind.Given, where);
                        break;
                    // Join Avenue's shops, at every rank (plan 22 · U11)
                    case Op.JoinAvenue when i.Name == "shop":
                        for (int rank = 0; rank <= JoinAvenue.TopRank; rank++)
                            if (JoinAvenue.StockOf(i.Other ?? "", rank) is { } stock) counters.Add(stock);
                        break;
                    // Plan 40 · GE11: what a Pokémon handed over holds, and what a person's trade sends with its Pokémon
                    case Op.GivePokemon when !string.IsNullOrEmpty(i.Other):
                        Add(i.Other, ItemSourceKind.Held, $"{where}: a {i.Name} given holding it");
                        break;
                    case Op.Trade when NpcTrades.Get(i.Name) is { } trade && !string.IsNullOrEmpty(trade.Item):
                        Add(trade.Item, ItemSourceKind.Traded, $"{where}: the trade {trade.Id}'s {trade.Gives}");
                        break;
                }
            }

        // The counters: every Poké Mart's common list where there is a world, and each specialty the scripts and rooms open
        if (any) foreach (var m in MartDatabase.Common) Add(m.Item, ItemSourceKind.Stock, "a Poké Mart");
        foreach (string counter in counters.OrderBy(c => c, StringComparer.Ordinal))
            foreach (string item in MartDatabase.Specialties.GetValueOrDefault(counter) ?? new List<string>())
                Add(item, ItemSourceKind.Stock, $"the counter {counter}");
    }
}
