using System.Collections.Generic;

namespace PokemonPlatinumEngine.Overworld;

/// <summary>
/// What a tile of the imported world does when something stands on it or walks into it: the low byte of
/// Platinum's tile attributes, with the same values, so imported chunks need no translation. Only the values
/// Sinnoh's maps use have names; <c>docs/tile-behaviours.md</c> lists each with where it occurs. Whether a tile
/// blocks is a separate flag: most blocked tiles are <see cref="None"/>.
/// </summary>
public enum TileBehavior : byte
{
    None = 0x00,
    TallGrass = 0x02,
    VeryTallGrass = 0x03,
    CaveFloor = 0x08,
    OldChateauFloor = 0x0B,
    MountainFloor = 0x0C,
    River = 0x10,
    Waterfall = 0x13,
    Sea = 0x15,
    Puddle = 0x16,
    ShallowWater = 0x17,
    StillPuddle = 0x1D,
    Ice = 0x20,
    Sand = 0x21,
    ShinyFloor = 0x2C,
    NoExplorerKit = 0x2D,
    BlockEast = 0x30,
    BlockWest = 0x31,
    LedgeEast = 0x38,
    LedgeWest = 0x39,
    LedgeSouth = 0x3B,
    Unknown3C = 0x3C,
    Unknown3D = 0x3D,
    LedgeCornerSouthEast = 0x3E,
    LedgeCornerSouthWest = 0x3F,
    SlideEast = 0x40,
    SlideWest = 0x41,
    SlideNorth = 0x42,
    SlideSouth = 0x43,
    BlockNorthAndSouth = 0x49,
    BlockEastAndWest = 0x4A,
    RockClimbNorthSouth = 0x4B,
    RockClimbEastWest = 0x4C,
    PastoriaGymHigh = 0x56,
    PastoriaGymMiddle = 0x57,
    PastoriaGymLow = 0x58,
    MovingFloor = 0x59,
    LongLedgeNorth = 0x5A,
    LongLedgeSouth = 0x5B,
    LongLedgeWest = 0x5C,
    LongLedgeEast = 0x5D,
    StairsEast = 0x5E,
    StairsWest = 0x5F,
    Unknown60 = 0x60,
    EntranceEast = 0x62,
    EntranceWest = 0x63,
    EntranceNorth = 0x64,
    EntranceSouth = 0x65,
    WarpPanel = 0x67,
    Door = 0x69,
    EscalatorFacingBack = 0x6A,
    Escalator = 0x6B,
    ExitEast = 0x6C,
    ExitWest = 0x6D,
    ExitNorth = 0x6E,
    ExitSouth = 0x6F,
    BridgeEnd = 0x70,
    Bridge = 0x71,
    BridgeOverCave = 0x72,
    BridgeOverWater = 0x73,
    BridgeOverSnow = 0x75,
    BikeBridgeNorthSouth = 0x76,
    BikeBridgeNorthSouthOverSand = 0x79,
    BikeBridgeEastWest = 0x7A,
    BikeBridgeEastWestOverGrass = 0x7B,
    BikeBridgeEastWestOverWater = 0x7C,
    BikeBridgeEastWestOverSand = 0x7D,
    Counter = 0x80,
    Computer = 0x83,
    TownMap = 0x85,
    Television = 0x86,
    Unknown88 = 0x88,
    Unknown8E = 0x8E,
    Unknown8F = 0x8F,
    BerrySoil = 0xA0,
    DeepSnow = 0xA1,
    DeeperSnow = 0xA2,
    DeepestSnow = 0xA3,
    Mud = 0xA4,
    DeepMud = 0xA5,
    MarshGrass = 0xA6,
    DeepMarshGrass = 0xA7,
    ShallowSnow = 0xA8,
    ShadedSnow = 0xA9,
    BikeRampEast = 0xD7,
    BikeRampWest = 0xD8,
    BikeSlopeTop = 0xD9,
    BikeSlopeBottom = 0xDA,
    BikeRack = 0xDB,
    SmallBookshelf = 0xE0,
    Bookshelf = 0xE1,
    TallBookshelf = 0xE2,
    TrashCan = 0xE4,
    ShopShelf = 0xE5
}

/// <summary>What each <see cref="TileBehavior"/> means, in a sentence, and the two facts the original keeps in a
/// table of its own: where wild Pokémon appear and what can be surfed on.</summary>
public static class TileBehaviors
{
    private static readonly Dictionary<TileBehavior, string> Meanings = new()
    {
        [TileBehavior.None] = "Plain ground or a plain obstacle; only the blocked flag matters.",
        [TileBehavior.TallGrass] = "Tall grass.",
        [TileBehavior.VeryTallGrass] = "Grass taller than the walker; too thick to cycle through.",
        [TileBehavior.CaveFloor] = "The floor of a cave.",
        [TileBehavior.OldChateauFloor] = "The floors of the Old Chateau, the only building with wild Pokémon in its rooms.",
        [TileBehavior.MountainFloor] = "A rough floor without wild Pokémon of its own: the Underground's tunnels and parts of Eterna Forest and Victory Road.",
        [TileBehavior.River] = "Fresh water.",
        [TileBehavior.Waterfall] = "A waterfall: climbed with Waterfall; a surfer who enters from above is carried down.",
        [TileBehavior.Sea] = "The sea, and the still water of lakes and ponds.",
        [TileBehavior.Puddle] = "A puddle: walked through with a splash, and it mirrors the walker.",
        [TileBehavior.ShallowWater] = "Water ankle deep: walked through, leaving ripples.",
        [TileBehavior.StillPuddle] = "A puddle that mirrors the walker without splashing.",
        [TileBehavior.Ice] = "Ice: a walker slides on until something stops them.",
        [TileBehavior.Sand] = "Sand, which keeps footprints.",
        [TileBehavior.ShinyFloor] = "A polished floor that mirrors the walker.",
        [TileBehavior.NoExplorerKit] = "Ground where the Explorer Kit can't be used.",
        [TileBehavior.BlockEast] = "Open ground whose east side is closed: no stepping across that edge.",
        [TileBehavior.BlockWest] = "Open ground whose west side is closed.",
        [TileBehavior.LedgeEast] = "A ledge hopped over eastward; a wall from every other side.",
        [TileBehavior.LedgeWest] = "A ledge hopped over westward.",
        [TileBehavior.LedgeSouth] = "A ledge hopped over southward, the common kind.",
        [TileBehavior.Unknown3C] = "Not understood yet; by its place in the list, a ledge's north-east corner.",
        [TileBehavior.Unknown3D] = "Not understood yet; by its place in the list, a ledge's north-west corner.",
        [TileBehavior.LedgeCornerSouthEast] = "The corner where a ledge hopped eastward ends at its south: a wall from every side.",
        [TileBehavior.LedgeCornerSouthWest] = "The corner where a ledge hopped westward ends at its south: a wall from every side.",
        [TileBehavior.SlideEast] = "A floor that carries the walker east.",
        [TileBehavior.SlideWest] = "A floor that carries the walker west.",
        [TileBehavior.SlideNorth] = "A floor that carries the walker north.",
        [TileBehavior.SlideSouth] = "A floor that carries the walker south.",
        [TileBehavior.BlockNorthAndSouth] = "A walkway closed on its north and south sides.",
        [TileBehavior.BlockEastAndWest] = "A walkway closed on its east and west sides.",
        [TileBehavior.RockClimbNorthSouth] = "A rock face climbed north or south with Rock Climb.",
        [TileBehavior.RockClimbEastWest] = "A rock face climbed east or west with Rock Climb.",
        [TileBehavior.PastoriaGymHigh] = "Pastoria Gym: a floor stepped onto only by someone standing at the bottom of the pool.",
        [TileBehavior.PastoriaGymMiddle] = "Pastoria Gym: a floor stepped onto only by someone standing two tiles up, the walkways' height.",
        [TileBehavior.PastoriaGymLow] = "Pastoria Gym: a floor stepped onto only by someone standing four tiles up, the decks' height.",
        [TileBehavior.MovingFloor] = "Ground that can't be stood on from a Gym's moving water (the pool round the Pastoria Gym's floats); plain ground anywhere else.",
        [TileBehavior.LongLedgeNorth] = "A gap in the Distortion World, jumped northward to the third tile on.",
        [TileBehavior.LongLedgeSouth] = "A gap in the Distortion World, jumped southward to the third tile on.",
        [TileBehavior.LongLedgeWest] = "A gap in the Distortion World, jumped westward to the third tile on.",
        [TileBehavior.LongLedgeEast] = "A gap in the Distortion World, jumped eastward to the third tile on.",
        [TileBehavior.StairsEast] = "Stairs at the side of a room: walking east onto them takes the warp there.",
        [TileBehavior.StairsWest] = "Stairs at the side of a room, taken walking west.",
        [TileBehavior.Unknown60] = "Not understood yet.",
        [TileBehavior.EntranceEast] = "An opening (a cave mouth, a gate) entered walking east: it takes the warp there.",
        [TileBehavior.EntranceWest] = "An opening entered walking west.",
        [TileBehavior.EntranceNorth] = "An opening entered walking north.",
        [TileBehavior.EntranceSouth] = "An opening entered walking south.",
        [TileBehavior.WarpPanel] = "A panel that warps whoever steps on it.",
        [TileBehavior.Door] = "A door: blocked, but walking into it opens it and takes the warp there.",
        [TileBehavior.EscalatorFacingBack] = "An escalator that turns its rider round.",
        [TileBehavior.Escalator] = "An escalator.",
        [TileBehavior.ExitEast] = "The mat at a room's east edge: walking east off it takes the warp there.",
        [TileBehavior.ExitWest] = "The mat at a room's west edge.",
        [TileBehavior.ExitNorth] = "The mat at a room's north edge.",
        [TileBehavior.ExitSouth] = "The mat at a room's south edge, where most rooms are left.",
        [TileBehavior.BridgeEnd] = "The end of a bridge, where a walker steps onto its deck.",
        [TileBehavior.Bridge] = "The deck of a bridge: the walker is on the upper level.",
        [TileBehavior.BridgeOverCave] = "A bridge inside a cave.",
        [TileBehavior.BridgeOverWater] = "A bridge with water under it that can be surfed.",
        [TileBehavior.BridgeOverSnow] = "A bridge over snow.",
        [TileBehavior.BikeBridgeNorthSouth] = "A plank running north to south, crossed only on the Bicycle.",
        [TileBehavior.BikeBridgeNorthSouthOverSand] = "A bicycle plank running north to south over sand.",
        [TileBehavior.BikeBridgeEastWest] = "A plank running east to west, crossed only on the Bicycle.",
        [TileBehavior.BikeBridgeEastWestOverGrass] = "A bicycle plank running east to west over tall grass.",
        [TileBehavior.BikeBridgeEastWestOverWater] = "A bicycle plank running east to west over water.",
        [TileBehavior.BikeBridgeEastWestOverSand] = "A bicycle plank running east to west over sand.",
        [TileBehavior.Counter] = "A table or a counter: talking across it reaches whoever stands behind.",
        [TileBehavior.Computer] = "A PC.",
        [TileBehavior.TownMap] = "A map of the region on a wall.",
        [TileBehavior.Television] = "A television.",
        [TileBehavior.Unknown88] = "Not understood yet.",
        [TileBehavior.Unknown8E] = "Not understood yet.",
        [TileBehavior.Unknown8F] = "Not understood yet.",
        [TileBehavior.BerrySoil] = "Soft soil where a Berry can be planted.",
        [TileBehavior.DeepSnow] = "Snow deep enough to slow a walker.",
        [TileBehavior.DeeperSnow] = "Deeper snow, slower still.",
        [TileBehavior.DeepestSnow] = "The deepest snow: a walker wades.",
        [TileBehavior.Mud] = "Marsh mud.",
        [TileBehavior.DeepMud] = "Deep marsh mud, where a walker can get stuck.",
        [TileBehavior.MarshGrass] = "Marsh mud with grass growing in it.",
        [TileBehavior.DeepMarshGrass] = "Deep marsh mud with grass.",
        [TileBehavior.ShallowSnow] = "A thin cover of snow that keeps footprints.",
        [TileBehavior.ShadedSnow] = "Thin snow in shade.",
        [TileBehavior.BikeRampEast] = "A ramp jumped eastward on a fast Bicycle.",
        [TileBehavior.BikeRampWest] = "A ramp jumped westward on a fast Bicycle.",
        [TileBehavior.BikeSlopeTop] = "The top of a muddy slope that only a fast Bicycle climbs.",
        [TileBehavior.BikeSlopeBottom] = "The foot of a muddy slope.",
        [TileBehavior.BikeRack] = "A bicycle rack.",
        [TileBehavior.SmallBookshelf] = "A low bookshelf.",
        [TileBehavior.Bookshelf] = "A bookshelf.",
        [TileBehavior.TallBookshelf] = "A second kind of bookshelf.",
        [TileBehavior.TrashCan] = "A trash can.",
        [TileBehavior.ShopShelf] = "A shelf of goods in a shop."
    };

    /// <summary>True for the values Sinnoh's maps use, which are the ones the enum names.</summary>
    public static bool IsKnown(byte value) => Meanings.ContainsKey((TileBehavior)value);

    public static string Meaning(TileBehavior behavior) => Meanings.TryGetValue(behavior, out var text) ? text : "Not used by any map.";

    /// <summary>Wild Pokémon can appear on a step here (what appears depends on the area and on land or water).</summary>
    public static bool HasEncounters(TileBehavior b) => b is TileBehavior.TallGrass or TileBehavior.VeryTallGrass or TileBehavior.CaveFloor
        or TileBehavior.OldChateauFloor or TileBehavior.River or TileBehavior.Sea or TileBehavior.MarshGrass or TileBehavior.DeepMarshGrass
        or TileBehavior.BridgeOverCave or TileBehavior.BikeBridgeEastWestOverGrass;

    /// <summary>Water a Pokémon can carry its trainer across, including what runs under some bridges.</summary>
    public static bool IsSurfable(TileBehavior b) => b is TileBehavior.River or TileBehavior.Waterfall or TileBehavior.Sea
        or TileBehavior.BridgeOverWater or TileBehavior.BikeBridgeEastWestOverWater;

    /// <summary>Ground that keeps the print of a shoe, as in Platinum: its sand, and snow of every depth.</summary>
    public static bool KeepsFootprints(TileBehavior behaviour) => behaviour is TileBehavior.Sand or TileBehavior.ShallowSnow
        or TileBehavior.ShadedSnow or TileBehavior.DeepSnow or TileBehavior.DeeperSnow or TileBehavior.DeepestSnow;

    /// <summary>
    /// What the game does with a behaviour today (plan 01 · M3), for <c>docs/tile-behaviours.md</c> and for the
    /// test that keeps the list honest: whether <see cref="FieldMovement"/> has a rule for it, whether it is
    /// ground like any other as far as a step goes, or whether it waits for the place that needs it.
    /// </summary>
    public static (BehaviourSupport Support, string Note) InGame(TileBehavior b) => b switch
    {
        TileBehavior.LedgeSouth or TileBehavior.LedgeEast or TileBehavior.LedgeWest
            => (BehaviourSupport.Ruled, "Hopped the way it faces, two tiles on; a wall from every other side."),
        TileBehavior.BlockEast or TileBehavior.BlockWest or TileBehavior.BlockNorthAndSouth or TileBehavior.BlockEastAndWest
            => (BehaviourSupport.Ruled, "No step across its closed side, either way."),
        TileBehavior.RockClimbNorthSouth or TileBehavior.RockClimbEastWest
            => (BehaviourSupport.Ruled, "Climbed to its far end by a party that knows Rock Climb."),
        TileBehavior.Waterfall => (BehaviourSupport.Ruled, "Surfed up with Waterfall and down without; never sideways."),
        TileBehavior.Ice => (BehaviourSupport.Ruled, "Whoever steps on it slides on until something stops them."),
        TileBehavior.SlideEast or TileBehavior.SlideWest or TileBehavior.SlideNorth or TileBehavior.SlideSouth
            => (BehaviourSupport.Ruled, "Carries whoever steps on it along."),
        TileBehavior.River or TileBehavior.Sea
            => (BehaviourSupport.Ruled, "Stops a walker; surfed with Surf, meeting the area's water Pokémon."),
        TileBehavior.BridgeOverWater or TileBehavior.BikeBridgeEastWestOverWater
            => (BehaviourSupport.Ruled, "Crossed on its deck and surfed under, whichever level one is on."),
        TileBehavior.VeryTallGrass => (BehaviourSupport.Ruled, "Wild Pokémon, more often than in tall grass; no Bicycles."),
        TileBehavior.ShallowSnow => (BehaviourSupport.Ruled, "No Bicycles. Every step leaves a footprint."),
        TileBehavior.DeepSnow => (BehaviourSupport.Ruled, "No running, no Bicycles; a walker sinks to the ankle and leaves footprints."),
        TileBehavior.DeeperSnow => (BehaviourSupport.Ruled, "Half a walk's pace, no Bicycles; a walker sinks to the shin and leaves footprints."),
        TileBehavior.DeepestSnow => (BehaviourSupport.Ruled, "A quarter of a walk's pace, no Bicycles; a walker sinks to the knee and leaves footprints."),
        TileBehavior.Mud or TileBehavior.MarshGrass
            => (BehaviourSupport.Ruled, "No running, no Bicycles. (Platinum's sinking in the marsh is not built.)"),
        TileBehavior.DeepMud or TileBehavior.DeepMarshGrass
            => (BehaviourSupport.Ruled, "Half a walk's pace, no Bicycles. (Getting stuck, as in Platinum, is not built.)"),
        TileBehavior.BikeSlopeTop or TileBehavior.BikeSlopeBottom
            => (BehaviourSupport.Ruled, "Climbed only on a Bicycle in its fast gear. (The slide back down is not played.)"),
        TileBehavior.BikeBridgeNorthSouth or TileBehavior.BikeBridgeNorthSouthOverSand or TileBehavior.BikeBridgeEastWest
            or TileBehavior.BikeBridgeEastWestOverGrass or TileBehavior.BikeBridgeEastWestOverSand
            => (BehaviourSupport.Ruled, "Ridden along on a Bicycle, never across, and never walked."),

        TileBehavior.LongLedgeNorth or TileBehavior.LongLedgeSouth or TileBehavior.LongLedgeWest or TileBehavior.LongLedgeEast
            => (BehaviourSupport.Ruled, "A gap in the Distortion World: jumped the way it faces, over it and the tile past it to the third tile on, on foot; the drop it is from every other side (`FieldMovement.LongJumpDirection`)."),
        TileBehavior.BikeRampEast or TileBehavior.BikeRampWest => (BehaviourSupport.Ruled, "On a Bicycle going its way, jumped: three tiles on in top gear, one in low. A wall on foot (`FieldMovement.RampDirection`)."),
        TileBehavior.PastoriaGymHigh or TileBehavior.PastoriaGymMiddle or TileBehavior.PastoriaGymLow
            => (BehaviourSupport.Ruled, "Stepped onto only from its own height: the bottom, two tiles up or four (`PastoriaWater.Collides`, plan 01 · M9 1b)."),
        TileBehavior.MovingFloor
            => (BehaviourSupport.Ruled, "Blocks whoever would stand on the water of a Gym's puzzle there (`Map.StandAt`, `PastoriaWater`, plan 01 · M9 1b); elsewhere walked as plain ground. Canalave's and Sunyshore's own come with their Gyms (M9 part 2)."),
        TileBehavior.StairsEast or TileBehavior.StairsWest or TileBehavior.EntranceEast or TileBehavior.EntranceWest
            or TileBehavior.EntranceNorth or TileBehavior.EntranceSouth or TileBehavior.ExitEast or TileBehavior.ExitWest
            or TileBehavior.ExitNorth or TileBehavior.ExitSouth
            => (BehaviourSupport.Waiting, "Its warp is taken by stepping onto the tile; taking it by walking off the right way comes with the rooms (plan 01 · M11)."),
        TileBehavior.WarpPanel or TileBehavior.EscalatorFacingBack or TileBehavior.Escalator
            => (BehaviourSupport.Waiting, "Comes with the buildings that have them (plan 01 · M11)."),
        TileBehavior.Unknown3C or TileBehavior.Unknown3D or TileBehavior.Unknown60 or TileBehavior.Unknown88 or TileBehavior.Unknown8E
            or TileBehavior.Unknown8F => (BehaviourSupport.Waiting, "Not understood."),

        TileBehavior.TallGrass or TileBehavior.CaveFloor or TileBehavior.OldChateauFloor or TileBehavior.BridgeOverCave
            => (BehaviourSupport.Plain, "Walked on; wild Pokémon from the area's land table."),
        TileBehavior.Puddle => (BehaviourSupport.Plain, "Walked through with a splash: a ring and two drops. A puddle's look, and whoever stands in it is mirrored."),
        TileBehavior.ShallowWater => (BehaviourSupport.Plain, "Walked through, leaving a ring on the water."),
        TileBehavior.StillPuddle => (BehaviourSupport.Plain, "Walked through. A puddle's look, and whoever stands in it is mirrored."),
        TileBehavior.Sand or TileBehavior.ShadedSnow => (BehaviourSupport.Plain, "Walked on; every step leaves a footprint that fades."),
        TileBehavior.Door => (BehaviourSupport.Plain, "A blocked tile until a warp opens it."),
        TileBehavior.LedgeCornerSouthEast or TileBehavior.LedgeCornerSouthWest => (BehaviourSupport.Plain, "A blocked tile, drawn as the end of its ledge."),
        TileBehavior.BerrySoil => (BehaviourSupport.Plain, "Soft soil's look. The patch is the area's soft soil object standing on it, faced to plant, water and pick (`BerryPatches`, plan 06 · R14a)."),
        _ => (BehaviourSupport.Plain, "Ground like any other: only the blocked flag and the height matter.")
    };
}

/// <summary>How far the game's rules for a tile behaviour go.</summary>
public enum BehaviourSupport
{
    /// <summary>Nothing special happens to a step onto or off it.</summary>
    Plain,
    /// <summary><see cref="FieldMovement"/> has a rule for it.</summary>
    Ruled,
    /// <summary>It needs a rule that isn't written yet.</summary>
    Waiting
}
