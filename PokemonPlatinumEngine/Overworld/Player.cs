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

    private float moveProgress = 0f;
    private int targetGridX = 0;
    private int targetGridY = 0;

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
    public (int X, int Y) Heading => IsMoving ? (targetGridX, targetGridY) : (GridX, GridY);

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

    /// <summary>Ends any gesture and lets the face go: the place has been come to again.</summary>
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
        Yaw = YawOf(facing);
        WalkBlend = 0f;
        Encounters.Reset();
    }

    /// <summary>Puts the player on foot, on the water or on the Bicycle outright: a loaded save, a test, the harness.</summary>
    public void SetMode(TravelMode mode) => Mode = mode;

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
        carried = FieldMovement.Carries(underfoot, Facing);
        IsSliding = carried != null;
    }

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

    private Walker WalkerOn(Map map) => new(Mode, HeightOn(map), IsRunning, FastGear, Moves, Bike: Bike);

    /// <summary>One frame of the player's movement, steered by the keys held.</summary>
    public void Update(float dt, Map map, Action<WildEncounterEntry> onWildEncounter, Action<Warp> onWarpTrigger, Func<bool>? onArrive = null)
    {
        Vector2 dir = InputManager.GetMovementVector();
        Direction? want = null;
        if (dir != Vector2.Zero)
        {
            want = Math.Abs(dir.X) > Math.Abs(dir.Y)
                ? (dir.X > 0 ? Direction.Right : Direction.Left)
                : (dir.Y > 0 ? Direction.Down : Direction.Up);
        }
        Advance(dt, map, want, InputManager.IsActionDown(GameAction.Run), onWildEncounter, onWarpTrigger, onArrive);
    }

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
    {
        if (bumpCooldown > 0f) bumpCooldown -= dt;
        bool walkingInPlace = false;

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
                float startX = GridX * TileSize;
                float startY = GridY * TileSize;
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
        else
        {
            // Idle state: follow the steering
            IsSliding = false;
            IsRunning = Mode == TravelMode.OnFoot && run && HasRunningShoes;

            if (want is { } desiredDir)
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

        Settle(dt, (IsMoving && !IsSliding && Mode == TravelMode.OnFoot) || walkingInPlace);
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
        JustLanded = IsHoppingLedge;
        JustRodeOut = mounting;
        IsHoppingLedge = false;
        mounting = false;
        moveProgress = 0f;
        height = toHeight;
        Mode = modeAfter;

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
        carried = interrupted || slipping ? null : FieldMovement.Carries(underfoot, movingDir);
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
        if (onWildEncounter != null && !interrupted && TileBehaviors.HasEncounters(underfoot) && where)
        {
            bool thick = underfoot == TileBehavior.VeryTallGrass || Mode == TravelMode.Cycling;
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
        Settle(dt, walking: false);
    }

    /// <summary>Eases the walk in and out, and turns smoothly toward the facing direction.</summary>
    private void Settle(float dt, bool walking)
    {
        WalkBlend += ((walking ? 1f : 0f) - WalkBlend) * Math.Min(1f, dt * 12f);
        float turn = YawOf(Facing) - Yaw;
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
        Bump();
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

    private void Begin(Map map, FieldStep step, Direction dir, bool sliding = false, float? tilesPerSecond = null)
    {
        int tiles = Math.Max(1, Math.Abs(step.X - GridX) + Math.Abs(step.Y - GridY));
        float speed = tilesPerSecond ?? FieldMovement.TilesPerSecond(sliding ? Pace.Fast : step.Pace);

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
        stride = speed;
        hopArc = step.Kind switch { StepKind.Hop => 16f, StepKind.Land => 9f, StepKind.Jump => 8f + 6f * tiles, _ => 0f };
        IsHoppingLedge = hopArc > 0f;
        IsSliding = sliding;
        IsMoving = true;
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
        return true;
    }

    private void Bump()
    {
        if (bumpCooldown > 0f) return;
        AudioManager.PlaySound("bump");
        bumpCooldown = BumpInterval;
    }
}
