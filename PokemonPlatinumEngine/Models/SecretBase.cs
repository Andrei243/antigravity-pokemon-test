using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Models;

/// <summary>The kind of spot a secret base is made at (Emerald's <c>SECRET_BASE_*</c>, in its order), which decides its room.</summary>
public enum SecretBaseKind { None, RedCave, BrownCave, BlueCave, YellowCave, Tree, Shrub }

/// <summary>
/// One of the twenty-four rooms a secret base can be (plan 21 · H10; Emerald's <c>sSecretBaseEntrancePositions</c>): its
/// group (the id's tens), its map, its kind, its number among the four of its kind, and the tile before its PC, where
/// whoever has just made the base stands.
/// </summary>
public sealed record SecretBaseLayout(int Group, string Map, SecretBaseKind Kind, int Number, int PcFrontX, int PcFrontY);

/// <summary>
/// The secret bases' rooms and spots (plan 21 · H10): a spot of the region's map names its base by an id, Emerald's
/// (<c>SECRET_BASE_TREE1_2</c> is 162): the tens are the room, which ten spots at most share, and the ones tell the
/// spots of a room apart. GPU-free, as every rule of the bases is.
/// </summary>
public static class SecretBaseLayouts
{
    /// <summary>The kinds in the order Emerald's groups go: four rooms of each.</summary>
    private static readonly SecretBaseKind[] GroupKinds =
        { SecretBaseKind.RedCave, SecretBaseKind.BrownCave, SecretBaseKind.BlueCave, SecretBaseKind.YellowCave, SecretBaseKind.Tree, SecretBaseKind.Shrub };

    /// <summary>The tile before each group's PC (Emerald's table's last two numbers), in its order.</summary>
    private static readonly (int X, int Y)[] PcFronts =
    {
        (1, 3), (5, 9), (1, 3), (7, 13), (2, 3), (9, 2), (13, 4), (1, 2), (1, 3), (1, 2), (3, 15), (3, 14),
        (9, 3), (8, 7), (3, 6), (5, 9), (2, 3), (5, 6), (15, 3), (4, 10), (3, 3), (1, 2), (7, 8), (9, 6)
    };

    /// <summary>The twenty-four rooms, in Emerald's order of groups.</summary>
    public static IReadOnlyList<SecretBaseLayout> All => Enumerable.Range(0, 24).Select(g =>
    {
        var kind = GroupKinds[g / 4];
        int number = g % 4 + 1;
        return new SecretBaseLayout(g, $"SecretBase_{kind}{number}", kind, number, PcFronts[g].X, PcFronts[g].Y);
    }).ToList();

    private static readonly Regex Constant = new(@"^SECRET_BASE_(RED_CAVE|BROWN_CAVE|BLUE_CAVE|YELLOW_CAVE|TREE|SHRUB)([1-4])_([1-9])$", RegexOptions.CultureInvariant);

    /// <summary>A base's id by its constant (<c>SECRET_BASE_RED_CAVE2_3</c> is 13); 0 for no base's.</summary>
    public static int IdOf(string constant)
    {
        var m = Constant.Match(constant);
        if (!m.Success) return 0;
        int kind = Array.IndexOf(new[] { "RED_CAVE", "BROWN_CAVE", "BLUE_CAVE", "YELLOW_CAVE", "TREE", "SHRUB" }, m.Groups[1].Value);
        int group = kind * 4 + int.Parse(m.Groups[2].Value) - 1;
        return group * 10 + int.Parse(m.Groups[3].Value);
    }

    /// <summary>The room a base's id makes (Emerald's <c>SECRET_BASE_ID_TO_GROUP</c>); null for no base's.</summary>
    public static SecretBaseLayout? Of(int id) => id > 0 && id / 10 < 24 && id % 10 > 0 ? All[id / 10] : null;

    /// <summary>The room a map is; null for a map that is no secret base's.</summary>
    public static SecretBaseLayout? OfMap(string? map) => map == null ? null : All.FirstOrDefault(l => l.Map == map);

    public static bool IsBaseMap(string? map) => map != null && map.StartsWith("SecretBase_", StringComparison.Ordinal) && OfMap(map) != null;
}

/// <summary>A decoration standing in the base: its place in the decoration PC and the corner it stands on.</summary>
public sealed class PlacedDecoration
{
    public int Slot { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
}

/// <summary>
/// The player's secret base as the save keeps it (plan 21 · H10; Emerald's <c>secretBases[0]</c>): the spot's id, the map
/// and tile of the spot, the tile the player stood on to open it (where the base's door leads back to), what stands in
/// it and how often it has been come into.
/// </summary>
public sealed class SecretBaseSave
{
    public int Id { get; set; }
    public string Map { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int OutX { get; set; }
    public int OutY { get; set; }
    public List<PlacedDecoration> Placed { get; set; } = new();
    public int TimesEntered { get; set; }

    public SecretBaseLayout? Layout => SecretBaseLayouts.Of(Id);
}

/// <summary>
/// The room of a base as its decorations see it: the open floor things may stand on, the wall's tiles over the floor
/// (where a poster hangs), the tiles kept clear (the way in from the door and the tile before the PC) and where the
/// cursor starts. Made from the room's map (<see cref="Of"/>) or by hand in a test.
/// </summary>
public sealed class BaseFloor
{
    public HashSet<(int X, int Y)> Floor { get; init; } = new();
    public HashSet<(int X, int Y)> Wall { get; init; } = new();
    public List<(int X, int Y)> KeptClear { get; init; } = new();
    public (int X, int Y) Start { get; init; }

    /// <summary>
    /// A base's room: floor is every open tile that is no way out; the wall is every blocked tile with floor right under
    /// it that is no PC (Emerald's <c>MB_SECRET_BASE_NORTH_WALL</c>, which the reader doesn't keep); kept clear are the
    /// tile inside the door and the tile before the PC.
    /// </summary>
    public static BaseFloor Of(Map room, SecretBaseLayout layout, ISet<(int X, int Y)>? laid = null)
    {
        var floor = new HashSet<(int, int)>();
        var wall = new HashSet<(int, int)>();
        // (a tile a decoration laid out stands in the way of is floor all the same: laid)
        for (int y = 0; y < room.Height; y++)
            for (int x = 0; x < room.Width; x++)
                if ((!room.IsSolid(x, y) || laid?.Contains((x, y)) == true) && room.GetWarpAt(x, y) == null) floor.Add((x, y));
        for (int y = 0; y < room.Height - 1; y++)
            for (int x = 0; x < room.Width; x++)
                if (room.IsSolid(x, y) && floor.Contains((x, y + 1)) && room.BehaviourAt(x, y) != TileBehavior.Computer) wall.Add((x, y));
        var clear = new List<(int, int)> { (layout.PcFrontX, layout.PcFrontY) };
        if (room.Warps.FirstOrDefault() is { } door) clear.Add((door.SourceX, door.SourceY - 1));
        return new BaseFloor { Floor = floor, Wall = wall, KeptClear = clear, Start = (layout.PcFrontX, layout.PcFrontY) };
    }

    /// <summary>
    /// Whether a decoration may stand with its corner on a tile, by how it stands (Emerald's <c>CanPlaceDecoration</c>):
    /// on floor all over for one on the floor or a figure, its top row on floor or the wall for a tall one, on the wall
    /// all over for a poster.
    /// </summary>
    public bool Fits(DecorationRecord decoration, int x, int y)
    {
        var (w, d) = decoration.Footprint;
        for (int dy = 0; dy < d; dy++)
            for (int dx = 0; dx < w; dx++)
            {
                var at = (x + dx, y + dy);
                bool ok = decoration.Placement switch
                {
                    DecorationPlacement.Wall => Wall.Contains(at),
                    DecorationPlacement.Tall when dy == 0 && d > 1 => Floor.Contains(at) || Wall.Contains(at),
                    _ => Floor.Contains(at)
                };
                if (!ok) return false;
            }
        return true;
    }
}

/// <summary>
/// The rules of the secret bases (plan 21 · H10; Emerald's <c>secret_base.c</c> and <c>decoration.c</c>): the decoration
/// PC's room, a base made at a spot (the one before emptied, its decorations back in the PC: one base at a time), the
/// room handed to the shared decorating (<see cref="DecoratingSession"/>) and what it left kept. No drawing, no input.
/// </summary>
public static class SecretBases
{
    /// <summary>How many decorations may stand in a base at once (Emerald's <c>DECOR_MAX_SECRET_BASE</c>).</summary>
    public const int MostOut = 16;

    /// <summary>Whether the decoration PC has room for one more of a decoration's kind.</summary>
    public static bool HasRoom(IReadOnlyList<StoredDecoration> store, int decoration) =>
        DecorationDatabase.Get(decoration) is { } d
        && store.Count(s => DecorationDatabase.Get(s.Kind)?.Category == d.Category) < DecorationDatabase.Room(d.Category);

    /// <summary>A decoration put into the PC (bought, given): false with no room for its kind.</summary>
    public static bool Add(List<StoredDecoration> store, int decoration)
    {
        if (!HasRoom(store, decoration)) return false;
        store.Add(new StoredDecoration { Kind = decoration });
        return true;
    }

    /// <summary>
    /// A base made at a spot (Emerald's <c>SetPlayerSecretBase</c>, after <c>MoveOutOfSecretBaseFromOutside</c>): the one
    /// before, if any, is emptied, everything in it back in the PC, and the new one is bare.
    /// </summary>
    public static SecretBaseSave Open(SecretBaseSave? old, List<StoredDecoration> store, int id, string map, int x, int y, int outX, int outY)
    {
        if (old != null) PackUp(old, store);
        return new SecretBaseSave { Id = id, Map = map, X = x, Y = y, OutX = outX, OutY = outY };
    }

    /// <summary>Everything standing in a base taken back into the PC.</summary>
    public static void PackUp(SecretBaseSave room, List<StoredDecoration> store)
    {
        foreach (var placed in room.Placed)
            if (placed.Slot >= 0 && placed.Slot < store.Count) store[placed.Slot].Out = false;
        room.Placed.Clear();
    }

    /// <summary>What stands in the base: each decoration, where its corner is.</summary>
    public static IEnumerable<(DecorationRecord Decoration, int X, int Y)> Standing(SecretBaseSave room, IReadOnlyList<StoredDecoration> store) =>
        room.Placed.Where(p => p.Slot >= 0 && p.Slot < store.Count)
            .Select(p => (Decoration: DecorationDatabase.Get(store[p.Slot].Kind), p.X, p.Y))
            .Where(p => p.Decoration != null)
            .Select(p => (p.Decoration!, p.X, p.Y));

    /// <summary>The kind the shared decorating knows a decoration by: its name and the tiles it takes.</summary>
    public static DecorationKind KindOf(int decoration)
    {
        var d = DecorationDatabase.Get(decoration);
        var (w, depth) = d?.Footprint ?? (1, 1);
        return new DecorationKind(decoration, d?.Name ?? "", w, depth);
    }

    /// <summary>
    /// The base's room to decorate: the floor and the wall over it inside their bounds, the tiles kept clear, what
    /// stands there out of the PC, at most <see cref="MostOut"/>, and whether a decoration fits where it hangs.
    /// </summary>
    public static DecorationRoom Room(SecretBaseSave room, IReadOnlyList<StoredDecoration> store, BaseFloor floor)
    {
        var all = floor.Floor.Concat(floor.Wall).ToList();
        int left = all.Count > 0 ? all.Min(t => t.X) : 0, top = all.Count > 0 ? all.Min(t => t.Y) : 0;
        int right = all.Count > 0 ? all.Max(t => t.X) : 0, bottom = all.Count > 0 ? all.Max(t => t.Y) : 0;
        var pieces = new List<RoomPiece>();
        foreach (var placed in room.Placed)
        {
            if (placed.Slot < 0 || placed.Slot >= store.Count) continue;
            var kind = KindOf(store[placed.Slot].Kind);
            pieces.Add(new RoomPiece { Kind = kind.Id, Name = kind.Name, X = placed.X, Y = placed.Y, Width = kind.Width, Depth = kind.Depth, Slot = placed.Slot });
        }
        return new DecorationRoom
        {
            Left = left, Top = top, Width = right - left + 1, Height = bottom - top + 1,
            KeptClear = floor.KeptClear.ToList(), Pieces = pieces, Limit = MostOut, Start = floor.Start,
            Allows = (piece, x, y) => DecorationDatabase.Get(piece.Kind) is { } d && floor.Fits(d, x, y),
            TileAt = (x, y) => floor.Floor.Contains((x, y)) ? RoomTile.Floor : floor.Wall.Contains((x, y)) ? RoomTile.Wall : RoomTile.Rock
        };
    }

    /// <summary>What a decorating left standing, kept as the base's (Emerald's decorations and their positions).</summary>
    public static void Keep(SecretBaseSave room, DecorationRoom decorated, IReadOnlyList<StoredDecoration> store)
    {
        room.Placed = decorated.Pieces.Where(p => !p.IsFixture).Select(p => new PlacedDecoration { Slot = p.Slot, X = p.X, Y = p.Y }).ToList();
        for (int i = 0; i < store.Count; i++) store[i].Out = room.Placed.Any(p => p.Slot == i);
    }
}
