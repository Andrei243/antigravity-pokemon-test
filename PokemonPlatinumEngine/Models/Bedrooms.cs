using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models;

/// <summary>
/// The bedroom at home in every region (plan 43 · LF7; Emerald's own bedroom decorating, H10's leftover): decorated from
/// the same decoration PC as the secret base, twelve pieces at most, each region's room a layout of its own, opened at
/// the bedroom's computer once the decoration PC holds anything (<c>openbase bedroom</c>). A piece standing in a bedroom
/// is out of the PC, and so stands neither in the base nor in another bedroom at the same time. No drawing, no input.
/// </summary>
public static class Bedrooms
{
    /// <summary>How many decorations may stand in a bedroom at once (Emerald's <c>DECOR_MAX_PLAYERS_HOUSE</c> is twelve for its room's decorations).</summary>
    public const int MostOut = 12;

    /// <summary>The player's room in each region, by the map the region's game begins in or the room upstairs at home.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> Maps = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
    {
        [RegionDatabase.Sinnoh] = new[] { "PlayerHouse2F" },
        [RegionDatabase.Kanto] = new[] { "PalletTown_PlayersHouse_2F" },
        [RegionDatabase.Johto] = new[] { "NewBark_PlayersHouse_2F" },
        // Hoenn's is the hero's own house's room, whichever the look lives in
        [RegionDatabase.Hoenn] = new[] { "LittlerootTown_BrendansHouse_2F", "LittlerootTown_MaysHouse_2F" },
        [RegionDatabase.Unova] = new[] { "AspertiaCity_PlayersHouse" },
        [RegionDatabase.Kalos] = new[] { "VanivilleTown_PlayersHouse_2F" },
    };

    /// <summary>The region a map is the bedroom of; null for no bedroom.</summary>
    public static string? RegionOf(string? map) =>
        map == null ? null : Maps.FirstOrDefault(p => p.Value.Contains(map, StringComparer.OrdinalIgnoreCase)).Key;

    /// <summary>
    /// The computers the import's furniture doesn't lay (May's room is drawn in metatiles Emerald's tables of looks
    /// don't name yet): the tile of the room's own PC sign event.
    /// </summary>
    private static readonly IReadOnlyDictionary<string, (int X, int Y)> SignedComputers = new Dictionary<string, (int X, int Y)>(StringComparer.OrdinalIgnoreCase)
    {
        ["LittlerootTown_MaysHouse_2F"] = (8, 1),
    };

    /// <summary>Whether a tile of a room is its computer: a computer standing over it, the tile's own behaviour, or the PC sign of a room whose furniture lacks one.</summary>
    public static bool IsComputerAt(Map room, int x, int y) =>
        room.InBounds(x, y) && (room.BehaviourAt(x, y) == TileBehavior.Computer
            || SignedComputers.TryGetValue(room.Name, out var pc) && pc == (x, y)
            || room.Props.Any(p => p.Type == PropType.Computer && x >= p.X && x < p.X + Math.Max(1, p.Width) && y >= p.Y && y < p.Y + Math.Max(1, p.Depth)));

    /// <summary>Whether a room has a computer at all.</summary>
    public static bool HasComputer(Map room)
    {
        for (int y = 0; y < room.Height; y++)
            for (int x = 0; x < room.Width; x++)
                if (IsComputerAt(room, x, y)) return true;
        return false;
    }

    /// <summary>
    /// The bedroom's floor as decorations see it: every open tile that is no way out, the wall's tiles over the floor,
    /// and kept clear every tile a scene or the player's own steps need: the ways out and the tiles beside them, the
    /// tiles the room's triggers lie on, where the region's game begins, and the tile before each piece of furniture
    /// that is spoken to or read (the computer, a television, a wardrobe, a sign). <paramref name="laid"/> are tiles a
    /// decoration laid out stands in the way of, which are floor all the same.
    /// </summary>
    public static BaseFloor FloorOf(Map room, string region, ISet<(int X, int Y)>? laid = null)
    {
        var floor = new HashSet<(int, int)>();
        var wall = new HashSet<(int, int)>();
        for (int y = 0; y < room.Height; y++)
            for (int x = 0; x < room.Width; x++)
                if ((!room.IsSolid(x, y) || laid?.Contains((x, y)) == true) && room.GetWarpAt(x, y) == null) floor.Add((x, y));
        for (int y = 0; y < room.Height - 1; y++)
            for (int x = 0; x < room.Width; x++)
                if (room.IsSolid(x, y) && floor.Contains((x, y + 1)) && !IsComputerAt(room, x, y)) wall.Add((x, y));

        var clear = new HashSet<(int X, int Y)>();
        void Round(int x, int y)
        {
            clear.Add((x, y));
            clear.Add((x + 1, y)); clear.Add((x - 1, y)); clear.Add((x, y + 1)); clear.Add((x, y - 1));
        }
        foreach (var warp in room.Warps) Round(warp.SourceX, warp.SourceY);
        foreach (var t in room.Triggers)
            for (int ty = t.Y; ty < t.Y + Math.Max(1, t.Depth); ty++)
                for (int tx = t.X; tx < t.X + Math.Max(1, t.Width); tx++) clear.Add((tx, ty));
        if (RegionDatabase.Get(region)?.Start is { } start && start.Map == room.Name) Round(start.X, start.Y);
        // What is spoken to or read: a sign, a script read on a tile, the furniture that answers
        var answering = new HashSet<(int X, int Y)>(room.Signboards.Keys);
        foreach (var key in room.TileScripts.Keys) answering.Add(key);
        foreach (var p in room.Props.Where(p => p.Type is PropType.Computer or PropType.Television or PropType.Wardrobe or PropType.Bed))
            for (int py = p.Y; py < p.Y + Math.Max(1, p.Depth); py++)
                for (int px = p.X; px < p.X + Math.Max(1, p.Width); px++) answering.Add((px, py));
        for (int y = 0; y < room.Height; y++)
            for (int x = 0; x < room.Width; x++)
                if (IsComputerAt(room, x, y)) answering.Add((x, y));
        foreach (var (x, y) in answering) Round(x, y);
        // and whoever lives in the room stands where they stand
        foreach (var npc in room.Everyone) Round(npc.GridX, npc.GridY);

        var kept = clear.Where(floor.Contains).OrderBy(t => t.Y).ThenBy(t => t.X).ToList();
        // The cursor starts on the first open tile that is free, nearest the room's middle
        var free = floor.Where(t => !clear.Contains(t)).OrderBy(t => Math.Abs(t.Item1 - room.Width / 2) + Math.Abs(t.Item2 - room.Height / 2)).ToList();
        var startAt = free.Count > 0 ? free[0] : floor.FirstOrDefault();
        return new BaseFloor { Floor = floor, Wall = wall, KeptClear = kept, Start = startAt };
    }

    /// <summary>
    /// The bedroom to decorate: its floor and wall, the tiles kept clear, what stands there out of the PC, at most
    /// <see cref="MostOut"/>, whether a decoration fits where it hangs, and that the floor left open still joins every
    /// tile kept clear, so no piece shuts the player in or a scene's way off.
    /// </summary>
    public static DecorationRoom Room(IReadOnlyList<PlacedDecoration> placed, IReadOnlyList<StoredDecoration> store, BaseFloor floor)
    {
        var all = floor.Floor.Concat(floor.Wall).ToList();
        int left = all.Count > 0 ? all.Min(t => t.X) : 0, top = all.Count > 0 ? all.Min(t => t.Y) : 0;
        int right = all.Count > 0 ? all.Max(t => t.X) : 0, bottom = all.Count > 0 ? all.Max(t => t.Y) : 0;
        var pieces = new List<RoomPiece>();
        foreach (var p in placed)
        {
            if (p.Slot < 0 || p.Slot >= store.Count) continue;
            var kind = SecretBases.KindOf(store[p.Slot].Kind);
            pieces.Add(new RoomPiece { Kind = kind.Id, Name = kind.Name, X = p.X, Y = p.Y, Width = kind.Width, Depth = kind.Depth, Slot = p.Slot });
        }
        DecorationRoom? room = null;
        room = new DecorationRoom
        {
            Left = left, Top = top, Width = right - left + 1, Height = bottom - top + 1,
            KeptClear = floor.KeptClear.ToList(), Pieces = pieces, Limit = MostOut, Start = floor.Start,
            Allows = (piece, x, y) => DecorationDatabase.Get(piece.Kind) is { } d && floor.Fits(d, x, y)
                && StillJoined(floor, room!.Pieces.Where(p => p != piece), d, x, y),
            TileAt = (x, y) => floor.Floor.Contains((x, y)) ? RoomTile.Floor : floor.Wall.Contains((x, y)) ? RoomTile.Wall : RoomTile.Rock
        };
        return room;
    }

    /// <summary>The tiles a decoration blocks where its corner stands (none for a mat or a poster, a tall one's top row walked behind).</summary>
    public static IEnumerable<(int X, int Y)> Blocked(DecorationRecord d, int x, int y)
    {
        if (!d.Blocks || d.Placement == DecorationPlacement.Wall) yield break;
        var (w, depth) = d.Footprint;
        int top = d.Placement == DecorationPlacement.Tall && depth > 1 ? 1 : 0;
        for (int ty = y + top; ty < y + depth; ty++)
            for (int tx = x; tx < x + w; tx++) yield return (tx, ty);
    }

    /// <summary>Whether, with a decoration set down at a tile beside what stands already, every tile kept clear is still reached from every other over open floor.</summary>
    public static bool StillJoined(BaseFloor floor, IEnumerable<RoomPiece> others, DecorationRecord d, int x, int y)
    {
        var shut = new HashSet<(int, int)>(Blocked(d, x, y));
        foreach (var o in others)
            if (DecorationDatabase.Get(o.Kind) is { } od) shut.UnionWith(Blocked(od, o.X, o.Y));
        var open = floor.Floor.Where(t => !shut.Contains(t)).ToHashSet();
        var keep = floor.KeptClear.Where(open.Contains).ToList();
        if (keep.Count == 0) return true;
        var seen = new HashSet<(int, int)> { keep[0] };
        var queue = new Queue<(int X, int Y)>();
        queue.Enqueue(keep[0]);
        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            foreach (var n in new[] { (cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1) })
                if (open.Contains(n) && seen.Add(n)) queue.Enqueue(n);
        }
        return keep.All(seen.Contains);
    }

    /// <summary>What a decorating of a region's bedroom left standing, kept as that bedroom's.</summary>
    public static void Keep(Dictionary<string, List<PlacedDecoration>> bedrooms, string region, DecorationRoom decorated)
    {
        var placed = decorated.Pieces.Where(p => !p.IsFixture).Select(p => new PlacedDecoration { Slot = p.Slot, X = p.X, Y = p.Y }).ToList();
        if (placed.Count == 0) bedrooms.Remove(region);
        else bedrooms[region] = placed;
    }

    /// <summary>What stands in a region's bedroom: each decoration, where its corner is.</summary>
    public static IEnumerable<(DecorationRecord Decoration, int X, int Y)> Standing(IReadOnlyList<PlacedDecoration>? placed, IReadOnlyList<StoredDecoration> store) =>
        (placed ?? (IReadOnlyList<PlacedDecoration>)Array.Empty<PlacedDecoration>())
            .Where(p => p.Slot >= 0 && p.Slot < store.Count)
            .Select(p => (Decoration: DecorationDatabase.Get(store[p.Slot].Kind), p.X, p.Y))
            .Where(p => p.Decoration != null)
            .Select(p => (p.Decoration!, p.X, p.Y));

    /// <summary>
    /// Which decorations of the PC are out: those standing in the base and those standing in any bedroom. Every place
    /// that changes what stands anywhere asks this last, so a piece is never set down in two rooms at once.
    /// </summary>
    public static void MarkOut(IReadOnlyList<StoredDecoration> store, SecretBaseSave? home, IReadOnlyDictionary<string, List<PlacedDecoration>>? bedrooms)
    {
        var out_ = new HashSet<int>(home?.Placed.Select(p => p.Slot) ?? Enumerable.Empty<int>());
        foreach (var room in bedrooms?.Values ?? Enumerable.Empty<List<PlacedDecoration>>())
            out_.UnionWith(room.Select(p => p.Slot));
        for (int i = 0; i < store.Count; i++) store[i].Out = out_.Contains(i);
    }
}
