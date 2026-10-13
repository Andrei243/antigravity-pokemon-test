using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Models;
using PokemonPlatinumEngine.Models.Underground;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;
using PokemonPlatinumEngine.UI;
using PokemonPlatinumEngine.UI.Kit;
using Raylib_cs;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Underground in the game (plan 06 · R16a): the way down with the Explorer Kit and the way up, what the tunnels
/// hold laid out as the player arrives, the wall ahead dug at, a Sphere dug up or buried, a trap set, taken up or
/// stepped on and what it does to the player, the radar's pulse, the hikers' trade, the secret base with its goods
/// stood about it and its PC, the other diggers' bases and their flags, and the screens for each. The rules are
/// <c>Models/Underground</c>'s; this is where they meet the map, the keys and the save.
/// </summary>
public partial class GameEngine
{
    public const string UndergroundMap = "Underground";

    /// <summary>The flags the Underground Man's missions wait for (the original's, each set where the original sets it).</summary>
    public const string EnteredUndergroundFlag = "FLAG_ENTERED_UNDERGROUND", DugForFossilsFlag = "FLAG_DIGGING_FOR_FOSSILS",
        SphereBuriedFlag = "FLAG_SPHERE_ACQUIRED", CreatedBaseFlag = "FLAG_CREATED_SECRET_BASE", DecoratedBaseFlag = "FLAG_DECORATED_SECRET_BASE",
        DeliveredFlagFlag = "FLAG_DELIVERED_STOLEN_FLAG";

    /// <summary>Those flags together: the game sets them as the player does each thing below ground, no script does.</summary>
    public static readonly string[] UndergroundMissionFlags =
        { EnteredUndergroundFlag, DugForFossilsFlag, SphereBuriedFlag, CreatedBaseFlag, DecoratedBaseFlag, DeliveredFlagFlag };

    /// <summary>The scripts of the Underground's file the game starts itself: a wall faced, the two kinds of PC, the bag's way up.</summary>
    public static readonly string[] UndergroundScripts = { "Wall", "BasePC", "DiggerPC", "GoUp" };

    private UndergroundState underground = new();
    private UndergroundField? tunnels;
    private readonly MiningScreen miningScreen = new();
    private readonly UndergroundScreen undergroundScreen = new();
    private readonly DecorationScreen decorationScreen = new();

    /// <summary>The trap that has the player, and the frames of it still owed at the console's thirty a second.</summary>
    private TrapEffect? trap;
    private float trapFrames;
    private bool trapTap;
    private readonly HeldKey smokeX = new(), smokeY = new();

    /// <summary>The day's other diggers, and whose cavern the base's room shows now: null for the player's own.</summary>
    private List<Digger> diggers = new();
    private int? visiting { get => underground.Visiting; set => underground.Visiting = value; }

    /// <summary>The tiles of the tunnels' rock that are ways into a base now, each with the height the rock had.</summary>
    private readonly Dictionary<(int X, int Z), (float Height, int? Digger)> baseEntrances = new();

    /// <summary>The tiles of the map nobody walks through: its rock, and a base's way in, which is rock to everything but the feet.</summary>
    private bool UndergroundRock(int x, int z)
    {
        var map = MapDatabase.Get(UndergroundMap);
        return !map.InBounds(x, z) || baseEntrances.ContainsKey((x, z)) || map.IsSolid(x, z);
    }

    private UndergroundField Tunnels => tunnels ??= new UndergroundField(underground, UndergroundRock);

    private bool IsUnderground => flow.Map?.Name == UndergroundMap;
    private bool InTheBase => IsUnderground && flow.Player.GridX < SecretBaseRules.Size && flow.Player.GridY < SecretBaseRules.Size;

    /// <summary>The cavern the base's room shows: the player's own, or the digger's being visited.</summary>
    private SavedBase? RoomShown => visiting is { } n ? diggers.FirstOrDefault(d => d.Number == n)?.Base : underground.Base;

    private void LoadUnderground(UndergroundState? saved)
    {
        // The maps outlast a game: the ways into the last game's bases are rock again before another begins
        if (baseEntrances.Count > 0)
        {
            var map = MapDatabase.Get(UndergroundMap);
            foreach (var (at, was) in baseEntrances)
            {
                map.SetGroundTile(at.X, at.Z, TileType.CaveWall, isSolid: true);
                map.SetHeight(at.X, at.Z, was.Height);
                world?.Forget(map, at.X, at.Z, 1, 1);
            }
        }
        underground = saved ?? UndergroundState.NewGame(encounters.DailyNumber);
        tunnels = null;
        trap = null;
        baseEntrances.Clear();
    }

    // ------------------------------------------------------------------ down and up

    /// <summary>
    /// Why the Explorer Kit can't be used where the player stands (<c>CanUseExplorerKit</c>): only on the map of the
    /// region itself, on foot, off the Cycling Road and the bridges, not in a Safari Game, and not on ground that
    /// forbids it. Null when it can.
    /// </summary>
    private string? WhyNotGoUnderground()
    {
        int x = flow.Player.GridX, y = flow.Player.GridY;
        const string no = "The Explorer Kit can't be used here.";
        if (!flow.Map.IsStreamed || flow.Map.Name != "Sinnoh") return no;
        if (flow.Story.Has(BicycleRules.OnCyclingRoadFlag) || safari.Active) return no;
        if (flow.Player.Mode == TravelMode.Surfing) return no;
        if (flow.Map.DeckAt(x, y) != null || flow.Map.BehaviourAt(x, y) == TileBehavior.NoExplorerKit) return no;
        return null;
    }

    /// <summary>The Explorer Kit used: down into the tunnels above ground, the Underground's own bag below.</summary>
    private void UseExplorerKit()
    {
        if (IsUnderground)
        {
            OpenUndergroundBag();
            return;
        }
        if (WhyNotGoUnderground() is { } why)
        {
            ShowNotification(why);
            AudioManager.PlaySound("error");
            return;
        }
        GoUnderground();
    }

    /// <summary>
    /// Down through the ground where the player stands, to the tile of the tunnels under it
    /// (<c>FieldTask_MapChangeToUnderground</c>): the place above is kept to come up at.
    /// </summary>
    private void GoUnderground()
    {
        underground.Surface = new SurfaceSpot { Map = flow.Map.Name, X = flow.Player.GridX, Y = flow.Player.GridY, Facing = flow.Player.Facing };
        var (x, z) = UndergroundField.Below(flow.Player.GridX, flow.Player.GridY);
        if (flow.Player.Mode == TravelMode.Cycling) flow.Player.SetCycling(false);
        var below = MapDatabase.Get(UndergroundMap);
        AudioManager.PlaySound("stairs");
        PlayAreaMusic(below, x, z);
        StartTransition(GameState.Overworld, () =>
        {
            flow.Map = below;
            flow.Player.SetPosition(x, z, Direction.Down);
            visiting = null;
            flow.Story.Set(EnteredUndergroundFlag);
            trainerScore = TrainerScore.Add(trainerScore, TrainerScore.WentUnderground);
            ArriveOnMap();
            AnnounceLocation();
            world.Life.Clear();
        });
    }

    /// <summary>Back up to where the player went down (<c>FieldTask_MapChangeFromUnderground</c>). A flag being carried is left behind.</summary>
    private void ComeUp()
    {
        if (!IsUnderground || underground.Surface is not { } spot) return;
        underground.HeldFlag = null;
        trap = null;
        var above = MapDatabase.Get(spot.Map);
        AudioManager.PlaySound("stairs");
        PlayAreaMusic(above, spot.X, spot.Y);
        StartTransition(GameState.Overworld, () =>
        {
            flow.Map = above;
            flow.Player.SetPosition(spot.X, spot.Y, spot.Facing);
            ArriveOnMap();
            AnnounceLocation();
            world.Life.Clear();
        });
    }

    /// <summary>
    /// The tunnels as the player comes to them (the original's <c>MiningEnv_Init</c> and the map's own script): the
    /// day's walls and traps spread if a new day asks (the hikers stand by the overlay), the day's other diggers, the ways
    /// into the bases, and the room of whichever base is shown.
    /// </summary>
    private void ArriveUnderground()
    {
        var map = flow.Map;
        Tunnels.SpreadIfDue();
        diggers = Diggers.Today(underground.Seed, UndergroundRock);
        LayOutEntrances();
        LayOutBase();
        // Roark is beside whoever comes down for the first time (Underground_PutRoarkNextToPlayer)
        if (flow.Story.Var("VAR_HAS_SEEN_UNDERGROUND_ROARK_INTRO") == 0 && map.Everyone.FirstOrDefault(n => n.Key == "roark") is { } roark)
        {
            int side = !map.IsSolid(flow.Player.GridX + 1, flow.Player.GridY) ? 1 : -1;
            (roark.GridX, roark.GridY) = (flow.Player.GridX + side, flow.Player.GridY);
            roark.Facing = Direction.Down;
        }
    }

    /// <summary>
    /// A way into a base is a hollow in the rock, like a cave's mouth: the tile is opened at the floor's height while
    /// a base lies behind it, and is rock again once the base has moved.
    /// </summary>
    private void LayOutEntrances()
    {
        var map = MapDatabase.Get(UndergroundMap);
        var wanted = new Dictionary<(int, int), int?>();
        if (underground.Base is { } own) wanted[(own.EntranceX, own.EntranceZ)] = null;
        foreach (var digger in diggers) wanted.TryAdd((digger.Base.EntranceX, digger.Base.EntranceZ), digger.Number);
        foreach (var (at, was) in baseEntrances.ToList())
        {
            if (wanted.ContainsKey(at)) continue;
            map.SetGroundTile(at.X, at.Z, TileType.CaveWall, isSolid: true);
            map.SetHeight(at.X, at.Z, was.Height);
            baseEntrances.Remove(at);
            world?.Forget(map, at.X, at.Z, 1, 1);
        }
        foreach (var (at, digger) in wanted)
        {
            if (baseEntrances.ContainsKey(at))
            {
                baseEntrances[at] = (baseEntrances[at].Height, digger);
                continue;
            }
            if (!map.InBounds(at.Item1, at.Item2)) continue;
            // The floor it opens onto: the open tile beside it
            float floor = new[] { (0, 1), (0, -1), (1, 0), (-1, 0) }.Where(d => map.InBounds(at.Item1 + d.Item1, at.Item2 + d.Item2) && !map.IsSolid(at.Item1 + d.Item1, at.Item2 + d.Item2))
                .Select(d => map.HeightAt(at.Item1 + d.Item1, at.Item2 + d.Item2)).DefaultIfEmpty(map.GroundLevel).First();
            baseEntrances[at] = (map.HeightAt(at.Item1, at.Item2), digger);
            map.SetGroundTile(at.Item1, at.Item2, TileType.CaveMouth, isSolid: false);
            map.SetHeight(at.Item1, at.Item2, floor);
            world?.Forget(map, at.Item1, at.Item2, 1, 1);
        }
    }

    /// <summary>The prop a good, a boulder or the PC is on the map: its tiles, and the model's name that says which it is.</summary>
    private static Prop BaseProp(string model, int x, int z, int width, int depth) =>
        new() { Type = PropType.Table, X = x, Y = z, Width = width, Depth = depth, Model = UndergroundArt.ModelPrefix + model };

    /// <summary>
    /// The base's room laid out for the cavern shown: its goods, boulders and PC stood on the floor as props, and the
    /// tiles they block shut (<c>SecretBases_CalculateBaseCollision</c>). With no base, the room is bare.
    /// </summary>
    private void LayOutBase()
    {
        var map = MapDatabase.Get(UndergroundMap);
        map.Props.RemoveAll(p => p.Model.StartsWith(UndergroundArt.ModelPrefix, StringComparison.Ordinal));
        var room = RoomShown;
        var blocked = room != null ? SecretBaseRules.BlockedTiles(room) : new HashSet<(int X, int Z)>();
        for (int z = SecretBaseRules.FloorTop; z <= SecretBaseRules.FloorBottom; z++)
            for (int x = SecretBaseRules.FloorLeft; x <= SecretBaseRules.FloorRight; x++)
                map.SetGroundTile(x, z, TileType.CaveFloor, blocked.Contains((x, z)));
        if (room != null)
        {
            int flags = visiting != null ? 0 : underground.Records.FlagsCaptured;
            map.Props.Add(BaseProp($"pc:{SecretBaseRules.FlagFor(flags)}", SecretBaseRules.PcX, SecretBaseRules.PcZ, 2, 1));
            foreach (var boulder in room.Boulders) map.Props.Add(BaseProp("boulder", boulder.X, boulder.Z, 2, 2));
            foreach (var good in room.Goods)
                if (UndergroundDatabase.Good(good.Good) is { } record) map.Props.Add(BaseProp($"good:{good.Good}", good.X, good.Z, record.Width, record.Depth));
        }
        world?.Forget(map, 0, 0, SecretBaseRules.Size, SecretBaseRules.Size);
    }

    // ------------------------------------------------------------------ steps

    /// <summary>
    /// A step taken below ground: into a base by its way in, out of it by its mat, onto a tool of the base or a trap
    /// of the tunnels. True when the step started something.
    /// </summary>
    private bool UndergroundStep()
    {
        int x = flow.Player.GridX, z = flow.Player.GridY;
        underground.Step();
        if (underground.Steps == 0) trainerScore = TrainerScore.Add(trainerScore, TrainerScore.UndergroundSteps);
        if (baseEntrances.TryGetValue((x, z), out var way))
        {
            EnterBase(way.Digger);
            return true;
        }
        if (InTheBase)
        {
            if (x == SecretBaseRules.ExitX && z >= SecretBaseRules.ExitZ)
            {
                LeaveBase();
                return true;
            }
            // A tool of the base goes off under whoever steps on it, and is still there afterwards (SecretBases_CheckPlayerTriggeredTool)
            if (RoomShown is { } room && SecretBaseRules.GoodStandingAt(room, x, z) is { } tool && SecretBaseRules.ToolTrap(tool.Good) is > 0 and var toolTrap)
            {
                SpringTrap(toolTrap);
                return true;
            }
            return false;
        }
        if (trap == null && Tunnels.StepOn(x, z) is > 0 and var stepped)
        {
            SpringTrap(stepped);
            return true;
        }
        return false;
    }

    private void SpringTrap(int which)
    {
        trap = new TrapEffect(which, fieldRandom);
        trapFrames = 0f;
        trapTap = false;
        flow.Player.ShowBubble(EmoteBubble.Exclaim);
        AudioManager.PlaySound("exclaim");
        ShowNotification($"A trap! It's a {UndergroundDatabase.TrapName(which)}!");
        if (trap.Done) trap = null;
    }

    /// <summary>Into a base by its way in (<c>SecretBases_TryEnterBase</c>): the room is laid out for whoever's it is, behind the fade.</summary>
    private void EnterBase(int? digger)
    {
        AudioManager.PlaySound("stairs");
        StartTransition(GameState.Overworld, () =>
        {
            visiting = digger;
            flow.Player.SetPosition(SecretBaseRules.ExitX, SecretBaseRules.FloorBottom, Direction.Up);
            LayOutBase();
            world.Life.Clear();
            ShowNotification(digger is { } n && diggers.FirstOrDefault(d => d.Number == n) is { } owner ? $"{owner.Name}'s secret base" : "Your secret base");
        });
    }

    /// <summary>Out of the base onto the floor before its way in (<c>SecretBases_ExitBase</c>).</summary>
    private void LeaveBase()
    {
        if (RoomShown is not { } room)
        {
            // A base that is no more (it was moved from inside): out where the player went down
            var (x, z) = underground.Surface is { } spot ? UndergroundField.Below(spot.X, spot.Y) : (40, 104);
            room = new SavedBase { EntranceX = x, EntranceZ = z - 1, EntranceFacing = Direction.Down };
        }
        var (dx, dz) = FieldMovement.Delta(room.EntranceFacing);
        int outX = room.EntranceX + dx, outZ = room.EntranceZ + dz;
        var facing = room.EntranceFacing;
        AudioManager.PlaySound("stairs");
        StartTransition(GameState.Overworld, () =>
        {
            visiting = null;
            flow.Player.SetPosition(outX, outZ, facing);
            LayOutBase();
            world.Life.Clear();
        });
    }

    // ------------------------------------------------------------------ the tile ahead

    private (int X, int Z) AheadBelow()
    {
        var (dx, dz) = FieldMovement.Delta(flow.Player.Facing);
        return (flow.Player.GridX + dx, flow.Player.GridY + dz);
    }

    /// <summary>
    /// The button pressed facing a tile of the Underground: the base's PC or a good with something written on it; in
    /// the tunnels a wall that holds something, a trap seen in the floor, a buried Sphere. Whoever carries a flag has
    /// no hand free for the last three (<c>UndergroundPlayer_BuriedObjectHeldFlagCheck</c>).
    /// </summary>
    private bool TryInteractUnderground(int x, int z)
    {
        if (!IsUnderground) return false;
        if (InTheBase)
        {
            if (RoomShown is not { } room) return false;
            if (z == SecretBaseRules.PcZ && x is SecretBaseRules.PcX or SecretBaseRules.PcX + 1)
                return StartScript(visiting == null ? "BasePC" : "DiggerPC", file: "underground");
            if (SecretBaseRules.GoodBlocking(room, x, z) is { } good && SecretBaseRules.RecordLine(good.Good, underground.Records) is { } line)
            {
                flow.Dialogue.ShowDialogue("", new[] { line });
                flow.State = GameState.Dialogue;
                return true;
            }
            return false;
        }
        bool wall = Tunnels.MiningSpotAt(x, z) != null, floorTrap = Tunnels.TrapAt(x, z) != null, sphere = Tunnels.SphereAt(x, z) != null;
        if (!wall && !floorTrap && !sphere) return false;
        if (underground.HeldFlag != null)
        {
            ShowNotification("Not with a flag in your hands!");
            AudioManager.PlaySound("error");
            return true;
        }
        if (wall) return StartScript("Wall", file: "underground");
        if (floorTrap)
        {
            int taken = Tunnels.Disarm(x, z);
            ShowNotification(taken > 0 ? $"You found a {UndergroundDatabase.TrapName(taken)} and took it up!" : "A trap! But you have no room for it.");
            AudioManager.PlaySound(taken > 0 ? "select" : "error");
            return true;
        }
        var dug = Tunnels.DigUp(x, z);
        if (dug == null) ShowNotification("There's no room for another Sphere.");
        else
        {
            int grew = Math.Min(dug.Growth, UndergroundState.MaxSphereSize - dug.Size);
            ShowNotification($"You dug up the {UndergroundDatabase.SphereName(dug.Kind)}: size {dug.Worth}." + (grew > 0 ? $" It had grown by {grew}!" : ""));
        }
        AudioManager.PlaySound(dug != null ? "select" : "error");
        return true;
    }

    /// <summary>
    /// The radar's pulse round the player (the original's touch on the lower screen, <c>UndergroundMan_StartTouchRadar</c>):
    /// the walls that hold something glint gold, the traps red and the buried Spheres blue, for a moment.
    /// </summary>
    private void PulseRadar()
    {
        var pulse = Tunnels.Pulse(flow.Player.GridX, flow.Player.GridY);
        AudioManager.PlaySound("poketch_beep");
        foreach (var at in pulse.Walls) world.Life.Glint(flow.Map, at.X, at.Z, new Color(255, 232, 130, 255));
        foreach (var at in pulse.Traps) world.Life.Glint(flow.Map, at.X, at.Z, new Color(255, 110, 96, 255));
        foreach (var at in pulse.Spheres) world.Life.Glint(flow.Map, at.X, at.Z, new Color(130, 200, 255, 255));
        if (pulse.Walls.Count + pulse.Traps.Count + pulse.Spheres.Count == 0) ShowNotification("Nothing answers nearby...");
    }

    // ------------------------------------------------------------------ a trap at work

    private bool BlockedAhead(Direction way)
    {
        var (dx, dz) = FieldMovement.Delta(way);
        int x = flow.Player.GridX + dx, z = flow.Player.GridY + dz;
        return !flow.Map.InBounds(x, z) || flow.Map.IsSolid(x, z) || flow.Map.GetNpcAt(x, z) != null || baseEntrances.ContainsKey((x, z));
    }

    private static Direction? WantedWay()
    {
        var dir = InputManager.GetMovementVector();
        if (dir == System.Numerics.Vector2.Zero) return null;
        return Math.Abs(dir.X) > Math.Abs(dir.Y) ? (dir.X > 0 ? Direction.Right : Direction.Left) : (dir.Y > 0 ? Direction.Down : Direction.Up);
    }

    /// <summary>What the trap that has the player is to be told this frame, when a tool drives it instead of the keys.</summary>
    public (Direction? Way, bool Tap, bool Held)? TrapKeys { get; set; }

    /// <summary>
    /// A frame of the trap that has the player. A slide carries them, tile by tile; wrong steering lets them walk
    /// where it sends them; everything else holds them where they stand until they are free of it.
    /// </summary>
    private void UpdateTrap(float dt)
    {
        var t = trap!;
        var keys = TrapKeys;
        var way = keys != null ? keys.Value.Way : WantedWay();
        bool tap = keys?.Tap ?? InputManager.IsActionPressed(GameAction.Confirm), held = keys?.Held ?? InputManager.IsActionDown(GameAction.Confirm);
        switch (t.Family)
        {
            case TrapFamily.Slide:
                if (!flow.Player.IsMoving && !t.Slide(BlockedAhead(t.Way)))
                {
                    trap = null;
                    return;
                }
                flow.Player.Advance(dt, flow.Map, t.Way, true, null, null, () => true);
                return;
            case TrapFamily.Steering:
                flow.Player.Advance(dt, flow.Map, way is { } pressed ? t.Steer(pressed) : null, false, null, null, () =>
                {
                    t.Stepped();
                    if (t.Done)
                    {
                        trap = null;
                        ShowNotification("Your feet go where you tell them again.");
                    }
                    return OnStep();
                });
                return;
        }
        flow.Player.StandStill(dt);
        // A hole is climbed out of by turning: a way pressed that the player isn't facing (CommPlayer_Direction against the facing)
        bool turned = false;
        if (t.Family == TrapFamily.Hole && way is { } turn && turn != flow.Player.Facing)
        {
            flow.Player.Facing = turn;
            turned = true;
            AudioManager.PlaySound("cursor");
            t.Advance(turned: true);
        }
        trapTap |= tap;
        int dx = 0, dy = 0;
        if (t.Family == TrapFamily.Smoke)
        {
            int ax = way == Direction.Left ? -1 : way == Direction.Right ? 1 : 0, ay = way == Direction.Up ? -1 : way == Direction.Down ? 1 : 0;
            dx = keys != null ? ax : smokeX.Advance(dt, InputManager.Axis(GameAction.Left, GameAction.Right), InputManager.Axis(GameAction.Left, GameAction.Right, held: true));
            dy = keys != null ? ay : smokeY.Advance(dt, InputManager.Axis(GameAction.Up, GameAction.Down), InputManager.Axis(GameAction.Up, GameAction.Down, held: true));
        }
        trapFrames += dt * TrapEffect.FramesPerSecond;
        bool first = true;
        while (trapFrames >= 1f && !t.Done)
        {
            trapFrames -= 1f;
            if (trapTap && t.Family is TrapFamily.Rock or TrapFamily.Bubbles) AudioManager.PlaySound(t.Family == TrapFamily.Rock ? "bump" : "cursor");
            t.Advance(turned: false, tap: trapTap, held: held, dx: first ? dx * 2 : 0, dy: first ? dy * 2 : 0);
            trapTap = false;
            first = false;
        }
        if (dx != 0 || dy != 0) trapFrames = Math.Max(trapFrames, 0f);
        if (!t.Done) return;
        trap = null;
        AudioManager.PlaySound("select");
        if (t.Family != TrapFamily.Alert) ShowNotification("You got free of the trap!");
    }

    private void DrawTrap()
    {
        if (trap != null && flow.State == GameState.Overworld) ModernUi.DrawTrap(VirtualWidth, VirtualHeight, trap, (float)FrameClock.Now);
    }

    // ------------------------------------------------------------------ the screens

    /// <summary>The bag below ground, from the Explorer Kit: its lists, and what is done in the tunnels when it closes.</summary>
    private void OpenUndergroundBag(int tab = 0)
    {
        if (trap != null) return;
        flow.State = GameState.Underground;
        undergroundScreen.OpenBag(underground, item =>
        {
            if (ItemDatabase.Get(item) is not { } data) return false;
            flow.Bag.AddItem(data, 1);
            return true;
        }, tab);
    }

    private void UpdateUndergroundScreen(float dt)
    {
        undergroundScreen.Update(dt);
        if (undergroundScreen.IsActive) return;
        flow.State = GameState.Overworld;
        var asked = undergroundScreen.TakeRequest();
        var (x, z) = AheadBelow();
        bool someone = flow.Map.GetNpcAt(x, z) != null;
        switch (asked.Kind)
        {
            case UndergroundRequestKind.GoUp:
                StartScript("GoUp", file: "underground");
                break;
            case UndergroundRequestKind.Decorate:
                OpenDecoration();
                break;
            case UndergroundRequestKind.BurySphere:
            {
                var sphere = asked.Slot >= 0 && asked.Slot < underground.Spheres.Count ? underground.Spheres[asked.Slot] : null;
                var result = Tunnels.Bury(asked.Slot, x, z, someone);
                if (result == BuryResult.Done)
                {
                    // SystemFlag_SetSphereAcquired: the Underground Man's third mission
                    flow.Story.Set(SphereBuriedFlag);
                    world.Life.Glint(flow.Map, x, z, new Color(130, 200, 255, 255));
                    AudioManager.PlaySound("select");
                    ShowNotification($"The {UndergroundDatabase.SphereName(sphere!.Kind)} of size {sphere.Size} was buried.");
                }
                else Refuse(result, "buried");
                break;
            }
            case UndergroundRequestKind.SetTrap:
            {
                int which = asked.Slot >= 0 && asked.Slot < underground.Traps.Count ? underground.Traps[asked.Slot] : 0;
                var result = Tunnels.SetTrap(asked.Slot, x, z, someone);
                if (result == BuryResult.Done)
                {
                    AudioManager.PlaySound("select");
                    ShowNotification($"The {UndergroundDatabase.TrapName(which)} was set.");
                }
                else Refuse(result, "set");
                break;
            }
            case UndergroundRequestKind.UseDrill:
                UseDiggerDrill(asked.Slot);
                break;
        }
    }

    private void Refuse(BuryResult why, string done)
    {
        AudioManager.PlaySound("error");
        ShowNotification(why switch
        {
            BuryResult.InTheBase => $"Nothing can be {done} in a secret base.",
            BuryResult.SomeoneThere => "Someone is standing there.",
            BuryResult.Wall => $"It can't be {done} in a wall.",
            BuryResult.SomethingThere => "Something is buried there already.",
            _ => $"No more can be {done}."
        });
    }

    /// <summary>
    /// The Digger Drill used facing a wall (<c>SecretBases_DiggerDrillTask</c>): where it may be, the drill is used up
    /// and a base is opened there; a base the player has already moves, its goods back in the PC.
    /// </summary>
    private void UseDiggerDrill(int slot)
    {
        var (x, z) = AheadBelow();
        if (InTheBase)
        {
            ShowNotification("The drill can't be used inside a base.");
            AudioManager.PlaySound("error");
            return;
        }
        if (baseEntrances.ContainsKey((x, z)) || !SecretBaseRules.CanDrill(UndergroundRock, x, z, flow.Player.Facing))
        {
            ShowNotification("It can't be used here.");
            AudioManager.PlaySound("error");
            return;
        }
        underground.Traps.RemoveAt(slot);
        bool moved = underground.Base != null;
        underground.Base = SecretBaseRules.Create(x, z, flow.Player.Facing, new Rand32((ulong)fieldRandom.NextInt64()));
        if (moved) underground.Records.TimesBaseMoved = UndergroundRecords.Add(underground.Records.TimesBaseMoved, 1);
        flow.Story.Set(CreatedBaseFlag);
        trainerScore = TrainerScore.Add(trainerScore, TrainerScore.NewSecretBase);
        AudioManager.PlaySound("rock_smash");
        LayOutEntrances();
        ShowNotification(moved ? "The drill opened a cavern! Your secret base has moved here." : "The drill opened a cavern! It is your secret base.");
    }

    /// <summary>The wall ahead is dug at (<c>Mining_StartGameTask</c>): its place on the map is used up as the game begins.</summary>
    private bool StartMining()
    {
        var (x, z) = AheadBelow();
        if (!Tunnels.StartMining(x, z)) return false;
        flow.Story.Set(DugForFossilsFlag);
        var game = new MiningGame(new Rand32((ulong)fieldRandom.NextInt64()), PlayerIdentity.Number % 2 == 1, flow.Pokedex.NationalUnlocked,
            !underground.HasMined, underground.NeverMined);
        flow.State = GameState.Mining;
        miningScreen.Open(game, underground);
        return true;
    }

    private void UpdateMining(float dt)
    {
        miningScreen.Update(dt);
        if (miningScreen.IsActive) return;
        // Everything out of the wall counts on the Trainer Card (TRAINER_SCORE_EVENT_UNDERGROUND_UNCOVER_FOSSIL)
        if (miningScreen.Game.AllDug) trainerScore = TrainerScore.Add(trainerScore, TrainerScore.UncoveredWall);
        flow.State = GameState.Overworld;
    }

    private void OpenHiker(NPC? hiker)
    {
        int vendor = hiker != null ? UndergroundVendors.At(hiker.GridX, hiker.GridY) : -1;
        if (vendor < 0) return;
        flow.State = GameState.Underground;
        undergroundScreen.OpenHiker(underground, vendor, flow.Pokedex.NationalUnlocked);
    }

    private void OpenBasePc()
    {
        flow.State = GameState.Underground;
        undergroundScreen.OpenPc(underground);
    }

    /// <summary>
    /// The base decorated (<c>BaseDecoration_StartDecorationMenuTask</c>): the shared decorating screen over the
    /// base's floor, with the PC's goods for its store, the PC and the boulders for its fixtures, and the original's
    /// rules for what may go where and which boulders may be cleared.
    /// </summary>
    private void OpenDecoration()
    {
        if (underground.Base == null) return;
        flow.State = GameState.Decoration;
        decorationScreen.Open(SecretBaseRules.Decorating(underground), "DECORATE THE BASE", piece => UndergroundArt.PlanLook(piece.Kind));
    }

    private void UpdateDecoration(float dt)
    {
        decorationScreen.Update(dt);
        if (decorationScreen.IsActive) return;
        // What stands in the room now is the base's (BaseDecoration_SaveBaseGoods), and the mission's flag is set whatever was done
        if (underground.Base is { } room)
        {
            SecretBaseRules.Keep(room, decorationScreen.Session.Room);
            flow.Story.Set(DecoratedBaseFlag);
            LayOutBase();
        }
        OpenBasePc();
    }

    /// <summary>What the Underground's own script commands do in the game (<c>underground …</c>); the number is the script's RESULT.</summary>
    private int UndergroundAct(string what, NPC? subject)
    {
        switch (what)
        {
            case "down":
                if (WhyNotGoUnderground() != null) return 0;
                GoUnderground();
                return 1;
            case "up":
                ComeUp();
                return 1;
            case "mine":
                return StartMining() ? 1 : 0;
            case "hiker":
                OpenHiker(subject);
                return 1;
            case "pc":
                OpenBasePc();
                return 1;
            case "bag":
                OpenUndergroundBag();
                return 1;
            case "takeflag":
                return visiting is { } n && Diggers.Take(underground, n) ? 1 : 0;
            case "cantake":
                return visiting is { } m && Diggers.CanTake(underground, m) ? 1 : 0;
            case "home":
            {
                if (underground.HeldFlag is not { } held) return 0;
                string owner = diggers.FirstOrDefault(d => d.Number == held)?.Name ?? "Someone";
                lastFlagOwner = owner;
                var better = Diggers.BringHome(underground, owner);
                flow.Story.Set(DeliveredFlagFlag);
                trainerScore = TrainerScore.Add(trainerScore, TrainerScore.CapturedFlag);
                if (better != null) LayOutBase();
                return better != null ? 2 : 1;
            }
            default:
                return 0;
        }
    }

    /// <summary>The name a line's <c>{digger}</c> stands for: whose base the player is in, or whose flag they carry.</summary>
    private string DiggerName => diggers.FirstOrDefault(d => d.Number == (visiting ?? underground.HeldFlag))?.Name ?? lastFlagOwner;

    private string lastFlagOwner = "someone";
}
