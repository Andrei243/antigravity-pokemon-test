using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PokemonPlatinumEngine.Overworld;
using TownArchitecture = PokemonPlatinumEngine.Overworld.Architecture;

namespace PokemonPlatinumEngine.Data;

/// <summary>
/// What a tile of the imported world looks like: what its ground is made of, or what stands on it. The tile's
/// <see cref="TileBehavior"/> says what it does; this says what to draw there.
/// </summary>
public enum TerrainCover
{
    Unknown,
    Grass,
    Flowers,
    TallGrass,
    /// <summary>Bare earth, a sandy track, gravel.</summary>
    Path,
    /// <summary>The made ground of towns and cities: flagstones, tiles, asphalt.</summary>
    Paving,
    Sand,
    /// <summary>Bare rock underfoot.</summary>
    Rock,
    CaveFloor,
    Snow,
    Ice,
    Marsh,
    Water,
    /// <summary>The planks of a bridge or a pier.</summary>
    Bridge,
    Steps,
    Tree,
    /// <summary>A rock face.</summary>
    Cliff,
    /// <summary>A rock standing on the ground or in water.</summary>
    Boulder,
    /// <summary>A fence, a railing, a low wall.</summary>
    Fence,
    /// <summary>Under one of the chunk's props: a building, usually.</summary>
    Building,
    /// <summary>A street lamp.</summary>
    Lamp,
    /// <summary>The deck of a raised walkway of steel and solar panels (Sunyshore).</summary>
    Walkway,
    /// <summary>A broad-leaved tree, where an area's forests are not Sinnoh's pines (the Battle Zone).</summary>
    Broadleaf,
    /// <summary>The dark in the mouth of a cave: blocked where it is the hole in the rock, open where it is the way in.</summary>
    CaveMouth,
    /// <summary>The dark under the trees where a forest is entered: blocked where it is the shade beyond, open where it is the way in.</summary>
    ForestMouth
}

/// <summary>The one-character codes of <see cref="TerrainCover"/> in chunk files.</summary>
public static class TerrainCoverCodes
{
    private static readonly (TerrainCover Cover, char Code)[] Table =
    {
        (TerrainCover.Unknown, '?'), (TerrainCover.Grass, '.'), (TerrainCover.Flowers, '*'), (TerrainCover.TallGrass, 'w'),
        (TerrainCover.Path, ':'), (TerrainCover.Paving, '='), (TerrainCover.Sand, ','), (TerrainCover.Rock, 'r'),
        (TerrainCover.CaveFloor, 'c'), (TerrainCover.Snow, '^'), (TerrainCover.Ice, 'i'), (TerrainCover.Marsh, 'm'),
        (TerrainCover.Water, '~'), (TerrainCover.Bridge, 'b'), (TerrainCover.Steps, 's'), (TerrainCover.Tree, 'T'),
        (TerrainCover.Cliff, 'C'), (TerrainCover.Boulder, 'R'), (TerrainCover.Fence, 'F'), (TerrainCover.Building, 'B'),
        (TerrainCover.Lamp, 'L'), (TerrainCover.Walkway, 'W'), (TerrainCover.Broadleaf, 'O'), (TerrainCover.CaveMouth, 'M'),
        (TerrainCover.ForestMouth, 'E')
    };

    public static char CodeOf(TerrainCover cover) => Table.First(e => e.Cover == cover).Code;

    public static TerrainCover Parse(char code)
    {
        foreach (var (cover, c) in Table)
            if (c == code) return cover;
        throw new InvalidDataException($"Unknown terrain cover code '{code}'.");
    }

    public static IEnumerable<TerrainCover> All => Table.Select(e => e.Cover);
}

/// <summary>
/// One 32×32-tile chunk of the imported world (<c>chunks/NNN.json</c>). Rows run north to south, tiles west to
/// east. <c>tools/MapImporter</c> writes these files; <c>docs/data-files.md</c> describes them.
/// </summary>
public sealed class WorldChunkFile
{
    public const int Tiles = 32;

    public int Id { get; set; }

    /// <summary>What each tile does: two hexadecimal digits per tile, a <see cref="TileBehavior"/> value.</summary>
    public List<string> Behaviours { get; set; } = new();

    /// <summary>Blocked tiles: '#' is solid, '.' is open.</summary>
    public List<string> Solid { get; set; } = new();

    /// <summary>What each tile looks like: one <see cref="TerrainCoverCodes"/> character per tile.</summary>
    public List<string> Cover { get; set; } = new();

    /// <summary>The ground's height, as rectangles of flat or sloping ground; see <see cref="HeightPlate"/>.</summary>
    public List<HeightPlate> Heights { get; set; } = new();

    public List<ChunkProp> Props { get; set; } = new();

    public TileBehavior BehaviourAt(int x, int z) =>
        (TileBehavior)byte.Parse(Behaviours[z].AsSpan(x * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

    public bool SolidAt(int x, int z) => Solid[z][x] == '#';

    public TerrainCover CoverAt(int x, int z) => TerrainCoverCodes.Parse(Cover[z][x]);

    /// <summary>
    /// The height of the ground at a point (in tiles from the chunk's north-west corner) for a walker who is at
    /// height <paramref name="from"/>: where plates overlap, as under a bridge, the nearest one. Null where the
    /// chunk has no ground.
    /// </summary>
    public float? HeightAt(float x, float z, float from = 0f)
    {
        float? best = null;
        foreach (var plate in Heights)
        {
            if (!plate.Contains(x, z)) continue;
            float h = plate.HeightAt(x, z);
            if (best == null || MathF.Abs(h - from) < MathF.Abs(best.Value - from)) best = h;
        }
        return best;
    }

    /// <summary>Checks the grids' sizes and codes, so a damaged file stops loading with its fault named.</summary>
    public void Validate()
    {
        void Rows(List<string> rows, string layer, int width)
        {
            if (rows.Count != Tiles || rows.Any(r => r.Length != width))
                throw new InvalidDataException($"Chunk {Id}: {layer} must be {Tiles} rows of {width} characters.");
        }
        Rows(Behaviours, "behaviours", Tiles * 2);
        Rows(Solid, "solid", Tiles);
        Rows(Cover, "cover", Tiles);
        for (int z = 0; z < Tiles; z++)
            for (int x = 0; x < Tiles; x++)
            {
                if (!byte.TryParse(Behaviours[z].AsSpan(x * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
                    throw new InvalidDataException($"Chunk {Id}: behaviours row {z} has '{Behaviours[z].Substring(x * 2, 2)}' at tile {x}.");
                if (Solid[z][x] != '#' && Solid[z][x] != '.')
                    throw new InvalidDataException($"Chunk {Id}: solid row {z} has '{Solid[z][x]}' at tile {x}; use '#' or '.'.");
                TerrainCoverCodes.Parse(Cover[z][x]);
            }
    }
}

/// <summary>
/// A rectangle of ground and the plane it lies in, in tiles from the chunk's north-west corner: the ground is
/// <see cref="Height"/> tiles high at the rectangle's north-west corner and rises by <see cref="SlopeX"/> per tile
/// eastward and <see cref="SlopeZ"/> per tile southward. Flat ground has no slopes; stairs and ramps have one.
/// </summary>
[JsonConverter(typeof(OneLine<HeightPlate>))]
public sealed class HeightPlate
{
    public float X { get; set; }
    public float Z { get; set; }
    public float Width { get; set; }
    public float Depth { get; set; }
    public float Height { get; set; }
    public float SlopeX { get; set; }
    public float SlopeZ { get; set; }

    public bool Contains(float x, float z) => x >= X && x <= X + Width && z >= Z && z <= Z + Depth;

    public float HeightAt(float x, float z) => Height + (x - X) * SlopeX + (z - Z) * SlopeZ;
}

/// <summary>
/// Something standing on a chunk: a building, a signboard, a piece of furniture. <see cref="Model"/> is the
/// original's id for it and <see cref="Name"/> its short name, which is how the game decides what to build there;
/// the box is the space it takes, in tiles (heights in tiles too), from the chunk's north-west corner.
/// </summary>
[JsonConverter(typeof(OneLine<ChunkProp>))]
public sealed class ChunkProp
{
    public int Model { get; set; }
    public string Name { get; set; } = "";

    /// <summary>The prop's own origin, usually the middle of its base.</summary>
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }

    /// <summary>The north-west corner of the space it takes, and that space's size.</summary>
    public float BoxX { get; set; }
    public float BoxZ { get; set; }
    public float Width { get; set; }
    public float Depth { get; set; }
    public float Height { get; set; }
}

/// <summary>
/// A grid of chunks (<c>matrices/NNN.json</c>): the overworld is matrix 0; a cave floor or the inside of a
/// building is a small matrix of its own.
/// </summary>
public sealed class WorldMatrixFile
{
    public const int NoChunk = -1;

    public int Id { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    /// <summary>Chunk ids, one row of the grid per string, separated by spaces; <c>-</c> where there is none.</summary>
    public List<string> Chunks { get; set; } = new();

    /// <summary>The area each chunk belongs to (an index into <see cref="AreaKeys"/>), in the same layout; left
    /// out when the whole matrix belongs to the one area that uses it.</summary>
    public List<string>? Areas { get; set; }

    /// <summary>The areas the grid refers to.</summary>
    public List<string>? AreaKeys { get; set; }

    /// <summary>How high each chunk sits, in half tiles, in the same layout; left out when all sit at zero.</summary>
    public List<string>? Altitudes { get; set; }

    public int ChunkAt(int x, int y) => Cell(Chunks, x, y) is "-" ? NoChunk : int.Parse(Cell(Chunks, x, y), CultureInfo.InvariantCulture);

    public string? AreaAt(int x, int y) =>
        Areas == null || AreaKeys == null || Cell(Areas, x, y) is "-" ? null : AreaKeys[int.Parse(Cell(Areas, x, y), CultureInfo.InvariantCulture)];

    public int AltitudeAt(int x, int y) => Altitudes == null ? 0 : int.Parse(Cell(Altitudes, x, y), CultureInfo.InvariantCulture);

    private string Cell(List<string> rows, int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) throw new ArgumentOutOfRangeException(nameof(x), $"Matrix {Id} has no cell {x},{y}.");
        var cells = rows[y].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (cells.Length != Width) throw new InvalidDataException($"Matrix {Id}: row {y} has {cells.Length} cells, expected {Width}.");
        return cells[x];
    }

    /// <summary>One row of a grid as it is stored: cells padded to one width, so the rows line up.</summary>
    public static string Row(IEnumerable<string> cells, int cellWidth)
    {
        var row = new StringBuilder();
        foreach (string cell in cells)
        {
            if (row.Length > 0) row.Append(' ');
            row.Append(cell.PadLeft(cellWidth));
        }
        return row.ToString();
    }
}

/// <summary>
/// One area (<c>areas/&lt;key&gt;.json</c>): a town, a route, a cave floor, a room. It says which matrix it lies on,
/// what it is called and how it behaves, and lists what is placed on it. Positions are tiles of its matrix (for
/// the overworld: tiles of the whole region).
/// </summary>
public sealed class WorldAreaFile
{
    /// <summary>The area's key: <c>twinleaf_town</c>.</summary>
    public string Key { get; set; } = "";

    /// <summary>The original's number for the area; warps and saved positions in the original's data use it.</summary>
    public int Index { get; set; }

    /// <summary>The name a player sees.</summary>
    public string Name { get; set; } = "";

    public int Matrix { get; set; }

    /// <summary>Town, Outdoors, Cave, Indoors, PokemonCenter or Underground.</summary>
    public string Kind { get; set; } = "";

    /// <summary>The style of the sign that names the area on arrival: City, Town, Route, Cave, Forest, Water, Park, Lake, Indoors or None.</summary>
    public string Sign { get; set; } = "";

    public string Weather { get; set; } = "";
    public string Camera { get; set; } = "";
    public string BattleBackground { get; set; } = "";

    /// <summary>The roles of the area's music by day and by night, as the original names them (a checklist for plan 05).</summary>
    public string DayMusic { get; set; } = "";
    public string NightMusic { get; set; } = "";

    /// <summary>The name of the area's table of wild Pokémon; left out when it has none.</summary>
    public string? Encounters { get; set; }

    public bool Bike { get; set; }
    public bool Running { get; set; }
    public bool EscapeRope { get; set; }
    public bool Fly { get; set; }

    /// <summary>
    /// The wild Pokémon of the area's grass, one per slot of the original's table; the slots are met 20, 20, 10,
    /// 10, 10, 10, 5, 5, 4, 4, 1 and 1 times in a hundred (<see cref="LandSlotWeights"/>). Left out when it has none.
    /// </summary>
    public List<AreaEncounter>? Land { get; set; }

    /// <summary>Platinum's rate for the land table: see <c>EncounterSteps</c>. Left out with the table.</summary>
    public int? LandRate { get; set; }

    public static readonly int[] LandSlotWeights = { 20, 20, 10, 10, 10, 10, 5, 5, 4, 4, 1, 1 };

    /// <summary>
    /// The wild Pokémon met surfing on the area's water: five slots, met 60, 30, 5, 4 and 1 times in a hundred
    /// (<see cref="WaterSlotWeights"/>), each with a range of levels. Left out when it has none.
    /// </summary>
    public List<AreaEncounter>? Water { get; set; }
    public int? WaterRate { get; set; }

    public static readonly int[] WaterSlotWeights = { 60, 30, 5, 4, 1 };

    public List<AreaWarp> Warps { get; set; } = new();
    public List<AreaObject> Objects { get; set; } = new();
    public List<AreaSign> Signs { get; set; } = new();
    public List<AreaTrigger> Triggers { get; set; } = new();
}

/// <summary>One slot of an area's table of wild Pokémon.</summary>
[JsonConverter(typeof(OneLine<AreaEncounter>))]
public sealed class AreaEncounter
{
    public string Species { get; set; } = "";
    public int Level { get; set; }

    /// <summary>For a slot with a range of levels (water), its top; <see cref="Level"/> is then its bottom.</summary>
    public int? MaxLevel { get; set; }
}

/// <summary>
/// Where a region's wild Pokémon live, for the Pokédex's area page (<c>world/&lt;region&gt;/habitats.json</c>,
/// written by <c>tools/MapImporter</c>): every area with wild Pokémon, open or not, and a coarse picture of the
/// overworld to show them on, one character per chunk.
/// </summary>
public sealed class WorldHabitatsFile
{
    public const string FileName = "habitats.json";

    /// <summary>
    /// The overworld's matrix, one row per string and one character per chunk: <c>~</c> mostly water, <c>.</c>
    /// land, <c>T</c> a town or a city, and a space where there is no chunk.
    /// </summary>
    public List<string> Map { get; set; } = new();

    public List<HabitatArea> Areas { get; set; } = new();
}

/// <summary>
/// One area's wild Pokémon by the way they are met: in its grass or cave at each of Platinum's times of day, on its
/// water and with each rod. Swarms, the Poké Radar and the species a second game in the console calls up are left
/// out, as the original's Pokédex leaves them out of its area page.
/// </summary>
public sealed class HabitatArea
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";

    /// <summary>
    /// The chunks of the overworld the area is shown on, <c>"x,y"</c> each, separated by spaces: an outdoor area's
    /// own, or the entrances that lead into a cave or a building. Empty for a place no warp reaches (an island
    /// reached by boat).
    /// </summary>
    public string Cells { get; set; } = "";

    public List<string>? Morning { get; set; }
    public List<string>? Day { get; set; }
    public List<string>? Night { get; set; }
    public List<string>? Surf { get; set; }
    public List<string>? OldRod { get; set; }
    public List<string>? GoodRod { get; set; }
    public List<string>? SuperRod { get; set; }
}

/// <summary>
/// The list of what is built so far of an imported region (<c>world/&lt;region&gt;/world.json</c>, written by
/// hand): the maps the game makes from its matrices, and the areas that are open to walk in. Every other area
/// whose chunks are present is scenery. <c>tools/MapImporter --data</c> reads this file to know which chunks,
/// matrices and areas to write beside it.
/// </summary>
public sealed class WorldIndexFile
{
    public string Region { get; set; } = "";
    public List<WorldMapEntry> Maps { get; set; } = new();
    public List<string> Areas { get; set; } = new();
}

/// <summary>One map the game makes from a matrix: the overworld, or a place that is a matrix of its own.</summary>
public sealed class WorldMapEntry
{
    /// <summary>The map's name in the game: what warps and saves refer to.</summary>
    public string Name { get; set; } = "";
    public int Matrix { get; set; }

    /// <summary>For a matrix that doesn't say which area each chunk belongs to: the area it all is.</summary>
    public string? Area { get; set; }

    /// <summary>The kind of tree that fills its forests where an area doesn't say.</summary>
    public TreeStyle Trees { get; set; } = TreeStyle.Pine;

    /// <summary>What the map is when nothing else is said: open country under the sky, or the inside of a cave.</summary>
    public MapSetting Setting { get; set; } = MapSetting.Outdoors;
}

/// <summary>
/// What the game adds to an imported area (<c>overlays/&lt;key&gt;.json</c>, written by hand): our music, the look of
/// its houses and trees, where its doors lead, and which of the original's people stand there and what they say
/// in our own words. A re-import never touches these files. An area without one is silent scenery.
/// </summary>
public sealed class WorldOverlayFile
{
    public string Area { get; set; } = "";
    public string? BgmTrack { get; set; }
    public TreeStyle? Trees { get; set; }
    /// <summary>How its fences and walls are built, where no building says (a building follows its model).</summary>
    public TownArchitecture? Architecture { get; set; }

    public BattleArena? BattleArena { get; set; }
    public List<string>? EvolutionSites { get; set; }

    /// <summary>Where the area's warps lead among the hand-made maps, by the warp's number in the area file.</summary>
    public List<OverlayDoor>? Doors { get; set; }

    /// <summary>Warps (by number) with nothing behind them yet: their doors stay shut.</summary>
    public List<int>? Locked { get; set; }

    /// <summary>Ways out the original doesn't have: onto a neighbouring map that isn't imported yet.</summary>
    public List<OverlayExit>? Exits { get; set; }

    /// <summary>
    /// Warps into a building that is only passed through (a gate house), by number, each with the warp of the
    /// area it comes out at on the far side: one is put down there as if the building's rooms had been walked.
    /// Its rooms come with plan 01 · M11.
    /// </summary>
    public List<OverlayPassage>? Through { get; set; }

    /// <summary>
    /// Which of the original's people and things appear, by their id in the area file, and who they are here.
    /// Anyone not listed stays away until the story brings them.
    /// </summary>
    public Dictionary<string, OverlayPerson>? People { get; set; }

    /// <summary>
    /// Objects of the area file left out for now, by id: an item on a ledge reached only through a place that
    /// isn't open yet, which would lie where nobody can get to it.
    /// </summary>
    public List<string>? HeldBack { get; set; }

    /// <summary>What the area's signposts and mailboxes say, by their id in the area file.</summary>
    public Dictionary<string, string>? Signs { get; set; }

    /// <summary>Signposts that run a script of the area's file instead of only being read, by their id in the area file.</summary>
    public Dictionary<string, string>? SignScripts { get; set; }

    /// <summary>
    /// Which of the area's triggers start a script, by the trigger's number in the area file. The tiles, the
    /// variable and its value are the original's; the script is ours. A trigger not listed does nothing.
    /// </summary>
    public List<OverlayTrigger>? Triggers { get; set; }

    /// <summary>People of our own, placed in tiles of the area's matrix.</summary>
    public List<MapFile.NpcRecord>? Npcs { get; set; }

    /// <summary>Street furniture of our own, placed in tiles of the area's matrix.</summary>
    public List<MapFile.PropRecord>? Props { get; set; }
}

/// <summary>Where one of an area's warps leads: a tile of a hand-made map.</summary>
[JsonConverter(typeof(OneLine<OverlayDoor>))]
public sealed class OverlayDoor
{
    public int Warp { get; set; }
    public string Map { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public Direction Facing { get; set; } = Direction.Up;
}

/// <summary>A warp into a gate house, and the warp of the area on its far side one comes out at.</summary>
[JsonConverter(typeof(OneLine<OverlayPassage>))]
public sealed class OverlayPassage
{
    public int Warp { get; set; }
    public string To { get; set; } = "";
    public int ToWarp { get; set; }
}

/// <summary>One of an area's triggers given its script: the name of one in the area's script file, or a common one.</summary>
[JsonConverter(typeof(OneLine<OverlayTrigger>))]
public sealed class OverlayTrigger
{
    public int Trigger { get; set; }
    public string Script { get; set; } = "";
}

/// <summary>A tile of an imported area that leads onto a hand-made map.</summary>
[JsonConverter(typeof(OneLine<OverlayExit>))]
public sealed class OverlayExit
{
    public int X { get; set; }
    public int Z { get; set; }
    public string Map { get; set; } = "";
    public int ToX { get; set; }
    public int ToY { get; set; }
    public Direction Facing { get; set; } = Direction.Up;
}

/// <summary>Who one of the original's people is in our game. They stand where the area file puts them.</summary>
public sealed class OverlayPerson
{
    /// <summary>Only for people other data refers to (a trainer's id is their trainer record's).</summary>
    public string? Id { get; set; }
    public string Name { get; set; } = "";

    /// <summary>Which of our characters plays them; left out to go by the original's looks.</summary>
    public string? NpcType { get; set; }
    public List<string>? Dialog { get; set; }
    public bool? IsStarterBriefcase { get; set; }

    /// <summary>The script talking to them runs: one of the area's script file, or a common one. Left out, they do what they are.</summary>
    public string? Script { get; set; }

    /// <summary>
    /// A story flag that takes them off the map while it is set. Left out, it is the one the area file gives the
    /// object (the original's own); <c>""</c> for someone who stays whatever that flag says.
    /// </summary>
    public string? HiddenBy { get; set; }

    /// <summary>A story flag they wait for: they are on the map only while it is set.</summary>
    public string? ShownBy { get; set; }
    public MapFile.TrainerRecord? Trainer { get; set; }
}

/// <summary>A tile that leads elsewhere: to the warp numbered <see cref="ToWarp"/> of the area <see cref="To"/>.</summary>
[JsonConverter(typeof(OneLine<AreaWarp>))]
public sealed class AreaWarp
{
    public int X { get; set; }
    public int Z { get; set; }
    public string To { get; set; } = "";
    public int ToWarp { get; set; }
}

/// <summary>A person or a thing standing on the area: what it looks like, how it moves and which script it runs.</summary>
[JsonConverter(typeof(OneLine<AreaObject>))]
public sealed class AreaObject
{
    public string Id { get; set; } = "";
    public string Looks { get; set; } = "";
    public string Movement { get; set; } = "";
    public int X { get; set; }
    public int Z { get; set; }
    public int Facing { get; set; }
    public int RangeX { get; set; }
    public int RangeZ { get; set; }

    /// <summary>
    /// How high the original stands it, in tiles; left out on the ground. Someone on a bridge's deck has the deck's
    /// height here, and the ground under them may be walked (or be a way into a cave, as under the Cycling Road).
    /// </summary>
    public int? Y { get; set; }

    /// <summary>For a trainer: how they watch for the player; left out for everyone else.</summary>
    public string? Trainer { get; set; }

    /// <summary>For a trainer: how many tiles ahead they see the player.</summary>
    public int? Sight { get; set; }

    /// <summary>The flag that hides the object while set; left out when it is always there.</summary>
    public string? HiddenBy { get; set; }

    /// <summary>For an item lying in its ball: the item, by the game's name for it, and how many when more than one.</summary>
    public string? Item { get; set; }
    public int? Count { get; set; }

    public string Script { get; set; } = "";
}

/// <summary>Something read or found by facing a tile: a signboard, a hidden item.</summary>
[JsonConverter(typeof(OneLine<AreaSign>))]
public sealed class AreaSign
{
    /// <summary>The <see cref="Type"/> of a sign that is an item nobody can see.</summary>
    public const int HiddenItem = 2;

    public int X { get; set; }
    public int Z { get; set; }
    public int Type { get; set; }
    public string Script { get; set; } = "";

    /// <summary>
    /// For a hidden item: the item by the game's name for it, how many when more than one, the flag set once it
    /// has been found, and from how many tiles away the original's Dowsing Machine notices it.
    /// </summary>
    public string? Item { get; set; }
    public int? Count { get; set; }
    public string? Flag { get; set; }
    public int? Range { get; set; }
}

/// <summary>A rectangle that starts a script when stepped on while a story variable has a value.</summary>
[JsonConverter(typeof(OneLine<AreaTrigger>))]
public sealed class AreaTrigger
{
    public int X { get; set; }
    public int Z { get; set; }
    public int Width { get; set; } = 1;
    public int Depth { get; set; } = 1;
    public string Script { get; set; } = "";
    public string Variable { get; set; } = "";
    public string Value { get; set; } = "";
}

/// <summary>
/// Writes a small record on one line, so a chunk's hundred height plates take a hundred lines instead of nine
/// hundred. Reading is the ordinary reading.
/// </summary>
public sealed class OneLine<T> : JsonConverter<T> where T : class, new()
{
    private static readonly JsonSerializerOptions Inner = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
        Converters = { new JsonStringEnumConverter() }
    };

    public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var value = new T();
        foreach (var property in typeof(T).GetProperties().Where(p => p.CanWrite))
        {
            if (!doc.RootElement.TryGetProperty(JsonNamingPolicy.CamelCase.ConvertName(property.Name), out var element)) continue;
            property.SetValue(value, element.Deserialize(property.PropertyType, Inner));
        }
        return value;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        var text = new StringBuilder("{ ");
        bool first = true;
        foreach (var property in typeof(T).GetProperties().Where(p => p.CanRead && p.CanWrite))
        {
            object? field = property.GetValue(value);
            if (field == null) continue;
            if (!first) text.Append(", ");
            first = false;
            text.Append('"').Append(JsonNamingPolicy.CamelCase.ConvertName(property.Name)).Append("\": ");
            text.Append(JsonSerializer.Serialize(field, property.PropertyType, Inner));
        }
        text.Append(" }");
        // The writer puts raw values straight after one another, so each record brings its own line break
        if (writer.Options.Indented) text.Insert(0, "\n" + new string(' ', writer.CurrentDepth * 2));
        writer.WriteRawValue(text.ToString(), skipInputValidation: true);
    }
}
