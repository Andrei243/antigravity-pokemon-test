using System;
using System.Collections.Generic;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

public enum TileType : byte
{
    Grass,
    FlowerGrass,
    TallGrass,
    Path,
    Water,
    LedgeDown,
    Tree,
    TreeTrunk,
    RoofRed,
    RoofBlue,
    Wall,
    Door,
    Floor,
    Signpost,
    PC,
    RoofGreen,

    // Ground kinds beyond lawn and path (drawn by PixelGround; plan 01 places them)
    Sand,
    Dirt,
    Snow,
    CaveFloor,

    // Terrain features (plan 01 · M3). A tile's type is what it looks like; what it does is its TileBehavior,
    // which on a hand-made map follows from the type (Map.BehaviourAt)
    /// <summary>A ledge hopped over westward.</summary>
    LedgeLeft,
    /// <summary>A ledge hopped over eastward.</summary>
    LedgeRight,
    /// <summary>Bare rock underfoot: the tops of cliffs and crags.</summary>
    Rock,
    Ice,
    /// <summary>The deck of a bridge or a boardwalk.</summary>
    Planks,
    Stairs,
    /// <summary>Marsh mud.</summary>
    Marsh,

    // The made ground of towns (plan 01 · M4)
    /// <summary>Stone slabs: the streets and squares of cities.</summary>
    Paving,
    /// <summary>The deck of Sunyshore's raised walkways: steel, set with solar panels.</summary>
    Walkway,

    // Caves (plan 01 · M5)
    /// <summary>The rock a cave is cut into: nobody walks on it, and it stands up from the floor as the cave's wall.</summary>
    CaveWall,
    /// <summary>The dark inside the mouth of a cave, as the open country shows it: a hollow at the foot of the rock.</summary>
    CaveMouth,

    // Forests (plan 01 · M6)
    /// <summary>The dark under the trees where a forest is entered (Eterna Forest, Floaroma Meadow).</summary>
    ForestMouth,

    // The east (plan 01 · M7)
    /// <summary>A puddle: shallow water walked through, which mirrors whoever stands in it (Route 212).</summary>
    Puddle,

    // The Distortion World (plan 01 · M8)
    /// <summary>Nothing at all: the drop the Distortion World's islands float over. No ground is drawn there and nobody walks into it.</summary>
    Void,
    /// <summary>The grey-violet stone of the Distortion World's islands.</summary>
    DistortionGround,
    /// <summary>
    /// A pale slab set into an island, where the Distortion World carries the player somewhere else: a slab that
    /// rises or sinks to another floor, a stone that ferries across the drop, the foot of a wall walked sideways.
    /// </summary>
    DistortionSlab,
    /// <summary>
    /// Kelp on the sea floor (plan 21 · H5; style guide, "The sea floor"): the floor's sand with fronds standing in it
    /// that sway like tall grass, where the GBA's seaweed is and wild Pokémon live.
    /// </summary>
    Kelp
}

/// <summary>What a map of the imported world is where nothing else is said (plan 01 · M5).</summary>
public enum MapSetting
{
    /// <summary>Open country under the sky: forest where no chunk says otherwise, lit by the time of day.</summary>
    Outdoors,
    /// <summary>The inside of a cave: rock where no chunk says otherwise, walls standing up from the floor, a light of its own.</summary>
    Cave,
    /// <summary>
    /// The Distortion World (plan 01 · M8): islands floating over nothing. Where nothing stands there is no ground at
    /// all, only the drop beneath the islands, which nobody walks into; the islands have a light of their own.
    /// </summary>
    Void,
    /// <summary>
    /// A room built from a layout of the world files (plan 18 · W7: a house of a GBA game, which keeps its rooms as
    /// maps of their own and not as models): wall where no chunk says otherwise and wherever no way in reaches, the
    /// room's open tiles floor where the import couldn't name their ground. Sinnoh's rooms are map files instead.
    /// </summary>
    Indoors,
    /// <summary>
    /// The sea floor under Hoenn's eastern routes (plan 21 · H5): reached by Dive from the tile of deep water above it,
    /// with the same coordinates. Rock where no chunk says otherwise, sand where the import couldn't name the floor,
    /// kelp on the seaweed; a light of its own whatever the hour (<c>ArtLook.UnderwaterFieldRig</c>), no weather.
    /// </summary>
    Underwater
}

/// <summary>
/// The field's cameras, as Platinum's map headers name them (<c>sCameraTypes</c> in the original's
/// <c>src/overlay005/field_camera.c</c>): how far away, how steeply and how widely the field is looked at.
/// </summary>
public enum FieldCamera
{
    /// <summary>666.9 units away, pitched 59.05°, 16.18° of view: the overworld.</summary>
    Default,
    /// <summary>574.6 units away, pitched 63.26°, 19.0° of view: caves.</summary>
    Cave,
    /// <summary>515.5 units away, pitched 54.66°, 20.92° of view: Floaroma Meadow, Eterna Forest, Amity Square.</summary>
    ZoomedIn,
    /// <summary>866.6 units away, pitched 73.11°, 12.67° of view: the outside of Mt. Coronet's south face.</summary>
    CoronetSouth,
    /// <summary>662.9 units away, pitched 70.47°, 19.7° of view: the great room inside Stark Mountain, seen from high over it.</summary>
    StarkMountain,
    /// <summary>
    /// 760 units away, pitched 48°, 17° of view: the Driftveil Drawbridge (plan 22 · U6, part b; Black 2's camera 9),
    /// lower and further off than the overworld's, so the sea under the deck shows beside it.
    /// </summary>
    Drawbridge
}

/// <summary>
/// What the air of a place does to its light, beyond the hour and the weather (plan 01 · M10, part b; style guide,
/// "Climates"): a volcano's ash and heat, the cold of a temple of ice. Named by an area's overlay.
/// </summary>
public enum AreaClimate
{
    Temperate,
    /// <summary>Stark Mountain: a sun dimmed and reddened by ash outdoors, rock lit from below by its own heat inside.</summary>
    Volcanic,
    /// <summary>Snowpoint Temple: stone and ice under a cold, blue light.</summary>
    Frozen
}

/// <summary>Which kind of tree fills a map's forests: Sinnoh's layered pines or round broadleaf trees.</summary>
public enum TreeStyle
{
    Round,
    Pine
}

/// <summary>
/// The stage a battle is fought on (plan 04 · G8). Outdoors the ground under the player picks it (water, sand,
/// snow, a cave floor); a map can name its own (a forest, a gym, the League's rooms).
/// </summary>
public enum BattleArena
{
    Grass,
    Forest,
    Cave,
    Water,
    Snow,
    Sand,
    Indoors,
    /// <summary>A gym hall, themed on its leader's type.</summary>
    Gym,
    /// <summary>A room of the Pokémon League: an Elite Four member's type, or the Champion's room.</summary>
    League,
    /// <summary>The sea floor (plan 21 · H5): sand and kelp under the water, a light of its own.</summary>
    Underwater,
    /// <summary>
    /// A city's street (plan 22, wave 48): flagstones under a row of buildings, for a battle begun on paving where the
    /// area's header names a city's background.
    /// </summary>
    City
}

/// <summary>How a town's houses are built (style guide, "Buildings"); public buildings look the same everywhere.</summary>
public enum Architecture
{
    /// <summary>Plank walls with timber posts (Twinleaf).</summary>
    Timber,
    /// <summary>Plaster over a stone base (Sandgem).</summary>
    Plaster,
    /// <summary>Brick and panel blocks with flat roofs (Jubilife).</summary>
    City,
    /// <summary>White clapboard (Pallet).</summary>
    Clapboard,

    // The rest of Sinnoh (plan 01 · M4): each follows the models of its town
    /// <summary>Brick under rust-brown roofs: a mining town (Oreburgh).</summary>
    Brick,
    /// <summary>White boards, rose roofs and flowers at every window (Floaroma).</summary>
    Cottage,
    /// <summary>Plaster between dark beams under steep moss-green roofs: old towns (Eterna, Celestic).</summary>
    HalfTimber,
    /// <summary>Warm plaster with shutters under hipped plum roofs (Hearthome).</summary>
    Townhouse,
    /// <summary>Dark boards under straw-coloured roofs: farmhouses (Solaceon).</summary>
    Farm,
    /// <summary>Cut stone under grey roofs (Veilstone).</summary>
    Stone,
    /// <summary>Boards under hipped reed-green roofs (Pastoria).</summary>
    Marsh,
    /// <summary>Brick under slate-blue roofs: a port (Canalave).</summary>
    Harbour,
    /// <summary>Logs under roofs deep in snow (Snowpoint).</summary>
    Snow,
    /// <summary>White walls under flat or orange roofs with solar panels (Sunyshore).</summary>
    Seaside,
    /// <summary>White boards under hipped turquoise roofs: the lakeside hotel and the Battle Zone's villas.</summary>
    Resort,

    // Kanto (plan 19): each town's own way of building, a row of the style guide's "Buildings" first
    /// <summary>Logs under evergreen gable roofs with shutters and a chimney: the town at the forest's edge (Viridian, plan 19 · K2).</summary>
    Evergreen,
    /// <summary>Pale cut stone under hipped slate-brown roofs with a chimney: the stone-grey city under the mountain (Pewter, plan 19 · K3).</summary>
    Granite,
    /// <summary>White clapboard under cerulean-blue gable roofs with flower boxes: the city of the river and the bridge (Cerulean, plan 19 · K4).</summary>
    Riverside,
    /// <summary>Stucco under vermilion gable roofs with a chimney: the port of glowing sunsets (Vermilion, plan 19 · K5).</summary>
    Portside,
    /// <summary>Plaster under hipped lilac roofs with shutters: the quiet town of the tower (Lavender, plan 19 · K5).</summary>
    Lilac,
    /// <summary>Boards under fuchsia gable roofs with flower boxes: the warm town of the Safari Zone and the ninja's Gym (Fuchsia, plan 19 · K6).</summary>
    Wildwood,

    // Johto (plan 20): each town's own way of building, a row of the style guide's "Buildings" first
    /// <summary>White plaster under slate-blue gable roofs with a chimney: the windy town by the sea (New Bark, plan 20 · J2).</summary>
    Breeze,
    /// <summary>Warm boards under terracotta hip roofs with shutters: the town where the sea meets the cherry trees (Cherrygrove, plan 20 · J2).</summary>
    Bayside,
    /// <summary>Dark boards under hipped tile roofs of slate: the old towns of temples and lanterns (Violet, plan 20 · J2's first rows).</summary>
    OldTimber,
    /// <summary>Logs under dark-brown gable roofs with a chimney: the old town of charcoal smoke and Slowpoke at the forest's edge (Azalea, plan 20 · J5).</summary>
    Woodland,
    /// <summary>Brick under gold hipped roofs with shutters: the great city of the Radio Tower and the Department Store (Goldenrod, plan 20 · J5, part b).</summary>
    Boulevard,
    /// <summary>Dark planks under deep red-brown hipped roofs of tile: the old city of the two towers and the dancers (Ecruteak, plan 20 · J5, part b).</summary>
    Heritage,
    /// <summary>White plaster under red gable roofs with shutters: the port of the lighthouse and the ship (Olivine, plan 20 · J8).</summary>
    Seaport,
    /// <summary>Pale stone under sea-green gable roofs with a chimney: the island town of the pharmacy and the fighters (Cianwood, plan 20 · J8).</summary>
    Cliffside,
    /// <summary>Dark logs under moss-green hipped roofs with a chimney: the small mountain town of the souvenir shop and the ice Gym (Mahogany, plan 20 · J10).</summary>
    Upland,
    /// <summary>Grey cut stone under dragon-blue gable roofs with shutters and a chimney: the walled city of the dragon tamers (Blackthorn, plan 20 · J10).</summary>
    Dragonstone,

    // Hoenn (plan 21): each town's own way of building, a row of the style guide's "Buildings" first
    /// <summary>Boards under leaf-green gable roofs with a chimney and flower boxes: the little town where the grass starts (Littleroot, plan 21 · H1).</summary>
    Sapling,
    /// <summary>White clapboard under sky-blue hip roofs with shutters: the crossroads town between the routes (Oldale, plan 21 · H1).</summary>
    Meadow,
    /// <summary>Plaster under ochre hip roofs with a chimney: the quiet city of the Gym by the pond (Petalburg, plan 21 · H1).</summary>
    Parkside,
    /// <summary>Pale cut stone under brick-red gable roofs with a chimney: the stone city of Devon and the school (Rustboro, plan 21 · H2, part b).</summary>
    Quarry,
    /// <summary>Weathered boards under straw hip roofs with shutters: the fishermen's island of sand (Dewford, plan 21 · H3).</summary>
    Islet,
    /// <summary>Stucco under sea-teal gable roofs with shutters and flower boxes: the busy port of the market and the museum (Slateport, plan 21 · H3).</summary>
    Wharf,

    // Kanto again (plan 19 · K6, part b), at the enum's end so no other value moves
    /// <summary>Plaster under celadon-green hip roofs with flower boxes: the rainbow city of the department store (Celadon, plan 19 · K6, part b).</summary>
    Jade,
    /// <summary>Brick under saffron-gold gable roofs with shutters: the great city at Kanto's heart, home of Silph Co. (Saffron, plan 19 · K6, part b).</summary>
    Uptown,
    /// <summary>Pale cut stone under cinnabar-red hip roofs with a chimney: the volcanic island of the lab and the burned mansion (Cinnabar, plan 19 · K6, part b).</summary>
    Cinder,
    /// <summary>Stucco under electric-yellow hip roofs with shutters: the city of bright lights and electric power (Mauville, plan 21 · H3, part b).</summary>
    Dynamo,
    /// <summary>Plaster under moss-green gable roofs with a chimney and flower boxes: the quiet town of clean air (Verdanturf, plan 21 · H3, part b).</summary>
    Glade,
    /// <summary>Logs under rust-red gable roofs with a chimney and shutters: the farming town under the falling ash (Fallarbor, plan 21 · H4, part a).</summary>
    Hayloft,
    /// <summary>White stucco under sea-turquoise hip roofs with shutters: the warm port of the first Sevii Island (One Island, plan 19 · K7).</summary>
    Isle,
    /// <summary>White clapboard under ember-orange gable roofs with a chimney and shutters: the hot-spring town on the volcano's slope (Lavaridge, plan 21 · H4, part b).</summary>
    Spa,
    /// <summary>Weathered boards under leaf-green gable roofs with shutters: the city of houses built in the treetops (Fortree, plan 21 · H6).</summary>
    Treetop,
    /// <summary>White stucco under sky-blue hip roofs with shutters and flower boxes: the flowering island city of the Space Center (Mossdeep, plan 21 · H8).</summary>
    Starward,
    /// <summary>Pale stone under deep sea-blue hip roofs with shutters: the white city cut into an old crater's terraces round its harbour (Sootopolis, plan 21 · H8, part b).</summary>
    Crater,
    /// <summary>Logs under straw-gold gable roofs with shutters: the town of houses on rafts of logs moored out at sea (Pacifidlog, plan 21 · H8, part c).</summary>
    Raft,
    /// <summary>Pale stone under rose hip roofs with flower boxes: the city of flowers on the terraces at the League's foot (Ever Grande, plan 21 · H9).</summary>
    Bloomstone,

    // Unova (plan 22): each town's own way of building, a row of the style guide's "Buildings" first
    /// <summary>White stucco under slate-teal gable roofs with shutters: the town on the hill over the outlook (Aspertia, plan 22 · U5).</summary>
    Hillside,
    /// <summary>Boards under rust-orange gable roofs with a chimney and flower boxes: the quiet town under its bell tower (Floccesy, plan 22 · U5).</summary>
    Bellfield,
    /// <summary>Dark brick under violet-grey gable roofs with a chimney: the harbour and works town of the complex and the club (Virbank, plan 22 · U5, part b).</summary>
    Dockyard,
    /// <summary>Glass and pale stone in towers of stages stepping in, under flat roofs: the city of skyscrapers on the bay (Castelia, plan 22 · U5, part c).</summary>
    Metropolis,
    /// <summary>White clapboard under cherry-red gable roofs with shutters: the city of the amusement park, the stadiums and the Gear Station (Nimbasa, plan 22 · U6).</summary>
    Funfair,
    /// <summary>Red brick under slate-blue gable roofs with a chimney: the port of warehouses, the market and the Cold Storage (Driftveil, plan 22 · U6, part b).</summary>
    Storehouse
}

/// <summary>Visual theme for indoor maps; <see cref="None"/> means the map is outdoors.</summary>
public enum InteriorStyle
{
    None,
    House,
    PokemonCenter,
    PokeMart,
    Lab,
    /// <summary>
    /// A Gym rebuilt to the original's plan (plan 01 · M9): its floor is the ground its tiles say (a lawn, flowers,
    /// paths, earth, a hall's own tiles) and its walls are in its leader's colours (<see cref="Map.ArenaType"/>).
    /// </summary>
    Gym,
    /// <summary>A museum's hall of pale stone, its exhibits under glass (plan 06 · R16b; style guide, "Fossils, the Game Corner, the Villa and Pal Park").</summary>
    Museum,
    /// <summary>The Game Corner: a red carpet, midnight walls and a band of gold bulbs (plan 06 · R16b).</summary>
    GameHall,
    /// <summary>
    /// A covered street of shops (plan 22, wave 48: Join Avenue): its floor the ground its tiles say (the street's
    /// paving) as a Gym's is, its walls and the blocks of its shops shopfronts (style guide, "Rooms").
    /// </summary>
    Street,
}

/// <summary>
/// The look of a hall that isn't a Gym Leader's (plan 01 · M9, part 2d): a room of the League that has no type of
/// its own. A room of <see cref="InteriorStyle.Gym"/> with one takes its floor tiles, its walls and the face its
/// floors stand on from it, not from <see cref="Map.ArenaType"/> (style guide, "The League's rooms").
/// </summary>
public enum HallStyle
{
    /// <summary>The League's own stone: the passages between the rooms of the Elite Four, with their lifts.</summary>
    League,
    /// <summary>The Champion's room: white and gold.</summary>
    Champion,
    /// <summary>The Hall of Fame and the hallway to it: dark polished stone that mirrors whoever walks on it.</summary>
    HallOfFame
}

public class Warp
{
    private int sourceX, sourceY;
    private static int moves;

    /// <summary>
    /// How often any warp has been put on a tile or moved to another: with a map's list of warps' own count of
    /// changes, what tells the map that its warps by tile are out of date (plan 01 · M12).
    /// </summary>
    public static int Moves => System.Threading.Volatile.Read(ref moves);

    public int SourceX
    {
        get => sourceX;
        set
        {
            sourceX = value;
            System.Threading.Interlocked.Increment(ref moves);
        }
    }

    public int SourceY
    {
        get => sourceY;
        set
        {
            sourceY = value;
            System.Threading.Interlocked.Increment(ref moves);
        }
    }
    public string TargetMap { get; set; } = string.Empty;
    public int TargetX { get; set; }
    public int TargetY { get; set; }
    public Direction TargetFacing { get; set; } = Direction.Down;

    /// <summary>
    /// A way onto the Cycling Road (plan 02 · S2): only a rider is let through, and comes out unable to get off
    /// the Bicycle until the next warp.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public bool CyclistsOnly { get; set; }

    /// <summary>
    /// A door locked until the story sets this flag (plan 02 · S6: the Valley Windworks' door, opened with the Works
    /// Key): its tile stands in the way until then, and can be read. Null for a door that is always open.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? OpenedBy { get; set; }

    /// <summary>
    /// A way that is there only until the story sets this flag (plan 02 · S10): the original moves a warp off its
    /// tiles by script where two of them share the tiles (the way into a lake that is one map before Team Galactic
    /// comes and another after). Null for a way that is always there.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? Until { get; set; }

    /// <summary>A way that is there only once the story has set this flag: the other of two that share their tiles.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? From { get; set; }

    /// <summary>
    /// Whether the story has this way shut for now (<see cref="Until"/>, <see cref="From"/>; kept up to date by
    /// <see cref="Map.ApplyDoors"/>): nobody is taken through it, and the other way on its tile is the one taken.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Shut { get; set; }
}

/// <summary>
/// An item nobody can see, found by looking at its tile (plan 02): the item, how many, and the story flag set once
/// it has been found, after which nothing is there. <see cref="Range"/> is from how many tiles away the original's
/// Dowsing Machine notices it.
/// </summary>
public sealed record HiddenItem(string Item, int Count, string Flag, int Range = 0);

/// <summary>
/// Tiles that start a script when the player steps onto one of them (plan 02 · S1): a rectangle, the script, and
/// the state of the story it waits for. As in the original, a trigger goes by a variable: it fires only while the
/// variable has the value, and the script it starts moves the variable on so that it doesn't fire again.
/// </summary>
public sealed class StepTrigger
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; } = 1;
    public int Depth { get; set; } = 1;

    /// <summary>The script's name, looked for in <see cref="ScriptFile"/> first and among the common ones second.</summary>
    public string Script { get; set; } = string.Empty;

    /// <summary>The file of the place the trigger belongs to; null on a hand-made map, whose name it is.</summary>
    public string? ScriptFile { get; set; }

    /// <summary>The variable it goes by; null for a trigger that fires every time.</summary>
    public string? Variable { get; set; }
    public int Value { get; set; }

    public bool Covers(int x, int y) => x >= X && x < X + Width && y >= Y && y < Y + Depth;
}

/// <summary>
/// A row of a place's table of wild Pokémon: a species, its levels and how often it comes up. What a step has met
/// (<see cref="Map.RollWildEncounter"/>) is a row of its own with the level decided, and the gender and nature
/// the lead's ability chose for it, if it chose any (<see cref="WildEncounterRules"/>).
/// </summary>
public class WildEncounterEntry
{
    public string SpeciesName { get; set; } = "Bidoof";
    public int MinLevel { get; set; } = 2;
    public int MaxLevel { get; set; } = 4;
    public int Weight { get; set; } = 10;

    /// <summary>
    /// The species the slot gives by day and by night where it isn't <see cref="SpeciesName"/>, the morning's (plan 20 ·
    /// J2: HeartGold's grass); null for a slot the same all day. <c>EncounterSlots.Grass</c> puts the time's in.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? DaySpecies { get; set; }
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string? NightSpecies { get; set; }

    /// <summary>The gender Cute Charm chose for the Pokémon met; never in a table.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Gender? Gender { get; set; }

    /// <summary>The nature Synchronize chose for the Pokémon met; never in a table.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Nature? Nature { get; set; }

    /// <summary>The Pokémon met is shiny: a Poké Radar patch's sparkle (plan 06 · R13); never in a table.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Shiny { get; set; }

    /// <summary>The Pokémon met is a roaming one, in this slot of the roamers (plan 06 · R13, <see cref="Roamers"/>); never in a table.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public int? Roamer { get; set; }
}
