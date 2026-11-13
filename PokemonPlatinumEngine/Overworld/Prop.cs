namespace PokemonPlatinumEngine.Overworld;

public enum PropType
{
    // Furniture: blocks movement
    Table,
    Chair,
    Sofa,
    Bookshelf,
    Television,
    Plant,
    Fridge,
    KitchenCounter,
    Stove,
    Stairs,
    Counter,
    HealingMachine,
    Bench,
    StoreShelf,
    LabDesk,
    LabMachine,
    /// <summary>A bed two tiles long, its head against the north wall (plan 02 · S4).</summary>
    Bed,
    /// <summary>A desk with a computer on it (the player's own, in the bedroom).</summary>
    Computer,
    /// <summary>
    /// A flight of stairs at the side of a room, two tiles each way, climbing west to the floor above, as the Team
    /// Galactic Eterna Building's do (plan 02 · S6): its warp is the tile east of its foot.
    /// </summary>
    SideStairsUp,
    /// <summary>A stairwell at the side of a room going down eastward to the floor below, behind a rail: its warp is the tile west of its head.</summary>
    SideStairsDown,
    /// <summary>The same flight turned round, climbing east: its warp is the tile west of its foot (the Lost Tower's, plan 02 · S7).</summary>
    SideStairsUpEast,
    /// <summary>The same stairwell turned round, going down westward: its warp is the tile east of its head (the Lost Tower's).</summary>
    SideStairsDownWest,
    /// <summary>A headstone, a tile wide: the Lost Tower's small graves (plan 02 · S7).</summary>
    Headstone,
    /// <summary>A tomb, two tiles each way: the Lost Tower's large graves.</summary>
    Tomb,
    /// <summary>A bicycle on its stand, two tiles wide: what a cycle shop shows (Eterna City's, plan 02 · S6).</summary>
    Bicycle,
    /// <summary>A waste bin.</summary>
    TrashCan,
    /// <summary>A tall wooden wardrobe against the wall: where the player changes clothes at home (plan 11 · C10).</summary>
    Wardrobe,
    /// <summary>A rail of clothes on hangers, as wide as its tiles: what a boutique shows (plan 11 · C10).</summary>
    ClothesRack,
    /// <summary>A table under a checked cloth with a pot on its burner in the middle: where Poffins are cooked (the Poffin House, plan 06 · R14c).</summary>
    CookingTable,

    // Outdoors: a rock too big to step over, on land or standing in water
    Boulder,

    // Obstacles a field move clears: a small tree for Cut, a cracked rock for Rock Smash, a round boulder for
    // Strength. Since plan 02 · S2 they are objects of the map, as the original's are (NPC.Obstacle; Map.AddObstacle),
    // drawn as cards and cleared or pushed by their scripts; the kinds stay here for their art.
    CutTree,
    CrackedRock,
    StrengthBoulder,

    // Street furniture. A fence covers a run of tiles and joins up with the fences beside it; outdoors a Bench is a park bench
    Fence,
    /// <summary>A low wall of stone: what a town of brick and stone has where a village has a fence. It joins up like one.</summary>
    LowWall,
    LampPost,
    Mailbox,
    Planter,

    // What stands in Sinnoh's towns besides buildings, placed by the models of the imported world (Data/WorldModels).
    // Each covers the tiles of its model's box; the world's own data says which of them block the way.
    Fountain,
    /// <summary>A ship at its pier: lying north–south or east–west as its box is longer.</summary>
    Boat,
    WindTurbine,
    Statue,
    /// <summary>A broad-leaved tree standing alone, that wild Pokémon come to when it is slathered with honey.</summary>
    HoneyTree,
    Crates,
    CoalHeap,
    Hedge,
    /// <summary>A raised bed of flowers in a kerb of pale stone (Amity Square).</summary>
    FlowerBed,
    /// <summary>A stone pillar as tall as its model (<see cref="Prop.Height"/>).</summary>
    Column,
    Topiary,
    /// <summary>A small pile of worked stones: the Hallowed Tower, a stone tablet.</summary>
    Cairn,
    Billboard,
    /// <summary>A mass of rock as large as its box.</summary>
    Outcrop,
    /// <summary>A lattice mast as tall as its model.</summary>
    Mast,
    /// <summary>Steel drums standing together: what a mine keeps its oil in.</summary>
    Drums,
    /// <summary>
    /// A raised conveyor belt, a tile wide, running north and south (deeper than wide) or east and west: on a
    /// steel pier wherever the tile under it is blocked, and a span one walks beneath wherever it is open.
    /// </summary>
    Conveyor,
    /// <summary>
    /// What carries a conveyor over the open ground: two steel legs with a beam across (three tiles one way,
    /// the middle one open), or a single pier when it covers one tile.
    /// </summary>
    Gantry,

    // The east and the sea (plan 01 · M7)
    /// <summary>A heap of iron ore (Iron Island).</summary>
    OreHeap,
    /// <summary>The steel tower over a mine's shaft, with its winding wheel, as tall as its model (Iron Island).</summary>
    Headframe,
    /// <summary>The steel platform at the foot of a lift.</summary>
    LiftBase,
    /// <summary>A leaf of a drawbridge over the planks the ground shows: trusses along the deck and a portal at its bank (Canalave City).</summary>
    Drawbridge,
    /// <summary>The Great Marsh's little tram on its rails.</summary>
    Tram,
    /// <summary>A coin viewer on a post.</summary>
    Binoculars,
    /// <summary>An open shelter on four posts under a roof (the Hotel Grand Lake).</summary>
    Pavilion,

    // The north and the end (plan 01 · M8)
    /// <summary>A stack of rock standing in the sea, the water lapping round its foot (Sunyshore City).</summary>
    SeaStack,
    /// <summary>A low drift of snow banked against a wall (Snowpoint City's harbour).</summary>
    Snowdrift,
    /// <summary>A tear in the air taller than a person, edged in light: where Spear Pillar is torn open (plan 02 · S12).</summary>
    Rift,
    /// <summary>The dark a rift casts on the ground below it: the way down into the Distortion World.</summary>
    RiftShadow,

    // The Gyms' puzzles (plan 01 · M9): things of the map, like the obstacles, drawn as cards (GymArt)
    /// <summary>The Veilstone Gym's punching bag, which a kick sends along its track.</summary>
    PunchingBag,
    /// <summary>A stack of tyres in the Veilstone Gym's way, knocked down by a punching bag.</summary>
    TireStack,
    /// <summary>A post in the way out of the Hearthome Gym's Leader's room, gone once she is beaten.</summary>
    Bollard,
    /// <summary>A great ball of packed snow on the Snowpoint Gym's ice, broken by sliding into it fast enough.</summary>
    Snowball,
    /// <summary>A door of the League's rooms (plan 01 · M9, part 2d): a thing of the map, drawn in its wall by the room's pieces.</summary>
    LeagueDoor,

    // The Battle Zone (plan 01 · M10)
    /// <summary>A shipping container of corrugated steel, as large as its model's tiles (the Fight Area's quay).</summary>
    Container,
    /// <summary>A fountain on a stepped plinth standing in a pool: a bowl, a jet and water falling from its rim (before the Battle Frontier's gate).</summary>
    GrandFountain,
    /// <summary>A swimming pool in a kerb of pale tiles, with a ladder, and a parasol over a deckchair beside it (the Resort Area).</summary>
    Pool,
    /// <summary>A pool of lava lying in the rock, its crust drifting over the glow beneath (Stark Mountain's flank; part b).</summary>
    Lava,
    /// <summary>The mouth of a volcano: tiers of dark rock up to a rim, the glow in its throat and the smoke that rises from it (Stark Mountain).</summary>
    Crater,
    /// <summary>A stall with a counter on the side its attendant serves across, under a striped awning (the Battle Frontier's Exchange Service Corner).</summary>
    Booth,
    /// <summary>A model of the Battle Frontier on a table of stone, in the middle of its plaza.</summary>
    ScaleModel,
    /// <summary>A bronze of a trainer holding a Poké Ball high, on a plinth (before the Battle Frontier's gate).</summary>
    TrainerStatue,
    /// <summary>A giant of carved stone, taller than a door (the statue in Snowpoint Temple's hall).</summary>
    Colossus,
    // Amity Square (plan 06 · R16c): a hut of its ruins, walked into by the open tile of its south row, and the desk
    // each receptionist stands behind. The world says which of their tiles block
    RuinHut,
    ReceptionDesk,

    // Decoration: floor rugs and things hung on the back wall, never solid
    Rug,
    Window,
    Painting,
    Clock,
    WallEmblem,

    // Team Galactic's headquarters (plan 02 · S11, part b). Appended here, past the decorations: none of them blocks by
    // itself, the room's own tiles say where one can walk
    /// <summary>A warp panel set into the floor (a tile of <see cref="TileBehavior.WarpPanel"/>): a ring of teal light on a dark plate.</summary>
    WarpPanel,
    /// <summary>The panel that leads out of the building, and the pad it lands on: the same plate with its ring in green.</summary>
    WarpPanelOut,
    /// <summary>A leaf of a locked door of Team Galactic's: a thing of the map (<see cref="NPC.GalacticDoorType"/>), drawn as a card.</summary>
    GalacticDoor,
    /// <summary>A tank of the laboratory, two tiles by two: a glass cylinder of green fluid between a steel foot and cap.</summary>
    Tank,
    /// <summary>A heap of broken stone the story left: a thing of the map (<see cref="NPC.RubbleType"/>), drawn as a card (plan 02 · S12).</summary>
    Rubble,
    /// <summary>The cave painting that stands over a way through Mt. Coronet until the story breaks it: a thing of the map (<see cref="NPC.CavePaintingType"/>), drawn as a card (plan 02 · S12).</summary>
    CavePainting,
    // Fossils, the Game Corner and the Villa (plan 06 · R16b): none blocks by itself, the room's own tiles say where one can walk
    DisplayCase, SlotMachine, Piano, Rack, AudioSystem, Bust, SilverBust, GuestSet, NightTable, MusicBox, TeaSet, PcDesk,

    // The rooms of plan 01 · M11, rebuilt to the original's plans. Appended past the decorations: it never blocks
    /// <summary>
    /// A seat one walks onto, as the original's stools and floor cushions are: a square cushion on the floor. Whoever
    /// sits at a table stands on its tile.
    /// </summary>
    Cushion,
    /// <summary>
    /// A stand of potted flowers on each tile it covers (the Floaroma flower shop). Past the decorations like the
    /// laboratory's tank: the room's own tiles say that it blocks.
    /// </summary>
    FlowerStand,
    /// <summary>A lift's car standing open in its shaft (plan 02 · S13, the Vista Lighthouse): a steel plate on the floor between two jambs, walked onto. It blocks nothing by itself.</summary>
    LiftCar,
    /// <summary>The indicator on the back wall of a lift's car: a dark panel with an arrow up and an arrow down.</summary>
    LiftIndicator,
    /// <summary>
    /// Books piled on the floor (plan 01 · M11, part c: the original's book01 lying on a blocked tile). Past the
    /// decorations like a flower stand: the room's own tiles say that it blocks.
    /// </summary>
    BookPile,
    /// <summary>
    /// A vending machine (plan 01 · M11, part d: the roof of Veilstone's Department Store), read from the tile before
    /// it. Past the decorations: the room's own tiles say that it blocks.
    /// </summary>
    VendingMachine,
    /// <summary>The Magma Stone on its place in Stark Mountain's last room: a thing of the map (<see cref="NPC.MagmaStoneType"/>), drawn as a card (plan 02 · S15, part b).</summary>
    MagmaStone,
    /// <summary>
    /// Shelves of goods hung on a room's back wall (plan 01 · M11, the last rooms: a Poké Mart's east end), on the
    /// wall's own row like a painting. Past the decorations: it blocks nothing, and the floor before it is walked.
    /// </summary>
    WallShelf,
    /// <summary>A blackboard on a room's back wall (plan 01 · M11, the last rooms: the Trainers' School), on the wall's own row like a painting.</summary>
    Blackboard,
    /// <summary>An electric gate's beams between two posts (plan 19 · K4, part b: the Vermilion City Gym's locks): a thing of the map (<see cref="NPC.ElectricGateType"/>), drawn as a card.</summary>
    ElectricGate,
    /// <summary>The Dome Fossil laid on the ground to be taken (plan 19 · K4, part b: Mt. Moon): a thing of the map (<see cref="NPC.DomeFossilType"/>), drawn as a card.</summary>
    DomeFossil,
    /// <summary>The Helix Fossil laid on the ground to be taken (plan 19 · K4, part b: Mt. Moon): a thing of the map (<see cref="NPC.HelixFossilType"/>), drawn as a card.</summary>
    HelixFossil,
    /// <summary>The Old Amber on its stand (plan 19 · K4, part b: Pewter City's museum): a thing of the map (<see cref="NPC.OldAmberType"/>), drawn as a card.</summary>
    OldAmber,
    /// <summary>A Pokédex laid on a desk (plan 19 · K4, part b: Professor Oak's lab): a thing of the map (<see cref="NPC.PokedexType"/>), drawn as a card.</summary>
    DeskPokedex,
    /// <summary>The Town Map rolled up on a table (plan 19 · K4, part b: the rival's house): a thing of the map (<see cref="NPC.TownMapType"/>), drawn as a card.</summary>
    RolledTownMap,
    /// <summary>
    /// A whirlpool on the water (plan 20 · J8: HeartGold's <c>uzushio</c>), three tiles across: a ring of water turning,
    /// drawn lying a hair over the water. It blocks nothing by itself: its tiles' behaviour stops a surfer until the
    /// party knows Whirlpool (<see cref="FieldMovement.Step"/>).
    /// </summary>
    Whirlpool,
    /// <summary>
    /// A block of ice on the Mahogany Gym's floor (plan 20 · J11: HeartGold's <c>ice</c> objects): a thing of the map
    /// (<see cref="NPC.IceBlockType"/>), drawn as a card. Walked or slid into, it slides away over the ice until
    /// something stops it (<see cref="FieldMovement.IceBlockRun"/>), and it is back where it stood the next time the
    /// player comes in.
    /// </summary>
    IceBlock,
    /// <summary>
    /// A stone statue of the Mossdeep Gym's puzzle (plan 21 · H8: Emerald's Trick House statue): a thing of the map
    /// (<see cref="NPC.GymStatueType"/>), drawn as a card, which a floor switch moves a step round its loop of arrows.
    /// </summary>
    GymStatue,
    /// <summary>
    /// A decoration standing in a secret base of Hoenn's (plan 21 · H10): its model names which (<c>dec:45</c>, the
    /// decoration's number), built by <c>DecorationArt</c>. Blocks nothing by itself: the base lays the tiles it stands
    /// in the way of (<c>GameEngine.LayOutSecretBase</c>).
    /// </summary>
    Decoration,
    /// <summary>A small aeroplane on the apron or the runway, nose along its longer side (Mistralton City's, plan 22 · U7).</summary>
    Airplane,
    /// <summary>A cluster of pale charged crystals that glows (Chargestone Cave's, plan 22 · U7).</summary>
    ChargeStone,
    /// <summary>
    /// Rotom's appliances on Silph Co.'s research floor (plan 20 · J15, part n: HeartGold's objects of their own look):
    /// things of the map (<see cref="NPC.MicrowaveType"/> and its four fellows), drawn as cards (<c>ApplianceArt</c>),
    /// in the way like a person and spoken to like one.
    /// </summary>
    Microwave,
    WashingMachine,
    Refrigerator,
    ElectricFan,
    Lawnmower,
    /// <summary>The carved wall over the way through a Ruins of Alph chamber's back wall until its secret is brought: a thing of
    /// the map (<see cref="NPC.UnownWallType"/>), drawn as a card like the cave painting (plan 20 · J5's last leftover).
    /// </summary>
    UnownWall,
    /// <summary>The mouth of a Hidden Grotto in a tree wall (plan 22 · U11): a thing of the map (<see cref="NPC.GrottoHollowType"/>), drawn as a card.</summary>
    GrottoHollow,
    /// <summary>The Cianwood Gym's waterfall (plan 20, part r): a thing of the map (<see cref="NPC.GymWaterfallType"/>), drawn falling by <c>GymPieces</c>.</summary>
    GymWaterfall,
    /// <summary>A pile of cardboard crates in the Goldenrod Department Store's basement (plan 20, part r): a thing of the map (<see cref="NPC.CrateType"/>), drawn as a card.</summary>
    CratePile,
    /// <summary>
    /// Lumiose City's tower of light in the middle of its central plaza (plan 23 · Z2; our own drawing): four stages of
    /// steel lattice, each narrower than the one under it, a band of lit glass round each and a crown lit all night. An overlay's prop,
    /// so it blocks every tile it covers.
    /// </summary>
    PrismTower,
    /// <summary>Birth Island's triangle (plan 21 · H12, part p): a thing of the map (<see cref="NPC.DeoxysTriangleType"/>), drawn as a card (<c>HoennArt</c>).</summary>
    DeoxysTriangle,
    /// <summary>
    /// The Abandoned Ship lying in Route 108's sea (plan 21 · H12, part p): the ferry gone to ruin, its stern sunk and its
    /// hull rusted (<c>HoennArt.Wreck</c>). An overlay's prop over tiles the import blocks already; it blocks nothing by itself.
    /// </summary>
    Wreck,
    /// <summary>
    /// A ship's engines (plan 20 · J15, part s: the S.S. Aqua's hold; our own drawing): a block of machinery a tile at a
    /// time on the tiles the engines' model blocks, a casing of green-grey steel with a row of cylinder heads and a pipe
    /// along its top. A room's furniture: the tile blocks by itself.
    /// </summary>
    ShipEngine,
    /// <summary>
    /// The Abandoned Ship's deckhouse and bulwarks (plan 21 · part r; our own drawing): an overlay's prop over the deck's
    /// blocked tiles, whose shared metatiles the import lays as rock. Built from the deck below it up to the top of what it
    /// covers: a white-painted steel wall gone to rust with dark portholes where it stands tall, a bulwark with a rail where
    /// it stands low, a roof of rusted plate. It blocks nothing by itself.
    /// </summary>
    ShipCabin,
    /// <summary>
    /// A Pokéblock feeder of Hoenn's Safari Zone (plan 21 · H11, part f; our own drawing, <c>HoennArt.PaintFeeder</c>): a
    /// roofed box on a post, a card on the feeder's tile, which the import blocks already. An overlay's prop; it blocks
    /// nothing by itself.
    /// </summary>
    PokeblockFeeder,
    /// <summary>
    /// A workbench (plan 39 · CR1, the craftswoman's workshop in Sunyshore City): a plank top on trestles with a vice, a
    /// mallet, a row of chisels and a heap of shavings, our own drawing (<c>WorkshopArt</c>). Past the decorations like the
    /// laboratory's tank: the room's own tiles say that it blocks.
    /// </summary>
    Workbench,
    /// <summary>
    /// A drill target (plan 36 · TR2, the training hall): a round board of rings in a square frame hung on the back wall
    /// over its tile, our own drawing (<c>TrainingArt</c>). It lies on the wall's row as a painting does and blocks nothing.
    /// </summary>
    DrillTarget,
    /// <summary>
    /// A training mat (plan 36 · TR2): a padded blue mat with a white border and a lane line laid on the floor as a rug
    /// is, over any number of tiles (<c>TrainingArt</c>); walked on, blocking nothing.
    /// </summary>
    TrainingMat,
    /// <summary>
    /// A Zygarde Cell (plan 23, the rest of the islands: Sun and Moon's hundred pieces of Zygarde about Alola): a thing of the
    /// map (<see cref="NPC.ZygardeCellType"/>), a small green blob with a red core, drawn as a card and gathered into the
    /// Zygarde Cube when spoken to (<c>common.ZygardeCell</c>).
    /// </summary>
    ZygardeCell,
    /// <summary>
    /// A plinth of the Laurel Gallery (plan 41 · EG2, EG4): pale stone with a laurel wreath of a region's colour on its face
    /// and standing on its slab, our own drawing (<c>HubArt</c>); its region is the prop's <see cref="Prop.Model"/>. The
    /// gallery is furnished with one for each region finished; the room's own tiles say that it blocks.
    /// </summary>
    Plinth,
    /// <summary>
    /// A team's banner in Grayfall Keep (plan 41 · EG9): a cloth of the team's colours of ours hung on the back wall over its
    /// tile, its team the prop's <see cref="Prop.Model"/> (<c>HubArt</c>). It lies on the wall's row as a painting does and
    /// blocks nothing.
    /// </summary>
    Banner,
    /// <summary>
    /// The festivals' things (plan 40 · GE9; style guide, "Life", "Festivals"): things of the map shown by a festival's flag
    /// (<see cref="NPC.FestivalLanternsType"/> and its fellows), each a card of our own (<c>FestivalArt</c>) in the way like a
    /// person: paper lanterns on a string, Kanto's moon lantern, Snowpoint's lit pine, Floaroma's blossom arch, a stall's
    /// awning, Johto's bells and maple garland, Hoenn's bunting, and the holidays' decorated pine, basket of eggs, New Year's
    /// banner and St Valentine's heart on a post.
    /// </summary>
    FestivalLanterns,
    FestivalMoonLantern,
    FestivalPine,
    FestivalArch,
    FestivalStall,
    FestivalBells,
    FestivalBunting,
    HolidayTree,
    EggBasket,
    NewYearBanner,
    ValentineHeart,
    /// <summary>
    /// The gathering spots of the field (plan 39 · CR2; style guide, "Props", "Gathering spots"): things of the map
    /// (<see cref="NPC.GatherSpotType"/>), in five kinds, each bearing (something loose on it to take today) or bare
    /// (picked today), drawn as cards of our own (<c>GatherArt</c>).
    /// </summary>
    GatherRock,
    GatherRockBare,
    GatherWood,
    GatherWoodBare,
    GatherPlant,
    GatherPlantBare,
    GatherShore,
    GatherShoreBare,
    GatherSnow,
    GatherSnowBare,
    /// <summary>A photo spot's plate (plan 43 · LF4): a low post with a camera on its face, read from the tile before it; blocks nothing.</summary>
    PhotoSpot,
    /// <summary>The film studio's poster wall (plan 42 · PS4, part b): a hoarding with the four films' posters in warm reds, read from the tile before it; appended past the decorations and blocks its tiles all the same, as the tower does.</summary>
    PosterWall,
    /// <summary>Kalos's Solstice Serenade (plan 40 · GE9): a music stand of wrought iron with a sheet of songs and ribbons; a festival's thing.</summary>
    FestivalMusicStand,
    /// <summary>Alola's Garland Days (plan 40 · GE9): two bamboo poles with a lei of flowers swung between them and a crystal on each; a festival's thing.</summary>
    FestivalLeiArch,
    /// <summary>Galar's Wool Fair (plan 40 · GE9): bales of straw stacked with a prize rosette and a tuft of fleece; a festival's thing.</summary>
    FestivalHayBales,
    /// <summary>Hisui's Sheaf Feast (plan 40 · GE9): three sheaves of wheat bound together before a stake with a paper tally; a festival's thing.</summary>
    FestivalSheaves,
    /// <summary>The Sevii Islands' fast ferry at its harbour (plan 19's last notes): a thing of the map (<see cref="NPC.SeagallopType"/>), drawn as a card (<c>SeviiArt</c>).</summary>
    Seagallop,
    /// <summary>Celio's network machine in One Island's Pokémon Center (plan 19's last notes): a thing of the map (<see cref="NPC.NetworkMachineType"/>), drawn as a card.</summary>
    NetworkMachine,
    /// <summary>
    /// An object an Owner has set down in the Johto Safari Zone (plan 38 · EX8; style guide, "Props", "The Safari Zone's
    /// objects"): its type the prop's <see cref="Prop.Model"/> (<c>safari:&lt;key&gt;</c>), our own drawing (<c>SafariArt</c>).
    /// It blocks nothing, so no arrangement can shut a way.
    /// </summary>
    SafariObject,
}

/// <summary>A piece of furniture or decoration placed in a room, covering a rectangle of tiles.</summary>
public sealed class Prop
{
    public PropType Type { get; init; }
    public int X { get; init; }
    public int Y { get; init; }
    public int Width { get; init; } = 1;
    public int Depth { get; init; } = 1;

    /// <summary>For a prop placed by a model of the world: how tall the model stands, in tiles. 0 otherwise.</summary>
    public float Height { get; init; }

    /// <summary>For a prop placed by a model of the world: the model's short name, which picks among looks of one type.</summary>
    public string Model { get; init; } = "";

    /// <summary>A colour a prop is drawn in where its kind takes one (a booth's awning: Pennant Hall's stalls, plan 41 · EG3), as <c>#rrggbb</c>; empty for its own.</summary>
    public string Tint { get; init; } = "";

    /// <summary>Whether it blocks every tile it covers. A conveyor and its gantry stand over open ground: the world says which of their tiles block.</summary>
    // Lumiose City's tower (plan 23 · Z2) is appended past the decorations and blocks all the same: nothing walks through it
    public bool IsSolid => Type < PropType.Rug && Type is not (PropType.Conveyor or PropType.Gantry) || Type is PropType.PrismTower or PropType.PosterWall;

    /// <summary>Reception and shop counters: you can talk to whoever stands behind them.</summary>
    public bool IsCounter => Type is PropType.Counter or PropType.LabDesk;

    public bool Covers(int x, int y) => x >= X && x < X + Width && y >= Y && y < Y + Depth;
}
