using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// A named part of a large map: a town or a route of the imported world. It has the name, the music, the wild
/// Pokémon and the look that a small map has for the whole of itself.
/// </summary>
public sealed class MapArea
{
    /// <summary>The area's key in the world files: <c>twinleaf_town</c>.</summary>
    public string Key { get; init; } = "";
    public string DisplayName { get; set; } = "";
    public string BgmTrack { get; set; } = "";

    /// <summary>False for an area that is drawn as scenery but not yet built: nobody can walk into it.</summary>
    public bool Open { get; set; }

    public TreeStyle? Trees { get; set; }

    /// <summary>What falls or hangs in the air here.</summary>
    public FieldWeather Weather { get; set; }

    /// <summary>
    /// For the five places whose weather follows Platinum's calendar (plan 01 · M7): the weather of each day of a
    /// leap year, from the first of January (<see cref="Weathers.CalendarDay"/>). Null for every other area.
    /// </summary>
    public IReadOnlyList<FieldWeather>? Calendar { get; init; }

    /// <summary>The area's weather on a day: the calendar's, where it follows one.</summary>
    public FieldWeather WeatherOn(DateTime day) => Calendar is { Count: 366 } days ? days[Weathers.CalendarDay(day)] : Weather;

    /// <summary>Snow country: more of its open ground is snow than lawn, so snow lies on its trees too.</summary>
    public bool Snowbound { get; set; }

    public Architecture? Architecture { get; set; }
    public BattleArena? Arena { get; set; }

    /// <summary>
    /// The original's battle background for the area (<c>Plain</c>, <c>Forest</c>, <c>City</c>, <c>Mountain</c>,
    /// <c>Snow</c>, <c>Cave1</c>…), which with the tile underfoot picks the ground a battle is fought on
    /// (plan 06 · R6: Camouflage, Nature Power, Secret Power). Empty where the world files give none.
    /// </summary>
    public string BattleBackground { get; set; } = "";
    public List<string> EvolutionSites { get; } = new();

    /// <summary>What the original's header calls the place: <c>Town</c>, <c>Outdoors</c>, <c>Cave</c>, <c>Indoors</c>, <c>PokemonCenter</c> or <c>Underground</c>.</summary>
    public string Kind { get; init; } = "";

    public bool IsTown => Kind == "Town";
    public bool IsCave => Kind == "Cave";

    /// <summary>What the header lets the player do here (plan 02 · S2): ride the Bicycle, use an Escape Rope (and Dig), fly away (and Teleport).</summary>
    public bool BikeAllowed { get; init; }
    public bool EscapeRopeAllowed { get; init; }
    public bool FlyAllowed { get; init; }

    /// <summary>The wild Pokémon of the area's grass and caves, and Platinum's rate for them (see <see cref="EncounterSteps"/>).</summary>
    public List<WildEncounterEntry> WildEncounters { get; } = new();
    public int LandRate { get; set; }

    /// <summary>
    /// What other moments put in some of the land's slots (plan 06 · R13; <see cref="EncounterSlots"/>): the day's
    /// and the night's two (slots 2 and 3), a swarm's two (slots 0 and 1) and the Poké Radar's four (slots 4, 5, 10
    /// and 11). Empty where the area has no grass.
    /// </summary>
    public List<string> DaySlots { get; } = new();
    public List<string> NightSlots { get; } = new();
    public List<string> SwarmSlots { get; } = new();
    public List<string> RadarSlots { get; } = new();

    /// <summary>The wild Pokémon met surfing on the area's water, and the rate for them.</summary>
    public List<WildEncounterEntry> WaterEncounters { get; } = new();
    public int WaterRate { get; set; }

    /// <summary>Shellos and Gastrodon are met here in the east sea's colours, and the Unown in this table's letters (plan 06 · R10).</summary>
    public bool EastSea { get; init; }
    public int UnownTable { get; init; }

    /// <summary>The wild Pokémon hooked with each rod (by <see cref="FishingRod"/>), and each rod's rate (plan 02 · S2).</summary>
    public List<WildEncounterEntry>[] RodEncounters { get; } = { new(), new(), new() };
    public int[] RodRates { get; } = new int[3];
}

public class Map
{
    /// <summary>Tiles along each side of a chunk of a streamed map.</summary>
    public const int ChunkTiles = 32;

    public string Name { get; set; } = "Twinleaf Town";
    public string DisplayName { get; set; } = "Twinleaf Town";
    public string BgmTrack { get; set; } = "";
    public InteriorStyle Interior { get; set; } = InteriorStyle.None;
    public bool IsIndoors => Interior != InteriorStyle.None;

    /// <summary>
    /// Where a room's floor begins: the first column and the first row with anything but wall on them, above the
    /// front wall's row. Its side wall stands west of the one and its back wall north of the other. The rooms made
    /// by hand begin at (1, 2); a room rebuilt to the original's plan begins where the original's floor does, so its
    /// people stand on the original's tiles. Wall tiles inside that are the room's own inner walls.
    /// </summary>
    public (int Left, int Back) RoomCorner()
    {
        int left = Width, back = Height;
        for (int y = 0; y < Height - 1; y++)
            for (int x = 0; x < Width; x++)
            {
                if (groundLayer[y * Width + x] == TileType.Wall) continue;
                left = Math.Min(left, x);
                back = Math.Min(back, y);
            }
        return left < Width ? (left, back) : (1, 2);
    }

    /// <summary>True for maps with a sizeable body of water (not a garden pond): battles there have a lake behind them.</summary>
    public bool HasLake => groundLayer.Count(t => t == TileType.Water) >= 40;

    /// <summary>
    /// Whether a battle that starts at a tile has a lake behind it. A small map is judged as a whole; on a map of
    /// the imported world only the water within sight counts, and a pond is anything smaller than 64 tiles.
    /// </summary>
    public bool HasLakeNear(int x, int y)
    {
        if (!IsStreamed) return HasLake;
        const int reach = 14;
        int water = 0;
        for (int ty = Math.Max(0, y - reach); ty <= Math.Min(Height - 1, y + reach); ty++)
            for (int tx = Math.Max(0, x - reach); tx <= Math.Min(Width - 1, x + reach); tx++)
                if (groundLayer[ty * Width + tx] == TileType.Water) water++;
        return water >= 64;
    }

    public TreeStyle Trees { get; set; } = TreeStyle.Round;

    /// <summary>
    /// What the map is where nothing else is said (plan 01 · M5): open country, or the inside of a cave, whose
    /// rock stands up as walls and which no sky lights and no weather reaches.
    /// </summary>
    public MapSetting Setting { get; set; } = MapSetting.Outdoors;
    public bool IsCave => Setting == MapSetting.Cave;

    /// <summary>The Distortion World (plan 01 · M8): islands over nothing, a light of their own, no weather.</summary>
    public bool IsVoid => Setting == MapSetting.Void;

    /// <summary>The camera the field is looked at with here, as the area's header in the original names it.</summary>
    public FieldCamera Camera { get; set; } = FieldCamera.Default;

    /// <summary>
    /// A place with no light of its own: nothing shows but a circle round the player until a Pokémon lights it
    /// with Flash (the original's <c>DarkFlash</c> weather, which only Wayward Cave has).
    /// </summary>
    public bool IsDark { get; set; }

    /// <summary>
    /// Whether Flash has lit this dark place (plan 02 · S2): the game keeps it from <see cref="FieldMoveRules.FlashFlag"/>
    /// for the map the player is on. A dark map that isn't lit is covered (<see cref="Darkness.Covers"/>).
    /// </summary>
    public bool Lit { get; set; }

    /// <summary>
    /// Whether Defog has blown the fog away here: fog then reads as clear weather (<see cref="WeatherAt"/>), as the
    /// original sets the field's weather to clear. The game keeps it from <see cref="FieldMoveRules.DefogFlag"/>.
    /// </summary>
    public bool FogLifted { get; set; }

    /// <summary>
    /// The Gym's puzzle, for a Gym rebuilt to the original's plan (plan 01 · M9): the flower clock, the dark rooms'
    /// doors, the punching bags, the water. Null everywhere else.
    /// </summary>
    public GymPuzzle? Puzzle { get; set; }

    // ------------------------------------------------------------------ areas of a large map

    private MapArea?[]? areaGrid;
    private readonly List<MapArea> areas = new();

    /// <summary>See <see cref="MapStructures.BuildingsOf"/>.</summary>
    internal List<BuildingInfo>? BuildingCache;

    /// <summary>
    /// The buildings of a map of the imported world, each standing in for one of the original's models
    /// (<see cref="WorldMapBuilder"/>); null on a hand-made map, whose buildings are found from its tiles.
    /// </summary>
    public List<BuildingInfo>? PlacedBuildings { get; set; }

    /// <summary>
    /// True for a map of the imported world: it is too large to draw whole, so it is drawn a chunk at a time,
    /// and its name, music and wild Pokémon change from area to area.
    /// </summary>
    public bool IsStreamed => areaGrid != null;

    public int ChunkColumns => (Width + ChunkTiles - 1) / ChunkTiles;
    public int ChunkRows => (Height + ChunkTiles - 1) / ChunkTiles;

    public IReadOnlyList<MapArea> Areas => areas;

    /// <summary>Says which area a chunk belongs to (and makes this a streamed map).</summary>
    public void SetArea(int chunkX, int chunkY, MapArea area)
    {
        areaGrid ??= new MapArea?[ChunkColumns * ChunkRows];
        if (chunkX < 0 || chunkY < 0 || chunkX >= ChunkColumns || chunkY >= ChunkRows) return;
        areaGrid[chunkY * ChunkColumns + chunkX] = area;
        if (!areas.Contains(area)) areas.Add(area);
    }

    /// <summary>The area a tile lies in; null on a small map, which is one place.</summary>
    public MapArea? AreaAt(int x, int y)
    {
        if (areaGrid == null || !InBounds(x, y)) return null;
        return areaGrid[y / ChunkTiles * ChunkColumns + x / ChunkTiles];
    }

    public MapArea? FindArea(string key) => areas.FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase));

    /// <summary>The tiles an area's chunks span, or null if the map has no such area.</summary>
    public (int X, int Y, int Width, int Height)? AreaBounds(string key)
    {
        if (areaGrid == null || FindArea(key) is not { } area) return null;
        int x0 = int.MaxValue, y0 = int.MaxValue, x1 = -1, y1 = -1;
        for (int cy = 0; cy < ChunkRows; cy++)
            for (int cx = 0; cx < ChunkColumns; cx++)
            {
                if (areaGrid[cy * ChunkColumns + cx] != area) continue;
                x0 = Math.Min(x0, cx); x1 = Math.Max(x1, cx);
                y0 = Math.Min(y0, cy); y1 = Math.Max(y1, cy);
            }
        if (x1 < 0) return null;
        return (x0 * ChunkTiles, y0 * ChunkTiles, (x1 - x0 + 1) * ChunkTiles, (y1 - y0 + 1) * ChunkTiles);
    }

    /// <summary>The name of the place a tile is in: its area's, or the map's own.</summary>
    public string DisplayNameAt(int x, int y) =>
        Core.PlayerIdentity.Fill(AreaAt(x, y)?.DisplayName is { Length: > 0 } name ? name : DisplayName);

    public string BgmTrackAt(int x, int y) => AreaAt(x, y)?.BgmTrack is { Length: > 0 } track ? track : BgmTrack;

    public TreeStyle TreesAt(int x, int y) => AreaAt(x, y)?.Trees ?? Trees;

    /// <summary>The weather of a small map as a whole (a map of the world has it by area: <see cref="WeatherAt"/>).</summary>
    public FieldWeather Weather { get; set; }

    /// <summary>What falls or hangs in the air over a tile; rooms have no weather, and in a cave only fog hangs.</summary>
    public FieldWeather WeatherAt(int x, int y)
    {
        if (IsIndoors) return FieldWeather.Clear;
        // The calendar follows the computer's date, as the original follows the DS's
        var weather = AreaAt(x, y) is { } area ? area.WeatherOn(Core.GameClock.Today) : Weather;
        if (weather == FieldWeather.Fog && FogLifted) return FieldWeather.Clear;
        if (IsVoid) return FieldWeather.Clear;
        return IsCave && weather != FieldWeather.Fog ? FieldWeather.Clear : weather;
    }

    public Architecture ArchitectureAt(int x, int y) => AreaAt(x, y)?.Architecture ?? Architecture;

    public IReadOnlyList<string> EvolutionSitesAt(int x, int y) => AreaAt(x, y) is { } area ? area.EvolutionSites : EvolutionSites;

    /// <summary>The stage this map's battles are fought on; null lets the map decide (a room, or the ground under the player).</summary>
    public BattleArena? Arena { get; set; }

    /// <summary>For a gym or the League: the type the stage is themed on (in the League, null is the Champion's room).</summary>
    public PokemonType? ArenaType { get; set; }

    /// <summary>
    /// The special places this map has that some Pokémon evolve at when they level up there: "Moss Rock",
    /// "Ice Rock", "Magnetic Field", "Stone Arch" (the names the evolutions in species.json use).
    /// </summary>
    public List<string> EvolutionSites { get; set; } = new();

    /// <summary>
    /// The stage for a battle that starts with the player on (<paramref name="x"/>, <paramref name="y"/>):
    /// rooms are indoors unless the map names its stage; outdoors water, sand, snow and cave floors under the
    /// player win over the map's own stage, which is grass if it names none.
    /// </summary>
    public BattleArena ArenaAt(int x, int y)
    {
        if (IsIndoors) return Arena ?? BattleArena.Indoors;
        var named = AreaAt(x, y)?.Arena ?? Arena;
        if (named is BattleArena.Gym or BattleArena.League) return named.Value;
        if (InBounds(x, y))
        {
            switch (GetGroundTile(x, y))
            {
                case TileType.Water: return BattleArena.Water;
                case TileType.Sand: return BattleArena.Sand;
                case TileType.Snow: return BattleArena.Snow;
                case TileType.CaveFloor: return BattleArena.Cave;
                // The Distortion World's own stage comes with its battles (plan 02 · S13); a cave's stands in
                case TileType.DistortionGround or TileType.DistortionSlab: return BattleArena.Cave;
            }
        }
        return named ?? BattleArena.Grass;
    }

    /// <summary>How the houses of this town are built.</summary>
    public Architecture Architecture { get; set; } = Architecture.Timber;

    /// <summary>
    /// What kind of building covers a tile, where the map says so. Other buildings are told by where their door
    /// leads (see <see cref="MapStructures.FindBuildings"/>).
    /// </summary>
    public Dictionary<(int X, int Y), BuildingKind> BuildingKinds { get; } = new();
    public int Width { get; }
    public int Height { get; }

    private readonly TileType[] groundLayer;
    private readonly TileType?[] overheadLayer;
    private readonly bool[] solidGrid;

    // ------------------------------------------------------------------ what each tile does, and how high it lies

    // A tile that has no behaviour of its own takes the one its type implies
    private const TileBehavior Implied = (TileBehavior)0xFF;
    private TileBehavior[]? behaviours;
    private float[]? heights, slopesX, slopesZ, decks;

    /// <summary>
    /// What a tile does: one of Platinum's tile behaviours. A map of the world carries the original's value for
    /// every tile; on a hand-made map a tile does what its type implies (tall grass, water, a ledge, ice) unless
    /// <see cref="SetBehaviour"/> has said otherwise. <see cref="FieldMovement"/> holds the rules.
    /// </summary>
    public TileBehavior BehaviourAt(int x, int y)
    {
        if (!InBounds(x, y)) return TileBehavior.None;
        int i = y * Width + x;
        if (behaviours != null && behaviours[i] != Implied) return behaviours[i];
        return groundLayer[i] switch
        {
            TileType.TallGrass => TileBehavior.TallGrass,
            TileType.Water => TileBehavior.Sea,
            TileType.LedgeDown => TileBehavior.LedgeSouth,
            TileType.LedgeLeft => TileBehavior.LedgeWest,
            TileType.LedgeRight => TileBehavior.LedgeEast,
            TileType.Door => TileBehavior.Door,
            TileType.Sand => TileBehavior.Sand,
            TileType.CaveFloor => TileBehavior.CaveFloor,
            TileType.Snow => TileBehavior.ShallowSnow,
            TileType.Ice => TileBehavior.Ice,
            TileType.Marsh => TileBehavior.Mud,
            TileType.Puddle => TileBehavior.Puddle,
            _ => TileBehavior.None
        };
    }

    /// <summary>A tile's behaviour where it was given one of its own (<see cref="SetBehaviour"/>); null where it does what its type implies.</summary>
    public TileBehavior? OwnBehaviourAt(int x, int y)
    {
        if (behaviours == null || !InBounds(x, y)) return null;
        var own = behaviours[y * Width + x];
        return own == Implied ? null : own;
    }

    public void SetBehaviour(int x, int y, TileBehavior behaviour)
    {
        if (!InBounds(x, y)) return;
        if (behaviours == null)
        {
            behaviours = new TileBehavior[Width * Height];
            Array.Fill(behaviours, Implied);
        }
        behaviours[y * Width + x] = behaviour;
    }

    /// <summary>True once any tile has been given a height: the map has relief to draw and to walk.</summary>
    public bool HasRelief => heights != null;

    /// <summary>
    /// The height of the map's lowlands, which the field draws at zero (Sinnoh's is one tile, in the original's
    /// numbers). Heights are walked as the map gives them; only the drawing is measured from here.
    /// </summary>
    public float GroundLevel { get; set; }

    /// <summary>A tile's rise per tile eastward and southward: zero on flat ground, something on stairs and ramps.</summary>
    public (float X, float Z) SlopeAt(int x, int y)
    {
        if (slopesX == null || !InBounds(x, y)) return (0f, 0f);
        return (slopesX[y * Width + x], slopesZ![y * Width + x]);
    }

    /// <summary>The height of the ground at the middle of a tile, in tiles (0 on a map without relief).</summary>
    public float HeightAt(int x, int y) => heights != null && InBounds(x, y) ? heights[y * Width + x] : 0f;

    /// <summary>
    /// The height of the ground at any point of a tile: flat, or on stairs and ramps the plane through the tile's
    /// middle that rises by its slopes per tile eastward and southward.
    /// </summary>
    public float HeightAt(int x, int y, float fx, float fz)
    {
        if (heights == null || !InBounds(x, y)) return 0f;
        int i = y * Width + x;
        float h = heights[i];
        if (slopesX != null) h += (fx - 0.5f) * slopesX[i] + (fz - 0.5f) * slopesZ![i];
        return h;
    }

    /// <summary>Sets a tile's ground: its height at the tile's middle and, for stairs and ramps, its rise per tile eastward and southward.</summary>
    public void SetHeight(int x, int y, float height, float slopeX = 0f, float slopeZ = 0f)
    {
        if (!InBounds(x, y)) return;
        heights ??= new float[Width * Height];
        int i = y * Width + x;
        heights[i] = height;
        if (slopeX == 0f && slopeZ == 0f && slopesX == null) return;
        slopesX ??= new float[Width * Height];
        slopesZ ??= new float[Width * Height];
        slopesX[i] = slopeX;
        slopesZ[i] = slopeZ;
    }

    /// <summary>The height of a bridge's deck over a tile, or null where there is none. The ground runs on underneath.</summary>
    public float? DeckAt(int x, int y)
    {
        if (decks == null || !InBounds(x, y)) return null;
        float deck = decks[y * Width + x];
        return float.IsNaN(deck) ? null : deck;
    }

    public void SetDeck(int x, int y, float height)
    {
        if (!InBounds(x, y)) return;
        if (decks == null)
        {
            decks = new float[Width * Height];
            Array.Fill(decks, float.NaN);
        }
        decks[y * Width + x] = height;
    }

    /// <summary>
    /// What someone coming from a height stands on at a tile: the deck of a bridge or the ground under it,
    /// whichever is nearer to where they were, as Platinum chooses between overlapping plates.
    /// </summary>
    public (float Height, bool OnDeck) SurfaceAt(int x, int y, float from)
    {
        var (height, onDeck, _) = StandAt(x, y, from);
        return (height, onDeck);
    }

    /// <summary>
    /// What someone coming from a height stands on at a tile, as <see cref="SurfaceAt"/>, and whether it is the water
    /// of a Gym's puzzle that sets the height there (the Pastoria Gym's, <see cref="GymPuzzle.WaterAt"/>): the
    /// original's <c>GetHeight</c> in <c>terrain_collision_manager.c</c>. Over the ground and a deck the nearer one
    /// is chosen as ever; the water is stood on where it lies above that and is nearer still.
    /// </summary>
    public (float Height, bool OnDeck, bool OnWater) StandAt(int x, int y, float from)
    {
        float ground = HeightAt(x, y);
        float height = ground;
        bool onDeck = false;
        if (DeckAt(x, y) is { } deck && MathF.Abs(deck - from) < MathF.Abs(ground - from)) (height, onDeck) = (deck, true);
        if (Puzzle?.WaterAt(x, y) is { } water && water > height && MathF.Abs(water - from) < MathF.Abs(height - from))
            return (water, false, true);
        return (height, onDeck, false);
    }

    public List<NPC> NPCs { get; } = new();

    /// <summary>
    /// People the story keeps off the map for now: a flag hides them, or a script sent them away (plan 02 · S1).
    /// They are neither seen nor in the way, so whatever asks <see cref="NPCs"/> goes on as if they weren't there.
    /// </summary>
    public List<NPC> Absent { get; } = new();

    /// <summary>Everyone the map has, on it or off it.</summary>
    public IEnumerable<NPC> Everyone => NPCs.Concat(Absent);

    private int peopleCounted;

    /// <summary>Puts everyone where the story has them, on the map or off it, each back in their own place in the list.</summary>
    public void ApplyPresence(Func<string, bool> flagSet)
    {
        var everyone = Everyone.ToList();
        foreach (var npc in everyone)
            if (npc.Order < 0) npc.Order = peopleCounted++;
        // Nothing to do is the usual case, and then the lists are left alone
        if (NPCs.All(n => n.IsPresent(flagSet)) && Absent.All(n => !n.IsPresent(flagSet))) return;

        everyone.Sort((a, b) => a.Order.CompareTo(b.Order));
        NPCs.Clear();
        Absent.Clear();
        foreach (var npc in everyone) (npc.IsPresent(flagSet) ? NPCs : Absent).Add(npc);
    }

    /// <summary>
    /// Locks and unlocks the doors that wait for the story (<see cref="Warp.OpenedBy"/>): a locked door's tile stands
    /// in the way like a wall, an open one is walked into as ever.
    /// </summary>
    public void ApplyDoors(Func<string, bool> flagSet)
    {
        foreach (var warp in Warps)
            if (warp.OpenedBy is { } flag) SetSolid(warp.SourceX, warp.SourceY, !flagSet(flag));
    }

    /// <summary>Forgets who a script showed or hid by itself: from here on the flags decide again.</summary>
    public void ForgetForced()
    {
        foreach (var npc in Everyone) npc.Forced = null;
    }

    /// <summary>
    /// Someone of the map by what scripts call them (<see cref="NPC.Key"/>) or, failing that, by their name, on
    /// the map or off it. A map of the world has a "clown_1" in more than one town, so a script's names mean the
    /// people of its own place: with <paramref name="place"/> (an area's key) given, nobody of another area is
    /// found, whatever they are called.
    /// </summary>
    public NPC? FindPerson(string name, string? place = null)
    {
        NPC? byName = null;
        foreach (var npc in Everyone)
        {
            if (place != null && npc.ScriptFile != null && !string.Equals(npc.ScriptFile, place, StringComparison.OrdinalIgnoreCase)) continue;
            if (string.Equals(npc.Key, name, StringComparison.OrdinalIgnoreCase)) return npc;
            if (byName == null && string.Equals(npc.Name, name, StringComparison.OrdinalIgnoreCase)) byName = npc;
        }
        return byName;
    }

    /// <summary>Tiles that start a script when stepped on (plan 02 · S1).</summary>
    public List<StepTrigger> Triggers { get; } = new();

    /// <summary>The triggers that cover a tile, in the order the map has them.</summary>
    public IEnumerable<StepTrigger> TriggersAt(int x, int y) => Triggers.Where(t => t.Covers(x, y));

    /// <summary>Signboards that run a script of their own instead of only being read, by their tile.</summary>
    public Dictionary<(int X, int Y), string> SignScripts { get; } = new();

    /// <summary>Items hidden in the ground, by their tile (plan 02).</summary>
    public Dictionary<(int X, int Y), HiddenItem> HiddenItems { get; } = new();

    /// <summary>
    /// Tiles that run a script when faced (plan 01 · M8): the original's read events that aren't signposts, such as
    /// an inscription in a cave or a pillar. The script is the place's own.
    /// </summary>
    public Dictionary<(int X, int Y), string> TileScripts { get; } = new();

    /// <summary>The file a place's scripts are written in: its area's key on a map of the world, the map's name otherwise.</summary>
    public string ScriptFileAt(int x, int y) => InBounds(x, y) && AreaAt(x, y) is { Key.Length: > 0 } area ? area.Key : Name;

    public List<Prop> Props { get; } = new();
    public List<Warp> Warps { get; } = new();
    public List<WildEncounterEntry> WildEncounters { get; } = new();
    public Dictionary<(int X, int Y), string> Signboards { get; } = new();

    private readonly Random rng = Core.Dice.New();

    public Map(int width, int height)
    {
        Width = width;
        Height = height;
        groundLayer = new TileType[width * height];
        overheadLayer = new TileType?[width * height];
        solidGrid = new bool[width * height];

        for (int i = 0; i < groundLayer.Length; i++)
        {
            groundLayer[i] = TileType.Grass;
        }
    }

    public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public TileType GetGroundTile(int x, int y) => groundLayer[y * Width + x];

    public void SetGroundTile(int x, int y, TileType type, bool isSolid = false)
    {
        if (!InBounds(x, y)) return;
        int idx = y * Width + x;
        groundLayer[idx] = type;
        solidGrid[idx] = isSolid;
    }

    public TileType? GetOverheadTile(int x, int y) => overheadLayer[y * Width + x];

    /// <summary>Whether the tile itself blocks movement (walls, water, furniture), ignoring NPCs.</summary>
    public bool IsSolid(int x, int y) => solidGrid[y * Width + x];

    public void SetOverheadTile(int x, int y, TileType type)
    {
        if (!InBounds(x, y)) return;
        overheadLayer[y * Width + x] = type;
    }

    // Where each obstacle stood when the map was made, to put a pushed boulder back
    private readonly Dictionary<NPC, (int X, int Y)> obstacleHomes = new();

    /// <summary>Puts an obstacle that a field move clears on a tile: an object of the map, like an item's ball (plan 02 · S2).</summary>
    public NPC AddObstacle(PropType obstacle, int x, int y)
    {
        var thing = new NPC
        {
            Name = obstacle switch { PropType.CutTree => "Tree", PropType.CrackedRock => "Rock", _ => "Boulder" },
            NpcType = NPC.TypeOf(obstacle),
            GridX = x,
            GridY = y
        };
        Add(thing);
        return thing;
    }

    /// <summary>Someone or something of the map; an obstacle has the tile it stands on kept, to be put back there (<see cref="ResetObstacles"/>).</summary>
    public void Add(NPC npc)
    {
        NPCs.Add(npc);
        if (npc.IsObstacle) obstacleHomes[npc] = (npc.GridX, npc.GridY);
    }

    /// <summary>
    /// Puts every boulder that was pushed back where it stood, as the original's objects are when their map is
    /// loaded again: the game does it when the player comes to another place.
    /// </summary>
    public void ResetObstacles()
    {
        foreach (var (thing, (x, y)) in obstacleHomes)
        {
            (thing.GridX, thing.GridY) = (x, y);
            thing.StepOffsetX = thing.StepOffsetY = 0f;
        }
    }

    /// <summary>Places furniture or decoration; furniture makes the tiles it covers solid.</summary>
    public Prop AddProp(PropType type, int x, int y, int width = 1, int depth = 1)
    {
        var prop = new Prop { Type = type, X = x, Y = y, Width = width, Depth = depth };
        Props.Add(prop);
        if (prop.IsSolid)
        {
            for (int ty = y; ty < y + depth; ty++)
                for (int tx = x; tx < x + width; tx++)
                    SetSolid(tx, ty, true);
        }
        return prop;
    }

    /// <summary>A counter or a table one talks across: a room's own, or a tile of the world that the original marks as one (Amity Square's gates).</summary>
    public bool IsCounter(int x, int y) => Props.Any(p => p.IsCounter && p.Covers(x, y)) || (behaviours != null && BehaviourAt(x, y) == TileBehavior.Counter);

    // ------------------------------------------------------------------ places the story reveals (plan 01 · M8)

    /// <summary>
    /// A part of the region the original leaves out until the story reveals it: the Spring Path, whose chunks
    /// are forest (<c>MapMatrix_RevealSpringPath</c>) until a variable holds the original's number for it. The map
    /// is built with the place in it and keeps a copy of everything that stands there; hiding it makes its tiles
    /// forest, of no area, with nothing on them, and revealing it puts the copy back.
    /// </summary>
    public sealed class HiddenPlace
    {
        public string Var { get; init; } = "";
        public int Value { get; init; }

        /// <summary>The tiles it covers: whole chunks.</summary>
        public int X { get; init; }
        public int Y { get; init; }
        public int Width { get; init; }
        public int Height { get; init; }

        /// <summary>Whether the map shows forest there now.</summary>
        public bool Hidden { get; internal set; }

        internal TileType[] Ground = Array.Empty<TileType>();
        internal TileType?[] Overhead = Array.Empty<TileType?>();
        internal bool[] Solid = Array.Empty<bool>();
        internal TileBehavior[]? Behaviours;
        internal float[]? Heights, SlopesX, SlopesZ, Decks;
        internal MapArea?[] AreaCells = Array.Empty<MapArea?>();
        internal List<Warp> Warps = new();
        internal List<Prop> Props = new();
        internal List<NPC> People = new();
        internal List<StepTrigger> Triggers = new();
        internal List<KeyValuePair<(int X, int Y), string>> Signboards = new(), SignScripts = new(), TileScripts = new();
        internal List<KeyValuePair<(int X, int Y), HiddenItem>> HiddenItems = new();

        public bool Covers(int x, int y) => x >= X && y >= Y && x < X + Width && y < Y + Height;
    }

    public List<HiddenPlace> HiddenPlaces { get; } = new();

    /// <summary>Keeps a copy of the chunks given (in chunks, from their top left one) as a place the story reveals; shown until <see cref="ApplyHiddenPlaces"/> hides it.</summary>
    public HiddenPlace AddHiddenPlace(string variable, int value, int chunkX, int chunkY, int chunksWide, int chunksHigh)
    {
        int x0 = chunkX * ChunkTiles, y0 = chunkY * ChunkTiles;
        int w = Math.Min(chunksWide * ChunkTiles, Width - x0), h = Math.Min(chunksHigh * ChunkTiles, Height - y0);
        var place = new HiddenPlace { Var = variable, Value = value, X = x0, Y = y0, Width = w, Height = h };
        T[] Copy<T>(T[] all)
        {
            var part = new T[w * h];
            for (int y = 0; y < h; y++) Array.Copy(all, (y0 + y) * Width + x0, part, y * w, w);
            return part;
        }
        place.Ground = Copy(groundLayer);
        place.Overhead = Copy(overheadLayer);
        place.Solid = Copy(solidGrid);
        place.Behaviours = behaviours != null ? Copy(behaviours) : null;
        place.Heights = heights != null ? Copy(heights) : null;
        place.SlopesX = slopesX != null ? Copy(slopesX) : null;
        place.SlopesZ = slopesZ != null ? Copy(slopesZ) : null;
        place.Decks = decks != null ? Copy(decks) : null;
        place.AreaCells = new MapArea?[chunksWide * chunksHigh];
        for (int cy = 0; cy < chunksHigh; cy++)
            for (int cx = 0; cx < chunksWide; cx++)
                place.AreaCells[cy * chunksWide + cx] = areaGrid?[(chunkY + cy) * ChunkColumns + chunkX + cx];
        place.Warps.AddRange(Warps.Where(wp => place.Covers(wp.SourceX, wp.SourceY)));
        place.Props.AddRange(Props.Where(pr => place.Covers(pr.X, pr.Y)));
        place.People.AddRange(Everyone.Where(n => place.Covers(n.GridX, n.GridY)));
        place.Triggers.AddRange(Triggers.Where(t => place.Covers(t.X, t.Y)));
        place.Signboards.AddRange(Signboards.Where(kv => place.Covers(kv.Key.X, kv.Key.Y)));
        place.SignScripts.AddRange(SignScripts.Where(kv => place.Covers(kv.Key.X, kv.Key.Y)));
        place.TileScripts.AddRange(TileScripts.Where(kv => place.Covers(kv.Key.X, kv.Key.Y)));
        place.HiddenItems.AddRange(HiddenItems.Where(kv => place.Covers(kv.Key.X, kv.Key.Y)));
        HiddenPlaces.Add(place);
        return place;
    }

    /// <summary>
    /// Hides or reveals each of the map's hidden places as the story's variables say. Returns the places whose
    /// ground changed, so whatever was drawn of them can be made again.
    /// </summary>
    public List<HiddenPlace> ApplyHiddenPlaces(Func<string, int> variable)
    {
        var changed = new List<HiddenPlace>();
        foreach (var place in HiddenPlaces)
        {
            bool hide = variable(place.Var) != place.Value;
            if (hide == place.Hidden) continue;
            if (hide) Hide(place); else Reveal(place);
            place.Hidden = hide;
            BuildingCache = null;
            changed.Add(place);
        }
        return changed;
    }

    private void Hide(HiddenPlace place)
    {
        for (int y = place.Y; y < place.Y + place.Height; y++)
            for (int x = place.X; x < place.X + place.Width; x++)
            {
                int i = y * Width + x;
                groundLayer[i] = IsCave ? TileType.CaveWall : IsVoid ? TileType.Void : TileType.Tree;
                overheadLayer[i] = null;
                solidGrid[i] = true;
                if (behaviours != null) behaviours[i] = Implied;
                if (heights != null) heights[i] = GroundLevel;
                if (slopesX != null) slopesX[i] = slopesZ![i] = 0f;
                if (decks != null) decks[i] = float.NaN;
            }
        int columns = place.Width / ChunkTiles, rows = place.Height / ChunkTiles;
        if (areaGrid != null)
            for (int cy = 0; cy < rows; cy++)
                for (int cx = 0; cx < columns; cx++)
                    areaGrid[(place.Y / ChunkTiles + cy) * ChunkColumns + place.X / ChunkTiles + cx] = null;
        Warps.RemoveAll(place.Warps.Contains);
        Props.RemoveAll(place.Props.Contains);
        NPCs.RemoveAll(place.People.Contains);
        Absent.RemoveAll(place.People.Contains);
        Triggers.RemoveAll(place.Triggers.Contains);
        foreach (var kv in place.Signboards) Signboards.Remove(kv.Key);
        foreach (var kv in place.SignScripts) SignScripts.Remove(kv.Key);
        foreach (var kv in place.TileScripts) TileScripts.Remove(kv.Key);
        foreach (var kv in place.HiddenItems) HiddenItems.Remove(kv.Key);
    }

    private void Reveal(HiddenPlace place)
    {
        int w = place.Width;
        void Put<T>(T[] part, T[] all)
        {
            for (int y = 0; y < place.Height; y++) Array.Copy(part, y * w, all, (place.Y + y) * Width + place.X, w);
        }
        Put(place.Ground, groundLayer);
        Put(place.Overhead, overheadLayer);
        Put(place.Solid, solidGrid);
        if (place.Behaviours != null && behaviours != null) Put(place.Behaviours, behaviours);
        if (place.Heights != null && heights != null) Put(place.Heights, heights);
        if (place.SlopesX != null && slopesX != null) { Put(place.SlopesX, slopesX); Put(place.SlopesZ!, slopesZ!); }
        if (place.Decks != null && decks != null) Put(place.Decks, decks);
        int columns = place.Width / ChunkTiles, rows = place.Height / ChunkTiles;
        if (areaGrid != null)
            for (int cy = 0; cy < rows; cy++)
                for (int cx = 0; cx < columns; cx++)
                    areaGrid[(place.Y / ChunkTiles + cy) * ChunkColumns + place.X / ChunkTiles + cx] = place.AreaCells[cy * columns + cx];
        Warps.AddRange(place.Warps);
        Props.AddRange(place.Props);
        // Whoever the flags hide goes back among the absent at the next presence check
        NPCs.AddRange(place.People);
        Triggers.AddRange(place.Triggers);
        foreach (var kv in place.Signboards) Signboards[kv.Key] = kv.Value;
        foreach (var kv in place.SignScripts) SignScripts[kv.Key] = kv.Value;
        foreach (var kv in place.TileScripts) TileScripts[kv.Key] = kv.Value;
        foreach (var kv in place.HiddenItems) HiddenItems[kv.Key] = kv.Value;
    }

    public void SetSolid(int x, int y, bool isSolid)
    {
        if (InBounds(x, y))
        {
            solidGrid[y * Width + x] = isSolid;
        }
    }

    /// <summary>
    /// Whether someone on foot can stand on a tile: it isn't blocked, nobody is on it, and it is neither a ledge
    /// nor water deep enough to need Surf. Whether a step onto it can be taken from a given place and height is
    /// <see cref="FieldMovement.Step"/>'s to say.
    /// </summary>
    public bool IsWalkable(int x, int y)
    {
        if (!InBounds(x, y)) return false;
        int idx = y * Width + x;

        if (solidGrid[idx]) return false;

        if (NPCs.Any(n => n.GridX == x && n.GridY == y)) return false;

        var behaviour = BehaviourAt(x, y);
        if (FieldMovement.LedgeDirection(behaviour) != null) return false;
        if (TileBehaviors.IsSurfable(behaviour) && DeckAt(x, y) == null) return false;

        return true;
    }

    /// <summary>A ledge that is hopped over, whichever way it faces.</summary>
    public bool IsLedge(int x, int y) => FieldMovement.LedgeDirection(BehaviourAt(x, y)) != null;

    public bool IsTallGrass(int x, int y) => BehaviourAt(x, y) is TileBehavior.TallGrass or TileBehavior.VeryTallGrass;

    /// <summary>Water deep enough to need Surf.</summary>
    public bool IsDeepWater(int x, int y) => TileBehaviors.IsSurfable(BehaviourAt(x, y));

    public Warp? GetWarpAt(int x, int y)
    {
        return Warps.FirstOrDefault(w => w.SourceX == x && w.SourceY == y);
    }

    public string? GetSignboardAt(int x, int y)
    {
        return Signboards.TryGetValue((x, y), out var text) ? text : null;
    }

    public NPC? GetNpcAt(int x, int y)
    {
        return NPCs.FirstOrDefault(n => n.GridX == x && n.GridY == y);
    }

    /// <summary>
    /// Someone walking behind the player (plan 02 · S6, <see cref="Overworld.Follower"/>): spoken to like anyone, but
    /// never in the player's way, so walking back into them swaps the two round.
    /// </summary>
    public NPC? Follower { get; set; }

    /// <summary>
    /// Whoever stands on a tile at about a height: someone on a bridge's deck is not in the way of anyone on the
    /// ground under it, nor the other way round.
    /// </summary>
    public NPC? NpcIn(int x, int y, float height) =>
        NPCs.FirstOrDefault(n => n.GridX == x && n.GridY == y && MathF.Abs((n.Level ?? HeightAt(x, y)) - height) < FieldMovement.StepLimit);

    /// <summary>Platinum's rate for a small map's own wild Pokémon (see <see cref="EncounterSteps"/>).</summary>
    public int EncounterRate { get; set; } = 30;

    /// <summary>
    /// The wild Pokémon that live at a tile, on its land or in its water, with the place's rate for them: its
    /// area's on a map of the world, the map's own otherwise (a small map has no water table). At a moment
    /// (<see cref="EncounterMoment"/>) the grass's slots are the moment's: the time of day's, a swarm's, the Trophy
    /// Garden's and the Great Marsh's species in their places (plan 06 · R13, <see cref="EncounterSlots"/>).
    /// </summary>
    public (IReadOnlyList<WildEncounterEntry> Table, int Rate) WildAt(int x, int y, bool water = false, EncounterMoment? moment = null)
    {
        if (AreaAt(x, y) is { } area)
            return water ? (area.WaterEncounters, area.WaterRate) : (moment == null ? area.WildEncounters : EncounterSlots.Grass(area, moment), area.LandRate);
        return water ? (Array.Empty<WildEncounterEntry>(), 0) : (WildEncounters, EncounterRate);
    }

    /// <summary>
    /// What bites a rod cast into the water at a tile (plan 02 · S2): the rod's rate decides whether anything does
    /// (<c>WildEncounters_TryFishingEncounter</c>), then one of its five slots by weight, as for the water, with the
    /// lead's ability having its say (Keen Eye and Intimidate, but never a Repel). On one of the day's Feebas tiles
    /// every slot is Feebas, one time in two (plan 06 · R13, <see cref="Feebas"/>). Null when nothing will bite; a
    /// small map's water has nothing in it.
    /// </summary>
    public WildEncounterEntry? Fish(int x, int y, FishingRod rod, WildLead? lead = null, EncounterMoment? moment = null)
    {
        if (AreaAt(x, y) is not { } area) return null;
        IReadOnlyList<WildEncounterEntry> table = area.RodEncounters[(int)rod];
        int rate = area.RodRates[(int)rod];
        if (table.Count == 0 || rate <= 0 || rng.Next(100) >= rate) return null;
        var feebas = SpecialEncounterTables.Sinnoh.Feebas;
        if (moment?.State is { } state && feebas.Area == area.Key
            && Feebas.Bites(SpecialEncounterTables.FeebasTiles, state.DailyNumber, x, y, rng))
            table = Overworld.Feebas.Table(feebas.Species, table);
        return WildEncounterRules.Meet(table, water: true, lead is { } first ? first with { RepelLevel = null } : null, rng);
    }

    /// <summary>Whether the Bicycle may be ridden at a tile: as its area's header says, and outdoors on a small map, never in a room.</summary>
    public bool BikeAllowedAt(int x, int y) => AreaAt(x, y)?.BikeAllowed ?? !IsIndoors;

    /// <summary>
    /// A wild Pokémon drawn out at once from the place's table, with no odds to meet first: Sweet Scent and Honey
    /// (plan 02 · S2, <c>WildEncounters_TrySweetScentEncounter</c>). Neither Keen Eye nor a Repel keeps it away, and a
    /// roamer where the player stands comes one time in two (plan 06 · R13). Null only where nothing lives.
    /// </summary>
    public WildEncounterEntry? DrawOutWild(int x, int y, bool water = false, WildLead? lead = null, EncounterMoment? moment = null)
    {
        var (table, _) = WildAt(x, y, water, moment);
        if (table.Count == 0) return null;
        if (moment is { Partner: false, State: { } state } && Roamers.MeetHere(state, AreaAt(x, y)?.Key, rng) is { } roamer)
            return RoamerMet(state, roamer);
        return WildEncounterRules.Meet(table, water, lead, rng, keptAway: false);
    }

    /// <summary>
    /// A wild Pokémon for a step onto a tile, or null: Platinum's odds for the step (<see cref="EncounterSteps.Meets"/>)
    /// at the place's rate, then one of the place's table by weight, with its level decided. The ability of the
    /// Pokémon at the head of the party has its say in each (<see cref="WildEncounterRules"/>). At a moment
    /// (<see cref="EncounterMoment"/>, plan 06 · R13) the table is the moment's, a shaking patch of the Poké Radar
    /// always meets its Pokémon, and a roamer where the player stands is met one time in two.
    /// </summary>
    /// <param name="thick">In grass taller than the walker, or on a Bicycle: more attempts get through.</param>
    /// <param name="lead">The Pokémon at the head of the party; left out, nothing shapes the meeting.</param>
    public WildEncounterEntry? RollWildEncounter(int x, int y, EncounterSteps steps, bool water = false, bool thick = false, WildLead? lead = null, EncounterMoment? moment = null)
    {
        var (table, rate) = WildAt(x, y, water, moment);
        if (table.Count == 0 || rate <= 0) return null;
        rate = WildEncounterRules.Rate(rate, lead, WeatherAt(x, y));
        bool meets = steps.Meets(rate, thick, rng, moment?.Today);
        // A shaking patch always has its Pokémon (PokeRadar_ShouldDoRadarEncounter)
        var radar = moment?.Radar?.StepOnto(x, y);
        if (!meets && radar == null) return null;

        var area = AreaAt(x, y);
        // A roamer where the player stands, but never in a patch or beside a partner (TryEncounterRoamer); a Repel
        // keeps it away as it would anything of its level
        if (radar == null && moment is { Partner: false, State: { } state } && Roamers.MeetHere(state, area?.Key, rng) is { } roamer)
            return WildEncounterRules.RepelTurnsAway(lead, state.Roamers[roamer].Level) ? null : RoamerMet(state, roamer);

        if (radar is { } patch && moment?.Radar is { } chain && !water)
            return chain.Meet(table, area, patch, lead, this, x, y, moment.Height, rng);
        var met = WildEncounterRules.Meet(table, water, lead, rng);
        // Any other Pokémon of the grass ends the Poké Radar's chain
        if (met != null && !water) moment?.Radar?.Clear();
        return met;
    }

    private static WildEncounterEntry RoamerMet(Models.SpecialEncounters state, int slot)
    {
        var roamer = state.Roamers[slot];
        return new WildEncounterEntry { SpeciesName = roamer.Species, MinLevel = roamer.Level, MaxLevel = roamer.Level, Roamer = slot };
    }

    /// <summary>
    /// A second Pokémon of the land's table beside one already met, as a partner's battles bring
    /// (<c>TryGenerateGrassEncounter_DoubleBattle</c>, plan 02 · S6): drawn as the first was, or null when the lead
    /// scared it off.
    /// </summary>
    public WildEncounterEntry? MeetAnother(int x, int y, WildLead? lead = null, EncounterMoment? moment = null)
    {
        var (table, _) = WildAt(x, y, moment: moment);
        return WildEncounterRules.Meet(table, false, lead, rng);
    }
}

/// <summary>
/// Platinum's odds that a step onto ground where Pokémon live meets one (<c>ShouldGetRandomEncounter</c> in the
/// decompilation). It keeps count of the attempts since the last battle or map change, because the first few of
/// them almost always fail.
/// </summary>
public sealed class EncounterSteps
{
    private int attempts;

    /// <summary>A wild battle has ended, or the map has changed: the next steps are nearly safe again.</summary>
    public void Reset() => attempts = 0;

    /// <summary>
    /// Whether this step meets a Pokémon. For the first steps after a reset (eight, less one for every ten of
    /// the place's rate) nineteen attempts in twenty fail outright. After that an attempt gets through four
    /// times in ten (seven in ten where <paramref name="thick"/>), and one that gets through succeeds as often
    /// as the place's <paramref name="rate"/> out of a hundred.
    /// </summary>
    /// <param name="today">The day, whose date may change the odds that an attempt gets through (<see cref="SpecialDates"/>); left out, it doesn't.</param>
    public bool Meets(int rate, bool thick, Random rng, DateTime? today = null)
    {
        if (rate <= 0) return false;
        if (attempts < 8 - Math.Min(8, rate / 10))
        {
            attempts++;
            if (rng.Next(100) >= 5) return false;
        }
        int flat = thick ? 70 : 40;
        if (today is { } day) flat = Math.Min(100, SpecialDates.ModifyEncounterRate(flat, day));
        return rng.Next(100) < flat && rng.Next(100) < rate;
    }
}
