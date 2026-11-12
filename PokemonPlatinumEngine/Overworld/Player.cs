using System;
using System.Numerics;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Overworld;

public class Player
{
    public const int TileSize = 32; // Full HD 32x32 grid tile size

    public int GridX { get; set; } = 8;
    public int GridY { get; set; } = 8;
    public Direction Facing { get; set; } = Direction.Down;

    public float PixelX { get; private set; }
    public float PixelY { get; private set; }

    /// <summary>Height of a hop's arc above the ground, in pixels (0 when on the ground).</summary>
    public float HopHeight { get; private set; }

    /// <summary>Walk cycle for the 3D model: advances one unit every two steps.</summary>
    public float WalkCycle { get; private set; }

    /// <summary>0 standing still .. 1 walking, eased so the legs settle smoothly when stopping.</summary>
    public float WalkBlend { get; private set; }

    /// <summary>Facing as an angle about the vertical axis (0 = toward the camera), eased when turning.</summary>
    public float Yaw { get; private set; }

    /// <summary>Progress through the current hop (0 when not hopping).</summary>
    public float HopProgress => IsHoppingLedge ? moveProgress : 0f;

    public bool IsMoving { get; private set; }
    public bool IsRunning { get; private set; }

    /// <summary>Holds the 3D model in a pose of its walk for a picture (the screenshot harness), as <see cref="Update"/> would leave it.</summary>
    internal void Pose(float walk, float blend, bool running)
    {
        WalkCycle = walk;
        WalkBlend = blend;
        IsRunning = running;
    }

    /// <summary>In the air: over a ledge, or between the shore and the Pokémon that carries the player on water.</summary>
    public bool IsHoppingLedge { get; private set; }

    /// <summary>Carried by ice or a moving floor: moving without walking, whatever is pressed.</summary>
    public bool IsSliding { get; private set; }

    /// <summary>
    /// How fast the player slides on ice, nought to three: gathered going down its slopes and lost going up them,
    /// gone once the slide stops (<see cref="IceSlide"/>, plan 01 · M9, part 2b).
    /// </summary>
    public int IceSpeed { get; private set; }

    // The step under way is a slip back down an icy slope, after which the player stands
    private bool slipping;

    // A snowball the player slid into and broke, for the game to play its burst
    private NPC? broken;

    /// <summary>The snowball broken by sliding into it since this was last asked, once: the game plays its burst and its sound.</summary>
    public NPC? TakeBroken()
    {
        var ball = broken;
        broken = null;
        return ball;
    }

    /// <summary>How the player is getting about: on foot, on a Pokémon's back across water, or by Bicycle.</summary>
    public TravelMode Mode { get; private set; }

    /// <summary>What the party's Pokémon let the player do in the field (<see cref="FieldMovement.MovesOf"/>); the game keeps it up to date.</summary>
    public FieldMoves Moves { get; set; }

    /// <summary>The Bicycle's fast gear, which is what gets up a muddy slope. Only matters while cycling.</summary>
    public bool FastGear { get; set; }

    /// <summary>
    /// Which bicycle the player rides (plan 21 · H3): Platinum's, or Emerald's Mach or Acro Bike, as the item used to get
    /// on says. Only matters while cycling.
    /// </summary>
    public BikeKind Bike { get; set; }

    /// <summary>
    /// Whether Strength is in force (<see cref="FieldMoveRules.StrengthFlag"/>; the game keeps it up to date): walking
    /// into a boulder then pushes it a tile on, if there is room beyond it.
    /// </summary>
    public bool PushesBoulders { get; set; }

    /// <summary>The Running Shoes Mom gives (plan 02 · S4): without them the run button does nothing on foot.</summary>
    public bool HasRunningShoes { get; set; } = true;

    /// <summary>
    /// Whether the run button puts the skates on here (plan 23 · Z1; the game keeps it up to date): the place's region
    /// skates and the skates are had. Then a step taken with the button held is a skater's
    /// (<see cref="TravelMode.Skating"/>), and on ground skates don't take a walker's instead.
    /// </summary>
    public bool CanSkate { get; set; }

    // A skater who has stopped steering glides a tile on, once (FieldMovement.Coast)
    private bool coasted;
    // The run button held with skates to put on, as the step is chosen
    private bool skating;

    /// <summary>
    /// A boulder the player has just pushed, and which way, for the game to slide it along (it is already on its
    /// new tile); taken with <see cref="TakePush"/>. Meanwhile the player walks on the spot behind it.
    /// </summary>
    private (NPC Boulder, Direction Way)? pushed;
    private float pushing;

    /// <summary>The boulder pushed this frame, once: the game slides it and plays its sound.</summary>
    public (NPC Boulder, Direction Way)? TakePush()
    {
        var push = pushed;
        pushed = null;
        return push;
    }

    /// <summary>True while the player leans into a boulder that is sliding on.</summary>
    public bool IsPushing => pushing > 0f;

    /// <summary>
    /// How many tiles the last push sends its boulder or block of ice (plan 20 · J11): one for a boulder, as many as
    /// the ice carries a block (<see cref="FieldMovement.IceBlockRun"/>). The game slides it over them.
    /// </summary>
    public int PushedTiles { get; private set; } = 1;

    /// <summary>
    /// Pushes a block of ice in the way, if it can move (plan 20 · J11): it is on its new tile at once and slides
    /// there, and the player leans after it as behind a boulder. No field move is asked for: HeartGold's blocks slide
    /// for anyone. False where the block can't move.
    /// </summary>
    private bool PushIceBlock(Map map, Direction dir)
    {
        if (Mode != TravelMode.OnFoot) return false;
        var (dx, dy) = FieldMovement.Delta(dir);
        if (map.GetNpcAt(GridX + dx, GridY + dy) is not { IsIceBlock: true } block) return false;
        var (x, y, tiles) = FieldMovement.IceBlockRun(map, block, dir);
        if (tiles == 0) return false;
        block.GridX = x;
        block.GridY = y;
        pushed = (block, dir);
        PushedTiles = tiles;
        pushing = Math.Max(FieldMovement.BoulderPushSeconds, tiles * FieldMovement.IceBlockSecondsATile);
        Facing = dir;
        return true;
    }

    // A step on the spot a script asks for (plan 11 · C11, the original's WalkOnSpot): the legs walk where the
    // player stands, as they do behind a boulder that slides on
    private float onSpot;

    /// <summary>True while the player takes a step on the spot.</summary>
    public bool IsSteppingOnSpot => onSpot > 0f;

    /// <summary>
    /// Turns to <paramref name="way"/> and takes a step there on the spot, as long as a step at that pace takes (a
    /// script's <c>onspot</c>). False in the middle of a step.
    /// </summary>
    public bool StepOnSpot(Direction way, bool fast)
    {
        if (IsMoving || carried != null || pushing > 0f) return false;
        Facing = way;
        onSpot = 1f / FieldMovement.TilesPerSecond(fast ? Pace.Fast : Pace.Walk);
        stride = FieldMovement.TilesPerSecond(fast ? Pace.Fast : Pace.Walk);
        return true;
    }

    /// <summary>
    /// Takes a step <paramref name="way"/> facing as the player faces (a script's <c>back</c>, the original's
    /// <c>LockDir</c>): a step back when the way is behind them. The field's rules say whether it can be taken. False
    /// when it can't, or in the middle of a step.
    /// </summary>
    public bool StepKeepingFacing(Map map, Direction way)
    {
        if (IsMoving || carried != null || pushing > 0f || onSpot > 0f) return false;
        var step = FieldMovement.Step(map, GridX, GridY, way, WalkerOn(map) with { Running = false });
        if (!step.Moves) return false;
        Begin(map, step, way);
        return true;
    }

    /// <summary>Counts the steps since the last wild battle or map change, for the odds of the next (<see cref="EncounterSteps"/>).</summary>
    public EncounterSteps Encounters { get; } = new();

    /// <summary>The Pokémon at the head of the party, whose ability shapes the wild Pokémon met (<see cref="WildEncounterRules"/>); the game keeps it up to date.</summary>
    public WildLead? Lead { get; set; }

    /// <summary>
    /// What the moment brings to the wild Pokémon met (plan 06 · R13, <see cref="EncounterMoment"/>), asked for as a
    /// step is taken; left out, a place's table is its own. The game sets it.
    /// </summary>
    public Func<EncounterMoment?>? Moment { get; set; }

    /// <summary>
    /// Wild Pokémon are seen in the field (plan 10 · F5, the options' WILD POKéMON row): a step into tall grass or on
    /// the water rolls nothing, since what lives there stands in sight and is met by touching it. Caves roll as ever.
    /// </summary>
    public bool WildInSight { get; set; }

    /// <summary>
    /// The run button creeps instead (plan 10 · F12): set by the engine while the DexNav has found a Pokémon in the
    /// grass nearby, so that Run held walks at <see cref="Pace.Sneak"/> and keeps it there.
    /// </summary>
    public bool SneaksInsteadOfRunning { get; set; }

    /// <summary>The player is creeping this step (<see cref="SneaksInsteadOfRunning"/> and the run button held).</summary>
    public bool IsSneaking { get; private set; }

    /// <summary>The player walked on the spot this frame against something in their way (plan 10 · F5: a wild Pokémon walked into).</summary>
    public bool PushedAgainst { get; private set; }

    private float moveProgress = 0f;
    private int targetGridX = 0;
    private int targetGridY = 0;

    // Free movement (plan 30 · FM7): the body's centre as an offset from the counted tile's centre, in 256ths of a
    // tile (FreeMovement.SubTile). Nought on the grid, always: only a free region's steering, and a save made in one,
    // moves it off the centre
    private int offX, offY;
    // Where a step of the grid sets out from, in pixels: the tile's corner on the grid, and wherever the body stood in a
    // free region (a hand-off begins from the centre, a surf or a climb from wherever the player was)
    private float startPixelX, startPixelY;
    // A hand-off under way: the body eases onto the centre of its tile, then the grid's step this way is played
    private Direction? handOff;
    // The way a free body last moved, and the cardinal it was facing then: the yaw is eased to that way while the
    // facing is still that cardinal (a script that turns the player turns them to the cardinal again)
    private (float Yaw, Direction Facing)? freeYaw;
    // The height of the tile the free centre is in, as the grid's step onto it gave it
    private float standing = float.NaN;
    // How long a full tilt has been held (a full tilt held a second runs, FM9)
    private float fullTiltFor;
    // The counted tile left this frame by free movement, for whoever walks behind
    private (int X, int Y)? leftTile;

    // The step being taken: how much of it passes each second, how fast the legs go, the heights it goes
    // between, how high it arcs, which way it goes and how the player travels once it ends
    private float stepRate = 4.5f;
    private float stride = 4.5f;
    private float fromHeight, toHeight;
    private float hopArc;
    private Direction movingDir = Direction.Down;
    private TravelMode modeAfter;
    private StepKind stepKind;
    private bool mounting;

    // The height underfoot; unknown after being put somewhere, until the next update looks at the map
    private float height = float.NaN;

    // False from being put somewhere until the next update has looked at where that is
    private bool settled;

    // Where ice or a moving floor takes the player next, once the step onto it has ended
    private Direction? carried;
    private Direction? spinning;

    // Walking into something plays one thud per step-length, not one per frame
    private const float BumpInterval = 0.4f;
    private float bumpCooldown = 0f;

    // Grass stepping effects
    /// <summary>True when the step that just ended was a hop: over a ledge, or between the bank and a Pokémon's back.</summary>
    public bool JustLanded { get; private set; }

    /// <summary>True when the step that just ended took the player out onto the water.</summary>
    public bool JustRodeOut { get; private set; }

    /// <summary>The tile a step in progress is taking the player to; where they stand, between steps.</summary>
    public (int X, int Y) Heading => IsMoving ? (targetGridX, targetGridY) : Standing;

    /// <summary>
    /// The tile the body's centre is in (plan 30 · FM7): the tile stood on, on the grid; in a free region it may be one
    /// the centre has crossed into and not yet gone far enough into to count (<see cref="FreeMovement.Counted"/>).
    /// </summary>
    public (int X, int Y) Standing => IsMoving || (offX == 0 && offY == 0) ? (GridX, GridY)
        : (FreeMovement.TileOf(FreeMovement.CentreOf(GridX) + offX), FreeMovement.TileOf(FreeMovement.CentreOf(GridY) + offY));

    /// <summary>
    /// The body's centre in 256ths of a tile (plan 30 · FM7): a tile's centre is its number × 256. On the grid it is the
    /// tile's centre between steps; free, wherever the stick has taken it.
    /// </summary>
    public int SubX => IsMoving ? (int)MathF.Round(PixelX * (FreeMovement.SubTile / (float)TileSize)) : FreeMovement.CentreOf(GridX) + offX;

    /// <inheritdoc cref="SubX"/>
    public int SubY => IsMoving ? (int)MathF.Round(PixelY * (FreeMovement.SubTile / (float)TileSize)) : FreeMovement.CentreOf(GridY) + offY;

    /// <summary>
    /// How far the body's centre stands off the counted tile's centre, in 256ths (plan 30 · FM7): what a save keeps.
    /// Always nought on the grid and in the middle of a step.
    /// </summary>
    public (int X, int Y) Offset => IsMoving ? (0, 0) : (offX, offY);

    /// <summary>
    /// Puts the body's centre off its tile's centre by an offset in 256ths (a save made in a free region, a test): the
    /// tile stays the counted one. An offset that would count another tile is cut to the most this one keeps.
    /// </summary>
    public void SetOffset(int x, int y)
    {
        int most = FreeMovement.Half + FreeMovement.Hysteresis - 1;
        offX = Math.Clamp(x, -most, most);
        offY = Math.Clamp(y, -most, most);
        PlaceFromSub();
    }

    // The pixel place from the free centre: eight 256ths to a texel
    private void PlaceFromSub()
    {
        PixelX = (FreeMovement.CentreOf(GridX) + offX) / (float)(FreeMovement.SubTile / TileSize);
        PixelY = (FreeMovement.CentreOf(GridY) + offY) / (float)(FreeMovement.SubTile / TileSize);
    }

    /// <summary>Whether the body moved freely this frame (plan 30 · FM7): the legs walk, though no step of the grid is under way.</summary>
    public bool IsGliding { get; private set; }

    /// <summary>
    /// Whether the region's speed is the stick's tilt (plan 30 · FM9, <see cref="MovementSpeed.Analog"/>); the engine
    /// sets it every frame from <c>GameEngine.MovementHere</c>.
    /// </summary>
    public bool AnalogSpeed { get; set; }

    /// <summary>The tile free movement left this frame, once (plan 30 · FM8): whoever walks behind goes there.</summary>
    public (int X, int Y)? TakeLeft()
    {
        var left = leftTile;
        leftTile = null;
        return left;
    }

    /// <summary>
    /// What talking and pressing ask in a free region (plan 30 · FM7): the tile <see cref="FreeMovement.AheadReach"/> ahead
    /// of the centre along the yaw, and the tile beside the centre's in the cardinal faced, which is asked next when the
    /// first has nobody. On the grid both are the tile faced.
    /// </summary>
    public ((int X, int Y) Ahead, (int X, int Y) Faced) InteractTiles()
    {
        var (dx, dy) = FieldMovement.Delta(Facing);
        if (Directions != MovementDirections.Free || IsMoving) return ((GridX + dx, GridY + dy), (GridX + dx, GridY + dy));
        var (sx, sy) = Standing;
        var faced = (sx + dx, sy + dy);
        // (a talk's turn is the body's look alone: a press aims where the body was steered)
        float aim = talkYaw == null ? Yaw : freeYaw is { } free && free.Facing == Facing ? free.Yaw : YawOf(Facing);
        var ahead = FreeMovement.AheadTile(SubX, SubY, aim);
        return (ahead == Standing ? faced : ahead, faced);
    }

    /// <summary>
    /// In a free place, turns the body at once to face a tile's middle from its own tile's middle (plan 30, the gate's owed:
    /// someone spoken to diagonally is faced, not the nearest of the four ways); <see cref="Facing"/> becomes the cardinal
    /// nearest that way, which the grid's rules read. On the grid, or moving, nothing changes.
    /// </summary>
    public void TurnToFace(int x, int y)
    {
        if (Directions != MovementDirections.Free || IsMoving) return;
        // (aimed from the tile's centre, which a held body eases onto while the talk has the field, StandStill)
        var way = new Vector2(FreeMovement.CentreOf(x) - FreeMovement.CentreOf(GridX), FreeMovement.CentreOf(y) - FreeMovement.CentreOf(GridY));
        if (way.LengthSquared() < 1f) return;
        // (the body's look alone: Facing and the stick's way stay as they were, so nothing the rules read, and no next
        // press, aims elsewhere; the turn holds until the body moves or is turned, Settle)
        talkYaw = (FreeMovement.YawOf(Vector2.Normalize(way)), Facing);
        Yaw = talkYaw.Value.Yaw;
    }

    // The way a talk turned the body to (TurnToFace) and the facing it was turned at; null once it moves or turns
    private (float Yaw, Direction Facing)? talkYaw;

    /// <summary>The pace of the step under way, in tiles a second: what someone walking behind keeps up with.</summary>
    public float Stride => stride;

    /// <summary>What shows in the bubble over the player's head, for how much longer, and for how long it has.</summary>
    public EmoteBubble Bubble { get; private set; }
    public float BubbleTimer { get; private set; }
    public float BubbleAge { get; private set; }

    public void ShowBubble(EmoteBubble bubble, float seconds = 0.9f)
    {
        Bubble = bubble;
        BubbleTimer = seconds;
        BubbleAge = 0f;
    }

    /// <summary>Runs the bubble's clock.</summary>
    public void TickBubble(float dt)
    {
        if (BubbleTimer <= 0f) return;
        BubbleTimer -= dt;
        BubbleAge += dt;
    }

    /// <summary>The gesture a script has them play (plan 11 · C11), null when none is under way.</summary>
    public Gesture? Gesture { get; private set; }

    /// <summary>Seconds since the gesture began, over every round of it.</summary>
    public float GestureAge => gestureAge;

    /// <summary>How many times in a row the gesture plays.</summary>
    public int GestureTimes { get; private set; }

    /// <summary>The face a script has them hold (plan 11 · C11) until it changes or the place is come to again.</summary>
    public Mood Mood { get; set; }

    private float gestureAge;

    /// <summary>Plays a gesture <paramref name="times"/> times in a row, from its start.</summary>
    public void Act(Gesture gesture, int times = 1)
    {
        Gesture = gesture;
        GestureTimes = Math.Max(1, times);
        gestureAge = 0f;
    }

    /// <summary>Runs the gesture's clock, as <see cref="TickBubble"/> runs the bubble's.</summary>
    public void TickGesture(float dt) => Gesture = Acting.Tick(Gesture, ref gestureAge, GestureTimes, dt);

    /// <summary>
    /// A gesture held at <paramref name="share"/> of its round for a photograph (plan 43 · LF4): photo mode runs no
    /// field, so its clock stands where it is put. Null lets go.
    /// </summary>
    public void HoldGesture(Gesture? gesture, float share)
    {
        Gesture = gesture;
        GestureTimes = 1;
        gestureAge = gesture is { } g ? share * Acting.Length(g) : 0f;
    }

    /// <summary>Ends any gesture and lets the face go: the place has been come to again.</summary>
    /// <summary>The stance a script's <c>pose</c> set the player in (plan 11 · C7), until they come to another place; null standing.</summary>
    public Stance? Posed { get; set; }

    public void StopActing()
    {
        Gesture = null;
        gestureAge = 0f;
        Mood = Mood.Neutral;
    }

    public bool InTallGrass { get; private set; }
    public float GrassRustleTimer { get; private set; }

    public Player(int startX, int startY)
    {
        SetPosition(startX, startY, Direction.Down);
    }

    public void SetPosition(int gx, int gy, Direction facing)
    {
        GridX = gx;
        GridY = gy;
        targetGridX = gx;
        targetGridY = gy;
        Facing = facing;
        PixelX = gx * TileSize;
        PixelY = gy * TileSize;
        HopHeight = 0f;
        IsMoving = false;
        DiagonalStep = null;
        JustLanded = false;
        JustRodeOut = false;
        IsHoppingLedge = false;
        IsSliding = false;
        IceSpeed = 0;
        slipping = false;
        mounting = false;
        carried = null;
        spinning = null;
        pushed = null;
        pushing = 0f;
        onSpot = 0f;
        moveProgress = 0f;
        height = float.NaN;
        settled = false;
        offX = offY = 0;
        handOff = null;
        freeYaw = null;
        talkYaw = null;
        standing = float.NaN;
        fullTiltFor = 0f;
        leftTile = null;
        IsGliding = false;
        Yaw = YawOf(facing);
        WalkBlend = 0f;
        Encounters.Reset();
    }

    /// <summary>Puts the player on foot, on the water or on the Bicycle outright: a loaded save, a test, the harness.</summary>
    public void SetMode(TravelMode mode)
    {
        // (a ride is got on only by SetRiding, which says whose back it is: a save made riding wakes on foot)
        if (mode == TravelMode.Riding && Steed == null) mode = TravelMode.OnFoot;
        if (mode != TravelMode.Riding) { if (RidePager.IsWaterRide(Steed)) Carrier = carrierBeforeJet; Steed = null; }
        Mode = mode;
    }

    /// <summary>
    /// Whoever comes down through a hole onto water rides out on it there (plan 19 · K6, part b: FireRed's
    /// <c>ForcePlayerToStartSurfing</c> under the Seafoam Islands' holes), and a current under them takes them on at
    /// once. Called as the player is put down on a map by a warp; nothing happens on ground or for someone already
    /// on the water.
    /// </summary>
    public void ComeDownOn(Map map)
    {
        var underfoot = map.BehaviourAt(GridX, GridY);
        if (!TileBehaviors.IsSurfable(underfoot) || underfoot == TileBehavior.Waterfall) return;
        Mode = TravelMode.Surfing;
        height = float.NaN;
        carried = CarriedBy(underfoot, Facing);
        IsSliding = carried != null;
    }

    /// <summary>
    /// What the tile just come onto carries the player on by (<see cref="FieldMovement.Carries"/>), but for a surfer on
    /// Sharpedo's back, who shoots through a current as it flows (<see cref="RidePager.StemsCurrents"/>; plan 23, Alola's
    /// strong currents) and is carried nowhere.
    /// </summary>
    private Direction? CarriedBy(TileBehavior underfoot, Direction arrivedMoving) =>
        Mode == TravelMode.Surfing && RidePager.StemsCurrents(Steed) ? null : FieldMovement.Carries(underfoot, arrivedMoving);

    /// <summary>The height of the ground (or deck, or water) under the player on a map, in tiles; eased from tile to tile while moving.</summary>
    public float HeightOn(Map map) => float.IsNaN(height) ? map.HeightAt(GridX, GridY) : height;

    /// <summary>
    /// Says which level the player stands on where a tile has two: after <see cref="SetPosition"/> the player is
    /// on the ground, and a save made on a bridge puts them back on its deck with this.
    /// </summary>
    public void SetHeight(float standing) => height = standing;

    /// <summary>
    /// Carried by something moving under the player (the Canalave Gym's platforms, plan 01 · M9): stands still on it
    /// wherever it has got to, in tiles, at its height, and is on the tile it is over.
    /// </summary>
    public void CarryTo(float x, float y, float standing, float dt)
    {
        PixelX = x * TileSize;
        PixelY = y * TileSize;
        GridX = targetGridX = (int)MathF.Round(x);
        GridY = targetGridY = (int)MathF.Round(y);
        height = standing;
        StandStill(dt);
    }

    /// <summary>
    /// Where the Pokémon the player rides on water is, in tiles, or null on land: under the player while
    /// surfing, and waiting on its tile of water while the player hops onto it or off it.
    /// </summary>
    public (float X, float Y)? Mount
    {
        get
        {
            if (mounting) return (targetGridX, targetGridY);
            if (IsMoving && stepKind == StepKind.Land) return (GridX, GridY);
            return Mode == TravelMode.Surfing ? (PixelX / TileSize, PixelY / TileSize) : null;
        }
    }

    /// <summary>
    /// The Pokémon that carries the player on the water, by the name its model goes by (<see cref="Models.Pokemon.ModelName"/>):
    /// the one whose Surf was used (plan 10 · F1). Null draws the swimmer every Pokémon shares.
    /// </summary>
    public string? Carrier { get; set; }

    /// <summary>
    /// How far onto the Pokémon's back the player is: 1 while surfing, rising from 0 through the hop onto it
    /// and falling back to 0 through the hop off it.
    /// </summary>
    public float Saddle =>
        mounting ? moveProgress
        : Mode != TravelMode.Surfing ? 0f
        : IsMoving && stepKind == StepKind.Land ? 1f - moveProgress
        : 1f;

    /// <summary>Model yaw for a facing direction; the model faces +Z (toward the camera) at 0.</summary>
    public static float YawOf(Direction facing) => facing switch
    {
        Direction.Up => MathF.PI,
        Direction.Left => -MathF.PI / 2f,
        Direction.Right => MathF.PI / 2f,
        _ => 0f
    };

    private Walker WalkerOn(Map map) => new(Mode, HeightOn(map), IsRunning, FastGear, Moves, Bike: Bike, Sneaking: IsSneaking, Steed: Steed);

    /// <summary>One frame of the player's movement, steered by the keys held.</summary>
    public void Update(float dt, Map map, Action<WildEncounterEntry> onWildEncounter, Action<Warp> onWarpTrigger, Func<bool>? onArrive = null)
    {
        // Free movement (plan 30 · FM7): the stick's tilt, or the keys' eight ways at a full tilt, steers the body
        if (MovesFreely(map))
        {
            SteerFreely(dt, map, InputManager.GetStick(), InputManager.IsActionDown(GameAction.Run) != Autorun, onWildEncounter, onWarpTrigger, onArrive);
            return;
        }
        Vector2 dir = InputManager.GetMovementVector();
        // The eight ways the keys or the stick hold (plan 30 · FM1): two directions held at once are a diagonal, read
        // here from the frame's four direction actions and nowhere else. Where the place moves four ways the grid's
        // four-way answer is walked, as it always was (the stronger axis, a tie the vertical one); where it moves
        // eight, the diagonal itself (plan 30 · FM2)
        WantedHeading = Headings.FromVector(dir);
        bool run = InputManager.IsActionDown(GameAction.Run) != Autorun;
        // With Autorun (plan 08 · P2) the run button walks and its absence runs; Advance itself is unchanged
        if (Directions != MovementDirections.Four && WantedHeading is { } heading && heading.IsDiagonal())
            Steer(dt, map, heading, run, onWildEncounter, onWarpTrigger, onArrive);
        else
            Advance(dt, map, Headings.FourWay(dir), run, onWildEncounter, onWarpTrigger, onArrive);
    }

    /// <summary>
    /// One frame of the player's movement steered one of the eight ways (plan 30 · FM2), as a tool or a test hands it:
    /// a cardinal heading is <see cref="Advance"/>'s own; a diagonal is walked as a diagonal only where
    /// <see cref="Directions"/> allows eight, and is its vertical part where the place moves four ways.
    /// </summary>
    public void Steer(float dt, Map map, PokemonPlatinumEngine.Overworld.Heading? want, bool run, Action<WildEncounterEntry>? onWildEncounter, Action<Warp>? onWarpTrigger, Func<bool>? onArrive = null)
    {
        if (want is { } heading && heading.IsDiagonal() && Directions != MovementDirections.Four)
            Move(dt, map, null, heading, null, run, onWildEncounter, onWarpTrigger, onArrive);
        else
            Advance(dt, map, want is { } h ? h.Cardinal() ?? h.Parts().Vertical : null, run, onWildEncounter, onWarpTrigger, onArrive);
    }

    /// <summary>
    /// Whether the body moves freely here (plan 30 · FM7): the place moves <see cref="MovementDirections.Free"/> and no
    /// Gym's puzzle is laid on the map (a puzzle's rooms are walked a cardinal step at a time, as they refuse diagonals).
    /// </summary>
    public bool MovesFreely(Map map) => Directions == MovementDirections.Free && map.Puzzle == null;

    /// <summary>
    /// One frame of free movement (plan 30 · FM7), steered by a stick as a tool, a test or <see cref="Update"/> hands it
    /// (−1 to 1 on each axis, x right and y down; its length the tilt). <paramref name="run"/> is the run button (with
    /// Autorun already turned round); where the speed is analog a full tilt held a second runs too (FM9). Where the place
    /// doesn't move freely the stick is its eight-way heading, steered as <see cref="Steer"/> steers.
    /// </summary>
    public void SteerFreely(float dt, Map map, Vector2 stick, bool run, Action<WildEncounterEntry>? onWildEncounter, Action<Warp>? onWarpTrigger, Func<bool>? onArrive = null)
    {
        if (!MovesFreely(map))
        {
            // (the eight ways the stick's tilt names, by the stick reader's own share)
            var ways = StickReader.Ways(stick.X, stick.Y);
            var bits = new Vector2(((ways & StickDirs.Right) != 0 ? 1f : 0f) - ((ways & StickDirs.Left) != 0 ? 1f : 0f),
                ((ways & StickDirs.Down) != 0 ? 1f : 0f) - ((ways & StickDirs.Up) != 0 ? 1f : 0f));
            Steer(dt, map, Headings.FromVector(bits), run, onWildEncounter, onWarpTrigger, onArrive);
            return;
        }
        float tilt = Math.Min(1f, stick.Length());
        fullTiltFor = AnalogSpeed && tilt >= FreeMovement.FullTilt ? fullTiltFor + dt : 0f;
        bool running = run || (AnalogSpeed && fullTiltFor >= FreeMovement.RunAfterSeconds);
        Move(dt, map, null, null, tilt > 0f ? stick : null, running, onWildEncounter, onWarpTrigger, onArrive);
    }

    /// <summary>
    /// The cardinal a diagonal leaves the player facing (plan 30 · FM2): the part of the two they already face, so
    /// adding a second direction to the first keeps the body turned as it was; otherwise the vertical part, as the
    /// grid's four-way answer takes a tie. Until FM3 and FM4 draw eight facings the sprite faces this cardinal too.
    /// </summary>
    public static Direction DiagonalFacing(PokemonPlatinumEngine.Overworld.Heading heading, Direction facing)
    {
        var (vertical, horizontal) = heading.Parts();
        return facing == horizontal ? horizontal : vertical;
    }

    /// <summary>
    /// The heading of the step under way when it is a diagonal (plan 30 · FM2); null between steps and for a cardinal
    /// step. <see cref="Facing"/> stays the cardinal <see cref="DiagonalFacing"/> gives, which is what everything else reads.
    /// </summary>
    public PokemonPlatinumEngine.Overworld.Heading? DiagonalStep { get; private set; }

    /// <summary>Whether the keys' run button is read the other way round (the options' Autorun, plan 08 · P2); running still needs the Running Shoes.</summary>
    public bool Autorun { get; set; }

    /// <summary>
    /// Which ways the player may step where they stand (plan 30 · FM1): the engine sets it every frame from
    /// <c>GameEngine.MovementHere</c>. Four everywhere before Kalos and under the options' FOUR WAYS; nothing reads more
    /// than four until FM2 builds the diagonal step.
    /// </summary>
    public MovementDirections Directions { get; set; } = MovementDirections.Four;

    /// <summary>The eight-way heading the keys or the stick held at the last <see cref="Update"/> (plan 30 · FM1); null for none.</summary>
    public PokemonPlatinumEngine.Overworld.Heading? WantedHeading { get; private set; }

    /// <summary>
    /// One frame of the player's movement with the steering given rather than read from the keys, which is how
    /// tests and the harness walk the player: a direction held turns the player to face it and then steps.
    /// </summary>
    /// <param name="onArrive">
    /// Called when a step ends on a new tile. True means something there takes over (a trainer catching the
    /// player's eye), so no wild Pokémon appears on that step.
    /// </param>
    /// <param name="onWildEncounter">Null for a walk nothing may interrupt (a script's): no wild Pokémon appears.</param>
    /// <param name="onWarpTrigger">Null for a walk that takes no door: stepping onto one only stands on it.</param>
    public void Advance(float dt, Map map, Direction? want, bool run, Action<WildEncounterEntry>? onWildEncounter, Action<Warp>? onWarpTrigger, Func<bool>? onArrive = null)
        => Move(dt, map, want, null, null, run, onWildEncounter, onWarpTrigger, onArrive);

    // A frame of movement steered by a cardinal (want) or, where eight ways are walked, a diagonal (slant); never both
    // (or, free, by a stick: plan 30 · FM7)
    private void Move(float dt, Map map, Direction? want, PokemonPlatinumEngine.Overworld.Heading? slant, Vector2? stick, bool run, Action<WildEncounterEntry>? onWildEncounter, Action<Warp>? onWarpTrigger, Func<bool>? onArrive)
    {
        if (bumpCooldown > 0f) bumpCooldown -= dt;
        bool walkingInPlace = false;
        PushedAgainst = false;
        IsGliding = false;

        // Leaning into a boulder as it slides: the player walks on the spot until it has come to rest
        if (pushing > 0f)
        {
            pushing -= dt;
            WalkCycle += dt * 1.1f;
            Settle(dt, walking: true);
            return;
        }

        // A step on the spot a script asked for: the legs take one step's worth of the walk
        if (onSpot > 0f)
        {
            onSpot -= dt;
            WalkCycle += stride * dt * 0.5f;
            Settle(dt, walking: true);
            return;
        }

        if (!settled)
        {
            // Just put here: stand on this map's ground unless told which level, and on foot unless this is water to float on
            settled = true;
            if (float.IsNaN(height)) height = map.HeightAt(GridX, GridY);
            if (Mode == TravelMode.Surfing && !map.IsDeepWater(GridX, GridY)) Mode = TravelMode.OnFoot;
            // On the sea floor everyone is a diver, and nowhere else (plan 21 · H5): a save made under the sea wakes there diving
            if (map.IsUnderwater) Mode = TravelMode.Diving;
            else if (Mode == TravelMode.Diving) Mode = map.IsDeepWater(GridX, GridY) ? TravelMode.Surfing : TravelMode.OnFoot;
        }

        // Off the tile's centre where the grid is to step (a script's walk, a tool's steering, a place on the grid; plan
        // 30 · FM8): the body eases onto the centre first, then the steps are the grid's own. Never on the grid itself,
        // where the body is always on its centre between steps
        if (!IsMoving && carried == null && (offX != 0 || offY != 0) && (want != null || slant != null || !MovesFreely(map)))
        {
            EaseToCentre(dt, GridX, GridY, FieldMovement.TilesPerSecond(Pace.Walk));
            Settle(dt, walking: true);
            return;
        }

        if (IsMoving)
        {
            moveProgress += stepRate * dt;
            if (!IsSliding) WalkCycle += stride * dt * 0.5f;

            if (moveProgress >= 1f)
            {
                if (Arrive(map, onWildEncounter, onWarpTrigger, onArrive)) return;
            }
            else
            {
                // Smooth interpolation
                float startX = startPixelX;
                float startY = startPixelY;
                float endX = targetGridX * TileSize;
                float endY = targetGridY * TileSize;

                PixelX = startX + (endX - startX) * moveProgress;
                PixelY = startY + (endY - startY) * moveProgress;
                height = fromHeight + (toHeight - fromHeight) * moveProgress;

                // The arc of a hop (height above the ground, in the same pixel units)
                HopHeight = MathF.Sin(moveProgress * MathF.PI) * hopArc;
            }
        }
        else if (carried is { } onward)
        {
            // Ice and moving floors: the next step takes itself, and ends the slide if something is in the way
            carried = null;
            if (map.BehaviourAt(GridX, GridY) == TileBehavior.Ice) Slide(map, onward);
            else
            {
                var step = FieldMovement.Step(map, GridX, GridY, onward, WalkerOn(map) with { Running = false });
                if (step.Moves) Begin(map, step, onward, sliding: true);
                else { IsSliding = false; spinning = null; }
            }
        }
        else if (Mode == TravelMode.Skating && want == null && slant == null && stick == null && !coasted
                 && FieldMovement.Coast(map, GridX, GridY, Facing, WalkerOn(map)) is { } glide)
        {
            // Skates let go of glide a tile on (plan 23 · Z1)
            coasted = true;
            Begin(map, glide, Facing, sliding: true);
        }
        else
        {
            // Idle state: follow the steering
            IsSliding = false;
            skating = CanSkate && run && Mode is TravelMode.OnFoot or TravelMode.Skating;
            // Standing still, or the button let go, the skates are off (plan 23 · Z1)
            if (Mode == TravelMode.Skating && ((want == null && slant == null && stick == null) || !skating)) Mode = TravelMode.OnFoot;
            // With the DexNav's find nearby, the run button creeps instead (plan 10 · F12)
            IsSneaking = Mode == TravelMode.OnFoot && run && SneaksInsteadOfRunning && !skating;
            IsRunning = Mode == TravelMode.OnFoot && run && HasRunningShoes && !skating && !IsSneaking;

            if (stick is { } freely)
            {
                // Free movement (plan 30 · FM7)
                walkingInPlace = Glide(dt, map, freely, onWildEncounter, onWarpTrigger, onArrive);
                if (walkingInPlace)
                {
                    PushedAgainst = true;
                    WalkCycle += dt * 1.1f;
                }
            }
            else if (slant is { } diagonal)
            {
                // A diagonal (plan 30 · FM2): turned to neither of its parts, the player turns first, as to a cardinal
                var face = DiagonalFacing(diagonal, Facing);
                if (Facing != face)
                {
                    Facing = face;
                }
                else
                {
                    TryDiagonal(diagonal, map);
                    if (!IsMoving)
                    {
                        walkingInPlace = true;
                        PushedAgainst = true;
                        WalkCycle += dt * 1.1f;
                    }
                }
            }
            else if (want is { } desiredDir)
            {
                if (Facing != desiredDir)
                {
                    Facing = desiredDir;
                }
                else
                {
                    TryStep(desiredDir, map);

                    if (!IsMoving)
                    {
                        // Blocked: walk in place against the obstacle, a little slower than walking
                        walkingInPlace = true;
                        PushedAgainst = true;
                        WalkCycle += dt * 1.1f;
                    }
                }
            }
            else if (FieldMovement.Pulls(map.BehaviourAt(GridX, GridY), Mode) is { } pulled
                && FieldMovement.Step(map, GridX, GridY, pulled, WalkerOn(map)) is { Moves: true } rolled)
            {
                // The Cycling Road's slope (plan 19 · K6): a rider who steers nowhere rolls on down it
                Facing = pulled;
                Begin(map, rolled, pulled);
            }
            else
            {
                bumpCooldown = 0f;
            }
        }

        Settle(dt, (IsMoving && !IsSliding && Mode == TravelMode.OnFoot) || walkingInPlace || (IsGliding && Mode == TravelMode.OnFoot));
    }

    /// <summary>Ends a step on its tile. True if something there took over: a warp, or a wild Pokémon.</summary>
    private bool Arrive(Map map, Action<WildEncounterEntry>? onWildEncounter, Action<Warp>? onWarpTrigger, Func<bool>? onArrive)
    {
        GridX = targetGridX;
        GridY = targetGridY;
        PixelX = GridX * TileSize;
        PixelY = GridY * TileSize;
        HopHeight = 0f;
        IsMoving = false;
        DiagonalStep = null;
        JustLanded = IsHoppingLedge;
        JustRodeOut = mounting;
        IsHoppingLedge = false;
        mounting = false;
        moveProgress = 0f;
        height = toHeight;
        standing = toHeight;
        Mode = modeAfter;
        // Off the water, Sharpedo goes home (plan 23, Alola's water ride)
        if (Mode != TravelMode.Surfing && RidePager.IsWaterRide(Steed)) SetJet(null);
        offX = offY = 0;
        return Landed(map, onWildEncounter, onWarpTrigger, onArrive);
    }

    /// <summary>
    /// What a tile entered does, once the player is counted on it: its warp, a trainer's eye (<paramref name="onArrive"/>),
    /// what carries them on, the grass and its wild Pokémon. The end of a step of the grid, and a tile entered by free
    /// movement (plan 30 · FM7, decision 7), alike. True if something took over.
    /// </summary>
    private bool Landed(Map map, Action<WildEncounterEntry>? onWildEncounter, Action<Warp>? onWarpTrigger, Func<bool>? onArrive)
    {
        var warp = map.GetWarpAt(GridX, GridY);
        if (warp != null && onWarpTrigger != null)
        {
            IsSliding = false;
            onWarpTrigger(warp);
            return true;
        }

        // A trainer's challenge comes before any wild Pokémon in the grass
        bool interrupted = onArrive != null && onArrive();

        // Ice carries the player on, unless this was a slip back down its slope, after which they stand; off the ice,
        // or stopped, the slide's speed is gone
        var underfoot = map.BehaviourAt(GridX, GridY);
        carried = interrupted || slipping ? null : CarriedBy(underfoot, movingDir);
        // A GBA spin (plan 19 · K5, part b): a slide on a map with stop tiles goes on over plain floor until one
        spinning = interrupted ? null : FieldMovement.Spins(map, underfoot, carried, spinning);
        carried ??= spinning;
        IsSliding = carried != null;
        slipping = false;
        if (carried == null || underfoot != TileBehavior.Ice) IceSpeed = 0;

        InTallGrass = map.IsTallGrass(GridX, GridY);
        if (InTallGrass) GrassRustleTimer = 0.2f;

        // Pokémon live in grass, in caves and in water; the water's are met only by someone surfing on it, and on the
        // sea floor in its seaweed, from the floor's own water table (Emerald's underwater encounters, plan 21 · H5)
        bool diving = Mode == TravelMode.Diving;
        bool onWater = Mode == TravelMode.Surfing || diving;
        bool where = diving ? underfoot is TileBehavior.Seaweed or TileBehavior.SeaweedNoSurfacing : onWater == TileBehaviors.IsSurfable(underfoot);
        // With wild Pokémon in sight (plan 10 · F5) the tall grass and the water roll nothing: what is seen there is what is met
        // (and in a place that shows its Pokémon on every floor, a hideaway, every step there: plan 38 · EX6)
        bool seenInstead = WildInSight && !diving && (onWater || InTallGrass || map.WildVisibleAt(GridX, GridY));
        // Mantine skims the water so lightly that nothing in it notices the rider (plan 23, Mantine Surf)
        if (Mode == TravelMode.Surfing && RidePager.Skims(Steed)) seenInstead = true;
        if (onWildEncounter != null && !interrupted && TileBehaviors.HasEncounters(underfoot) && where && !seenInstead)
        {
            bool thick = underfoot is TileBehavior.VeryTallGrass or TileBehavior.VeryTallDarkGrass || Mode == TravelMode.Cycling;
            var wild = map.RollWildEncounter(GridX, GridY, Encounters, onWater, thick, Lead, Moment?.Invoke());
            if (wild != null)
            {
                Encounters.Reset();
                carried = null;
                IsSliding = false;
                onWildEncounter(wild);
                return true;
            }
        }
        return false;
    }

    /// <summary>Keeps the player in place while something else plays out: the legs settle and the body turns to face.</summary>
    public void StandStill(float dt)
    {
        IsRunning = false;
        IsGliding = false;
        // Held by something else in a free region (a script, a trainer walking up): the body settles onto its tile's
        // centre, which is where every scene, sight line and walk of the grid expects it (plan 30 · FM8)
        if (!IsMoving && (offX != 0 || offY != 0) && Directions == MovementDirections.Free)
        {
            EaseToCentre(dt, GridX, GridY, FieldMovement.TilesPerSecond(Pace.Walk));
            Settle(dt, walking: true);
            return;
        }
        Settle(dt, walking: false);
    }

    /// <summary>Eases the walk in and out, and turns smoothly toward the facing direction.</summary>
    private void Settle(float dt, bool walking)
    {
        WalkBlend += ((walking ? 1f : 0f) - WalkBlend) * Math.Min(1f, dt * 12f);
        // (a talk's turn holds while the body stands as it was turned; a step or a turn lets it go)
        if (talkYaw is { } talk && (IsMoving || talk.Facing != Facing)) talkYaw = null;
        float turn = (talkYaw is { } held ? held.Yaw : freeYaw is { } free && free.Facing == Facing ? free.Yaw : YawOf(Facing)) - Yaw;
        turn = MathF.IEEERemainder(turn, MathF.Tau);
        Yaw += turn * Math.Min(1f, dt * 18f);

        if (GrassRustleTimer > 0f)
        {
            GrassRustleTimer -= dt;
        }
    }

    /// <summary>Takes a step in a direction if the field's rules allow one (<see cref="FieldMovement.Step"/>); a thud if not.</summary>
    private void TryStep(Direction dir, Map map)
    {
        var step = FieldMovement.Step(map, GridX, GridY, dir, WalkerOn(map));
        // With the skates on hand the step is a skater's, and where skates don't go a walker's (plan 23 · Z1)
        if (skating && Mode == TravelMode.OnFoot)
        {
            var skate = FieldMovement.Step(map, GridX, GridY, dir, WalkerOn(map) with { Mode = TravelMode.Skating, Running = false });
            // (the skates are on from the step's first frame, not from its end as a landing's mode is)
            if (skate.Moves) { step = skate; coasted = false; Mode = TravelMode.Skating; }
            else if (skate.Obstacle == Obstacle.Skates) IsRunning = HasRunningShoes;
        }
        else if (Mode == TravelMode.Skating)
        {
            coasted = false;
            if (!step.Moves && step.Obstacle == Obstacle.Skates)
            {
                Mode = TravelMode.OnFoot;
                IsRunning = HasRunningShoes;
                step = FieldMovement.Step(map, GridX, GridY, dir, WalkerOn(map));
            }
        }
        if (step.Moves)
        {
            // A step that pushes a gate of the Fortree Gym's turns it as it is taken (plan 21 · H6)
            if (step.Kind == StepKind.Walk && map.Puzzle != null)
            {
                var (dx, dy) = FieldMovement.Delta(dir);
                map.Puzzle.Crossed(map, GridX + dx, GridY + dy, dir);
            }
            Begin(map, step, dir);
            return;
        }

        // At the map's edge a warp underfoot is what happens next, not a thud
        if (step.Obstacle == Obstacle.MapEdge && map.GetWarpAt(GridX, GridY) != null) return;

        // A block of ice in the way slides off over the ice (plan 20 · J11)
        if (step.Obstacle == Obstacle.Person && PushIceBlock(map, dir)) return;

        // With Strength in force a boulder in the way is pushed on, if there is room beyond it (ov5_021DFF1C)
        if (step.Obstacle == Obstacle.Person && PushesBoulders && Mode == TravelMode.OnFoot)
        {
            var (dx, dy) = FieldMovement.Delta(dir);
            if (map.NpcIn(GridX + dx, GridY + dy, map.SurfaceAt(GridX + dx, GridY + dy, HeightOn(map)).Height) is { Obstacle: PropType.StrengthBoulder } boulder
                && FieldMovement.CanPush(map, boulder, dir))
            {
                boulder.GridX += dx;
                boulder.GridY += dy;
                pushed = (boulder, dir);
                PushedTiles = 1;
                pushing = FieldMovement.BoulderPushSeconds;
                return;
            }
        }
        // A ride runs into what stands in its way; the engine asks whether it charges through (plan 23, wave 91)
        if (step.Obstacle == Obstacle.Person && Mode == TravelMode.Riding)
        {
            var (dx, dy) = FieldMovement.Delta(dir);
            ranInto = (GridX + dx, GridY + dy);
        }
        Bump();
    }

    /// <summary>
    /// Takes a diagonal step where the field's rules allow one (<see cref="FieldMovement.StepDiagonal"/>, plan 30 · FM2).
    /// Refused, the player slides along what is in the way: the cardinal part they face first, then the other, and
    /// with neither open the part faced is tried as a cardinal step is (a bump, a boulder pushed, a block of ice).
    /// </summary>
    private void TryDiagonal(PokemonPlatinumEngine.Overworld.Heading heading, Map map)
    {
        // With the skates on hand a skater's diagonal first, as TryStep takes a skater's step (plan 23 · Z1)
        bool skate = skating && Mode is TravelMode.OnFoot or TravelMode.Skating;
        var walker = WalkerOn(map) with { Mode = skate ? TravelMode.Skating : Mode, Running = !skate && IsRunning };
        var step = FieldMovement.StepDiagonal(map, GridX, GridY, heading, walker);
        if (step.Moves)
        {
            if (skate) { Mode = TravelMode.Skating; coasted = false; IsRunning = false; }
            Begin(map, step, Facing, diagonal: heading);
            return;
        }
        var (vertical, horizontal) = heading.Parts();
        var faced = Facing == horizontal ? horizontal : vertical;
        var other = faced == vertical ? horizontal : vertical;
        if (!FieldMovement.Step(map, GridX, GridY, faced, WalkerOn(map)).Moves
            && FieldMovement.Step(map, GridX, GridY, other, WalkerOn(map)).Moves)
            faced = other;
        Facing = faced;
        TryStep(faced, map);
    }

    /// <summary>
    /// A frame of free movement steered by a stick (plan 30 · FM7, FM8; <see cref="FreeMovement"/>): the body turns to the
    /// stick's way, faces the cardinal nearest it, and moves its centre at the pace the ground, the way of travelling and
    /// the tilt give, the circle stopped against every edge the grid's own step refuses. An edge that is more than a plain
    /// walk (a ledge, a door, ice, a stair, water's edge, a boulder) pushed across within 35° of its normal is handed to the
    /// grid: the body eases onto its tile's centre and the grid's step is taken from there, as it is on the grid. A tile
    /// counted as entered runs everything a step does (<see cref="Landed"/>). True when the body pushes against something
    /// and doesn't move: it walks on the spot.
    /// </summary>
    private bool Glide(float dt, Map map, Vector2 stick, Action<WildEncounterEntry>? onWildEncounter, Action<Warp>? onWarpTrigger, Func<bool>? onArrive)
    {
        float tilt = Math.Min(1f, stick.Length());
        var way = stick / stick.Length();
        var face = FreeMovement.NearestCardinal(way);
        Facing = face;
        freeYaw = (FreeMovement.YawOf(way), face);
        talkYaw = null;
        if (float.IsNaN(standing)) standing = HeightOn(map);

        int sx = SubX, sy = SubY;
        int gx = FreeMovement.TileOf(sx), gy = FreeMovement.TileOf(sy);

        // On a tile the grid keeps (a stair, a ramp, the Cycling Road's pull: the body was handed there by the grid and
        // stands on its centre), the grid's own four ways step on from it
        if (!FreeMovement.Plain(map, gx, gy))
        {
            if ((gx, gy) != (GridX, GridY) || offX != 0 || offY != 0)
            {
                var (cx, cy) = TowardCentre(dt, gx, gy, FieldMovement.TilesPerSecond(Pace.Walk));
                MoveCentreTo(map, cx, cy, onWildEncounter, onWarpTrigger, onArrive);
                return false;
            }
            TryStep(face, map);
            return !IsMoving;
        }

        // A hand-off under way goes on while the stick still points across its edge
        if (handOff is { } going && Vector2.Dot(way, Unit(going)) < FreeMovement.HandOffCone) handOff = null;

        // The skates in a free place (plan 30's gate, plan 23 · Z1's rule): with the run held and the skates given they
        // are put on wherever the grid would skate the step ahead, and taken off where it wouldn't (tall grass, stairs),
        // as TryStep does a step at a time; let go, the idle branch takes them off as ever
        if (skating && Mode is TravelMode.OnFoot or TravelMode.Skating)
        {
            var skater = WalkerOn(map) with { Mode = TravelMode.Skating, Running = false, Height = standing };
            // (no skates in tall grass: the grid's step is a walker's all the way into it, so the body in it walks)
            bool refused = map.IsTallGrass(gx, gy) || FieldMovement.Step(map, gx, gy, face, skater).Obstacle == Obstacle.Skates;
            // ...and on either axis the stick pushes along, at an edge the body touches: a skater slanting into tall grass
            // takes the skates off and goes in on foot, as the grid's refused diagonal does, never sliding along it
            foreach (var (part, component, along) in new[]
                     {
                         (way.X < 0 ? Direction.Left : Direction.Right, MathF.Abs(way.X), sx),
                         (way.Y < 0 ? Direction.Up : Direction.Down, MathF.Abs(way.Y), sy)
                     })
            {
                if (refused || component < 0.2f) continue;
                int sign = part is Direction.Right or Direction.Down ? 1 : -1;
                int gap = (FreeMovement.CentreOf(FreeMovement.TileOf(along)) + sign * FreeMovement.Half - along) * sign - FreeMovement.Radius;
                if (gap <= FreeMovement.SubTile / 32) refused = FieldMovement.Step(map, gx, gy, part, skater).Obstacle == Obstacle.Skates;
            }
            if (!refused && Mode == TravelMode.OnFoot) { Mode = TravelMode.Skating; coasted = false; IsRunning = false; }
            else if (refused && Mode == TravelMode.Skating) { Mode = TravelMode.OnFoot; IsRunning = HasRunningShoes; }
        }

        var walker = WalkerOn(map) with { Height = standing };
        bool pushes = PushesBoulders && Mode == TravelMode.OnFoot;
        var pace = FreeMovement.PaceFor(tilt, AnalogSpeed, map.BehaviourAt(gx, gy), walker);
        float speed = FieldMovement.TilesPerSecond(pace);
        int dx = (int)MathF.Round(way.X * speed * dt * FreeMovement.SubTile);
        int dy = (int)MathF.Round(way.Y * speed * dt * FreeMovement.SubTile);

        if (handOff == null)
        {
            // A push across an edge the grid plays (a ledge, a door, a stair, ice, water's edge, a boulder)
            foreach (var (part, component, along, move) in new[]
                     {
                         (way.X < 0 ? Direction.Left : Direction.Right, MathF.Abs(way.X), sx, dx),
                         (way.Y < 0 ? Direction.Up : Direction.Down, MathF.Abs(way.Y), sy, dy)
                     })
            {
                if (component < FreeMovement.HandOffCone) continue;
                int sign = part is Direction.Right or Direction.Down ? 1 : -1;
                int tile = FreeMovement.TileOf(along);
                int gap = (FreeMovement.CentreOf(tile) + sign * FreeMovement.Half - along) * sign - FreeMovement.Radius;
                if (gap > Math.Abs(move) + 1) continue;
                if (FreeMovement.Classify(map, gx, gy, part, walker, pushes, out _) == FreeMovement.Edge.HandOff) handOff = part;
                break;
            }
        }

        if (handOff is { } handed)
        {
            if ((sx, sy) != (FreeMovement.CentreOf(gx), FreeMovement.CentreOf(gy)))
            {
                // Onto the tile's centre line first, at the pace walked (a hand-off's quarter of a second at most)
                var (cx, cy) = TowardCentre(dt, gx, gy, Math.Max(speed, FieldMovement.TilesPerSecond(Pace.Walk)));
                IsGliding = true;
                WalkCycle += speed * dt * 0.5f;
                if (MoveCentreTo(map, cx, cy, onWildEncounter, onWarpTrigger, onArrive)) return false;
                if ((cx, cy) != (FreeMovement.CentreOf(gx), FreeMovement.CentreOf(gy))) return false;
            }
            if (IsMoving || (GridX, GridY) != (gx, gy)) return false;
            // On the centre: the grid's step, exactly as on the grid (a hop, a door, a push, a bump)
            handOff = null;
            Facing = handed;
            freeYaw = null;
            TryStep(handed, map);
            return !IsMoving && pushing <= 0f;
        }

        var (nx, ny) = FreeMovement.Resolve(map, sx, sy, dx, dy, walker, pushes, ref standing);
        bool moved = (nx, ny) != (sx, sy);
        if (!moved)
        {
            Bump();
            return true;
        }
        IsGliding = true;
        WalkCycle += speed * dt * 0.5f;
        stride = speed;
        MoveCentreTo(map, nx, ny, onWildEncounter, onWarpTrigger, onArrive);
        return false;
    }

    /// <summary>A cardinal as a unit vector on the map (x right, y down).</summary>
    private static Vector2 Unit(Direction way)
    {
        var (x, y) = FieldMovement.Delta(way);
        return new Vector2(x, y);
    }

    /// <summary>
    /// Puts the free centre at (<paramref name="x"/>, <paramref name="y"/>) in 256ths, counts the tile it is now on
    /// (<see cref="FreeMovement.Counted"/>), and plays a tile entered as a step's end. True if something there took over
    /// (a trainer's eye, a wild Pokémon, a warp), which stops the body.
    /// </summary>
    private bool MoveCentreTo(Map map, int x, int y, Action<WildEncounterEntry>? onWildEncounter, Action<Warp>? onWarpTrigger, Func<bool>? onArrive)
    {
        int countedX = FreeMovement.Counted(GridX, x), countedY = FreeMovement.Counted(GridY, y);
        bool entered = (countedX, countedY) != (GridX, GridY);
        if (entered) leftTile = (GridX, GridY);
        GridX = targetGridX = countedX;
        GridY = targetGridY = countedY;
        offX = x - FreeMovement.CentreOf(GridX);
        offY = y - FreeMovement.CentreOf(GridY);
        PlaceFromSub();
        // The height underfoot eases to the ground the centre is over, as a step of the grid eases it
        height = float.IsNaN(height) ? standing : height + (standing - height) * 0.35f;
        if (!entered) return false;
        movingDir = Facing;
        slipping = false;
        JustLanded = false;
        JustRodeOut = false;
        if (Landed(map, onWildEncounter, onWarpTrigger, onArrive))
        {
            IsGliding = false;
            handOff = null;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Where the free centre comes to this frame, easing toward a tile's centre at a pace (plan 30 · FM8: before a
    /// hand-off, a script's walk or a step of the grid). The caller puts it there.
    /// </summary>
    private (int X, int Y) TowardCentre(float dt, int tx, int ty, float tilesPerSecond)
    {
        int x = SubX, y = SubY;
        int toX = FreeMovement.CentreOf(tx) - x, toY = FreeMovement.CentreOf(ty) - y;
        float reach = Math.Max(1f, tilesPerSecond * dt * FreeMovement.SubTile);
        float length = MathF.Sqrt((float)toX * toX + (float)toY * toY);
        if (length > reach)
        {
            toX = (int)MathF.Round(toX * reach / length);
            toY = (int)MathF.Round(toY * reach / length);
        }
        return (x + toX, y + toY);
    }

    /// <summary>
    /// Eases the free centre onto the centre of the tile counted as stood on (plan 30 · FM8), where nothing new is
    /// entered: a script's walk, a trainer walking up, a step of the grid about to be taken.
    /// </summary>
    private void EaseToCentre(float dt, int tx, int ty, float tilesPerSecond)
    {
        var (x, y) = TowardCentre(dt, tx, ty, tilesPerSecond);
        offX = x - FreeMovement.CentreOf(GridX);
        offY = y - FreeMovement.CentreOf(GridY);
        PlaceFromSub();
        WalkCycle += tilesPerSecond * dt * 0.5f;
    }

    /// <summary>
    /// The next step on ice (<see cref="IceSlide"/>): a snowball ahead breaks if the slide is fast enough, then the
    /// tile's tilt says whether the player slides on, faster or slower, slips back down, or stands.
    /// </summary>
    private void Slide(Map map, Direction onward)
    {
        if (IceSlide.Breaks(IceSpeed) && IceSlide.SnowballAhead(map, GridX, GridY, onward) is { } ball)
        {
            IceSlide.Break(map, ball);
            broken = ball;
        }
        var next = IceSlide.From(map, GridX, GridY, onward, IceSpeed, WalkerOn(map) with { Running = false });
        IceSpeed = next.Speed;
        switch (next.Outcome)
        {
            case IceSlide.Outcome.Slide:
                Begin(map, next.Step, onward, sliding: true, tilesPerSecond: IceSlide.TilesPerSecond(IceSpeed));
                break;
            case IceSlide.Outcome.SlipBack:
                // Back down the slope still facing up it
                Begin(map, next.Step, FieldMovement.Opposite(onward), sliding: true, tilesPerSecond: IceSlide.SlipTilesPerSecond);
                slipping = true;
                break;
            default:
                IsSliding = false;
                // A slide that ends against a block of ice sends the block on over the ice (plan 20 · J11)
                if (next.Step.Obstacle == Obstacle.Person) PushIceBlock(map, onward);
                break;
        }
    }

    private void Begin(Map map, FieldStep step, Direction dir, bool sliding = false, float? tilesPerSecond = null,
        PokemonPlatinumEngine.Overworld.Heading? diagonal = null)
    {
        int tiles = Math.Max(1, Math.Abs(step.X - GridX) + Math.Abs(step.Y - GridY));
        float speed = tilesPerSecond ?? FieldMovement.TilesPerSecond(sliding ? Pace.Fast : step.Pace);
        DiagonalStep = diagonal;
        // A step of the grid sets out from where the body stands: the tile's corner on the grid (the offset is nought
        // there), and wherever a free body was (plan 30 · FM7)
        startPixelX = (FreeMovement.CentreOf(GridX) + offX) / (float)(FreeMovement.SubTile / TileSize);
        startPixelY = (FreeMovement.CentreOf(GridY) + offY) / (float)(FreeMovement.SubTile / TileSize);
        offX = offY = 0;
        handOff = null;
        freeYaw = null;

        targetGridX = step.X;
        targetGridY = step.Y;
        fromHeight = HeightOn(map);
        toHeight = step.Height;
        modeAfter = step.Mode;
        movingDir = dir;
        stepKind = step.Kind;
        // A hop over a ledge takes as long as a step; a climb takes as long as the tiles it covers, and a jump from
        // a ramp, which flies, two thirds as long
        stepRate = step.Kind switch { StepKind.Climb => speed / tiles, StepKind.Jump => speed * 1.5f / tiles, _ => speed };
        // A diagonal crosses √2 of a tile at the same speed over the ground (plan 30 · FM2)
        if (diagonal != null) stepRate = speed / MathF.Sqrt(2f);
        stride = speed;
        hopArc = step.Kind switch { StepKind.Hop => 16f, StepKind.Land => 9f, StepKind.Jump => 8f + 6f * tiles, _ => 0f };
        IsHoppingLedge = hopArc > 0f;
        IsSliding = sliding;
        IsMoving = true;
        // A step stands them up from whatever a script set them in (plan 11 · C7)
        Posed = null;
        moveProgress = 0f;
        if (step.Kind is StepKind.Hop or StepKind.Jump) AudioManager.PlaySound("ledge");
        else if (Mode is not (TravelMode.Surfing or TravelMode.Diving) && SoundBank.StepSound(map.BehaviourAt(step.X, step.Y)) is { } footfall) AudioManager.PlaySound(footfall);
    }

    /// <summary>
    /// Sets out onto the water the player faces, on the back of a Pokémon that knows Surf: a hop from the shore
    /// onto it. False if there is no water there to surf on, or the player is busy.
    /// </summary>
    public bool StartSurf(Map map)
    {
        if (IsMoving || carried != null || !FieldMovement.CanStartSurf(map, GridX, GridY, Facing, WalkerOn(map))) return false;
        var (dx, dy) = FieldMovement.Delta(Facing);
        int nx = GridX + dx, ny = GridY + dy;
        Begin(map, new FieldStep(StepKind.Land, nx, ny, map.HeightAt(nx, ny), Pace.Walk, TravelMode.Surfing, Obstacle.None), Facing);
        mounting = true;
        return true;
    }

    /// <summary>
    /// Goes down under the water to the sea floor (plan 21 · H5): the player has been put on the floor's tile below,
    /// and swims there from now on. The dive's warp is the script's (<c>dive</c>); this only changes how they travel.
    /// </summary>
    public void StartDive()
    {
        Mode = TravelMode.Diving;
        height = float.NaN;
        carried = null;
        IsSliding = false;
    }

    /// <summary>
    /// Comes back up from the sea floor onto the sea above, on the back of the Pokémon whose Dive it was (plan 21 · H5):
    /// the player has been put on the water's tile above, and surfs from now on.
    /// </summary>
    public void Surface()
    {
        Mode = TravelMode.Surfing;
        height = float.NaN;
        carried = null;
        IsSliding = false;
    }

    /// <summary>
    /// Climbs the waterfall or the rock face the player faces, up or down, to the far side of it: what Waterfall
    /// and Rock Climb do once the player has said yes (plan 02 · S2). False when there is nothing to climb from here.
    /// </summary>
    public bool Climb(Map map)
    {
        if (IsMoving || carried != null || pushing > 0f) return false;
        var step = FieldMovement.Step(map, GridX, GridY, Facing, WalkerOn(map) with { Climbing = true });
        if (step.Kind != StepKind.Climb) return false;
        Begin(map, step, Facing);
        return true;
    }

    /// <summary>Gets on or off the Bicycle. False while on the water or in the middle of a step.</summary>
    public bool SetCycling(bool cycling)
    {
        if (IsMoving || Mode is TravelMode.Surfing or TravelMode.Diving) return false;
        Mode = cycling ? TravelMode.Cycling : TravelMode.OnFoot;
        Steed = null;
        return true;
    }

    /// <summary>
    /// The Ride Pager's Pokémon the player sits on while <see cref="TravelMode.Riding"/> (plan 23, wave 91), by its
    /// species; null otherwise. Drawn under the player on its own field sprite, as a surfer's <see cref="Carrier"/> is.
    /// </summary>
    public string? Steed { get; private set; }

    /// <summary>
    /// Gets on a ride on land (<paramref name="species"/>) or off it (null). False while on the water, on the Bicycle or
    /// in the middle of a step; whoever asks has checked that the ride is the pager's and the place allows it.
    /// </summary>
    public bool SetRiding(string? species)
    {
        if (IsMoving || Mode is TravelMode.Surfing or TravelMode.Diving or TravelMode.Cycling) return false;
        if (species == null && Mode != TravelMode.Riding) return false;
        Mode = species == null ? TravelMode.OnFoot : TravelMode.Riding;
        Steed = species;
        IsRunning = false;
        return true;
    }

    /// <summary>
    /// Calls a ride over the water (<paramref name="species"/>: Sharpedo, plan 23, Alola's water ride) under a surfer, or
    /// sends it home (null): the player goes on surfing, now on its back at a bicycle's top speed, and a rock in the sea
    /// that only it rams is broken by the rock's own script. False off the water, mid-step, or with nothing to send home.
    /// </summary>
    public bool SetJet(string? species)
    {
        if (species == null)
        {
            if (!RidePager.IsWaterRide(Steed)) return false;
            Steed = null;
            Carrier = carrierBeforeJet;
            return true;
        }
        if (IsMoving || Mode != TravelMode.Surfing) return false;
        if (!RidePager.IsWaterRide(Steed)) carrierBeforeJet = Carrier;
        Steed = species;
        Carrier = species;
        return true;
    }

    private string? carrierBeforeJet;

    /// <summary>
    /// The tile a ride ran into and was stopped by something standing there (plan 23, wave 91: Tauros's charge into a
    /// cracked rock), once; null when nothing was run into since it was last taken.
    /// </summary>
    public (int X, int Y)? TakeRanInto()
    {
        var tile = ranInto;
        ranInto = null;
        return tile;
    }

    private (int X, int Y)? ranInto;

    /// <summary>
    /// Collisions on a bicycle, counted for good (plan 21 · H12, part k: the Seaside Cycling Road's run counts those since
    /// it began, Emerald's <c>gBikeCollisions</c>).
    /// </summary>
    public int Bumps { get; private set; }

    private void Bump()
    {
        if (bumpCooldown > 0f) return;
        if (Mode == TravelMode.Cycling) Bumps++;
        AudioManager.PlaySound("bump");
        bumpCooldown = BumpInterval;
    }
}
