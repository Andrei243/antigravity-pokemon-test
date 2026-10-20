using System;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Overworld;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The Gyms' puzzles in the field (plan 01 · M9): each frame the room's puzzle is brought into line with the story,
/// a puzzle is laid out afresh when the player comes in by a door, and what a puzzle sets moving (the Eterna Gym's
/// clock turning) is played out over the field while the script that started it waits. The rules are the puzzles'
/// own (<see cref="GymPuzzle"/>), free of drawing and input.
/// </summary>
public partial class GameEngine
{
    // Where the camera looks while the Eterna Gym's clock turns (GameFlow.ClockTurn)
    private (int X, int Y)? clockLook;

    /// <summary>The room's puzzle as the story has it now, and what it has set moving.</summary>
    private void KeepPuzzleInForce(float dt)
    {
        if (flow.Map.Puzzle is not { } puzzle) return;
        puzzle.Apply(flow.Map, flow.Story);
        if (flow.ClockTurn != null) PlayClockTurn(dt);
        if (bagRun != null) PlayBagRun(dt);
        if (puzzle is PastoriaWater water) PlayWater(water, dt);
        if (puzzle is CanalaveLifts { Moving: not null } lifts) PlayLiftRide(lifts, dt);
        if (puzzle is SunyshoreGears { Turning: not null } gears) PlayGears(gears, dt);
        if (puzzle is LeagueLift { Moving: true } lift) PlayLeagueLift(lift, dt);
        if (puzzle is BlackthornPlatforms { Moving: not null } slabs) PlaySlabs(slabs, dt);
    }

    /// <summary>The player has come into a room by a door or a warp: its puzzle is laid out as the original lays it out on arrival.</summary>
    private void ArrivePuzzle()
    {
        if (flow.Map.Puzzle is not { } puzzle) return;
        puzzle.ArriveAt(flow.Map, flow.Story, Dice.Shared, flow.Player.GridX, flow.Player.GridY);
        // A League lift is laid out by the height the player stands at, which a save keeps (a game saved on the
        // platform at the top comes back with it there)
        if (puzzle is LeagueLift lift) lift.StandAt(flow.Player.HeightOn(flow.Map));
        puzzle.Apply(flow.Map, flow.Story);
    }

    /// <summary>Whether something a puzzle set moving is still being played out (the script waits for it).</summary>
    private bool PuzzleMoving => flow.ClockTurn != null
        || flow.Map.Puzzle is PastoriaWater { Moving: true } or CanalaveLifts { Moving: not null } or SunyshoreGears { Turning: not null }
            or LeagueLift { Moving: true } or BlackthornPlatforms { Moving: not null };

    /// <summary>Whether a puzzle has the field to itself for now, as the original's tasks do: a punching bag on its run.</summary>
    private bool PuzzleHoldsField => bagRun != null
        || flow.Map.Puzzle is PastoriaWater { Moving: true } or CanalaveLifts { Moving: not null } or SunyshoreGears { Turning: not null }
            or LeagueLift { Moving: true } or BlackthornPlatforms { Moving: not null };

    // ------------------------------------------------------------------ the Veilstone Gym's punching bags

    // A bag on its run: the kick, how long it has been going, and the time its windup and run take
    private (VeilstoneBags.Kick Kick, float Time, float Windup, float Run)? bagRun;

    /// <summary>The original's windup before a kicked bag gets going (thirty frames), and its speed once it has: two units of sixteen a frame.</summary>
    private const float BagWindup = 1f, BagTilesPerSecond = 30f * 2f / 16f, BagSwing = 0.4f;

    /// <summary>
    /// A punching bag the player faces is kicked (<c>VeilstoneGym_HitPunchingBag</c>): it swings where it can't go,
    /// or runs along its track, the camera going with it on a long run, and knocks down the stack of tyres it meets.
    /// </summary>
    private bool TryKickBag(NPC npc)
    {
        if (flow.Map.Puzzle is not VeilstoneBags || !VeilstoneBags.IsBag(npc) || bagRun != null) return false;
        var kick = VeilstoneBags.KickBag(flow.Map, npc, flow.Player.Facing, toppleNow: false);
        AudioManager.PlaySound("hit_normal");
        var (dx, dy) = FieldMovement.Delta(kick.Way);
        npc.StepOffsetX = -dx * kick.Distance;
        npc.StepOffsetY = -dy * kick.Distance;
        float run = kick.Distance / BagTilesPerSecond;
        bagRun = (kick, 0f, kick.Distance > 0 ? BagWindup : 0f, kick.Distance > 0 ? run : BagSwing);
        // A long run is followed by the camera (VeilstoneGym_CheckIfCameraNeedsToMove: over three tiles north, four any other way)
        if (kick.Distance > (kick.Way == Direction.Up ? 3 : 4))
        {
            world.PanCamera(npc.GridX + 0.5f, npc.GridY + 0.5f, BagWindup + run);
            flow.CameraSent = true;
        }
        return true;
    }

    private void PlayBagRun(float dt)
    {
        if (bagRun is not { } going) return;
        var (kick, time, windup, run) = going;
        time += dt;
        var bag = kick.Bag;
        var (dx, dy) = FieldMovement.Delta(kick.Way);
        if (kick.Distance == 0)
        {
            // Only a swing: the bag rocks away from the kick and back
            float t = Math.Clamp(time / run, 0f, 1f);
            float sway = 0.18f * MathF.Sin(t * MathF.PI * 2f) * (1f - t);
            bag.StepOffsetX = dx * sway;
            bag.StepOffsetY = dy * sway;
        }
        else
        {
            // Through the windup it creeps half a tile, gathering speed; then it runs on at its full speed
            float gone = time < windup ? 0.5f * (time / windup) * (time / windup)
                : 0.5f + (time - windup) * BagTilesPerSecond;
            gone = MathF.Min(gone, kick.Distance);
            bag.StepOffsetX = -dx * (kick.Distance - gone);
            bag.StepOffsetY = -dy * (kick.Distance - gone);
        }
        if (time < windup + run)
        {
            bagRun = (kick, time, windup, run);
            return;
        }
        bag.StepOffsetX = bag.StepOffsetY = 0f;
        if (kick.Distance > 0) AudioManager.PlaySound("bump");
        if (kick.Toppled is { } stack)
        {
            VeilstoneBags.Topple(flow.Map, stack);
            AudioManager.PlaySound("boulder");
        }
        if (flow.CameraSent && !flow.Runner.IsRunning)
        {
            world.ReleaseCamera(0.8f);
            flow.CameraSent = false;
        }
        bagRun = null;
    }

    // ------------------------------------------------------------------ the Pastoria Gym's water

    /// <summary>The water rises or falls to the level of the button pressed last, and goes quiet as it stops.</summary>
    private static void PlayWater(PastoriaWater water, float dt)
    {
        bool moving = water.Moving;
        water.Update(dt);
        if (moving && !water.Moving) AudioManager.PlaySound("bump");
    }

    // ------------------------------------------------------------------ the Snowpoint Gym's snowballs

    /// <summary>
    /// A snowball the player slid into fast enough has broken (<see cref="IceSlide"/>, <c>ov5_021E06A8</c>): it bursts
    /// into lumps of snow and a puff, with the original's crunch. It is back the next time the player comes in.
    /// </summary>
    private void ShatterSnowball()
    {
        if (flow.Player.TakeBroken() is not { } ball) return;
        world.Life.GiveWay(flow.Map, ball.GridX, ball.GridY, PropType.Snowball);
        AudioManager.PlaySound("snowball");
    }

    // ------------------------------------------------------------------ the Canalave Gym's lifts

    /// <summary>
    /// A step has ended on one of the Canalave Gym's platforms (<c>Field_ProcessStep</c>'s first check): it sets off for
    /// its other end with the player on it, and nothing else of the step happens.
    /// </summary>
    private bool TryRideLift()
    {
        if (flow.Map.Puzzle is not CanalaveLifts lifts || lifts.Moving != null) return false;
        if (lifts.PlatformAt(flow.Player.GridX, flow.Player.GridY, flow.Player.HeightOn(flow.Map)) is not { } index) return false;
        lifts.Board(index);
        AudioManager.PlaySound("lift");
        return true;
    }

    /// <summary>The platform carries the player on, and clanks to a stop at its end.</summary>
    private void PlayLiftRide(CanalaveLifts lifts, float dt)
    {
        var ride = lifts.Moving!;
        lifts.Update(dt);
        var (x, h, y) = ride.Now;
        flow.Player.CarryTo(x, y, h, dt);
        if (lifts.Moving == null) AudioManager.PlaySound("lift_stop");
    }

    // ------------------------------------------------------------------ the Sunyshore Gym's gears

    /// <summary>The gears turn on to the state of the button pressed last (their grinding was heard as it was pressed).</summary>
    private static void PlayGears(SunyshoreGears gears, float dt) => gears.Update(dt);

    // ------------------------------------------------------------------ the League's platform lifts

    /// <summary>
    /// The platform rises up its shaft with the player standing on it (<c>FieldTask_PlatformLiftGoUp</c>, which holds
    /// the player at the platform's height), and locks in place at the top with the original's clank.
    /// </summary>
    private void PlayLeagueLift(LeagueLift lift, float dt)
    {
        lift.Update(dt);
        flow.Player.CarryTo(flow.Player.GridX, flow.Player.GridY, lift.Height, dt);
        if (!lift.Moving) AudioManager.PlaySound("lift_stop");
    }

    // ------------------------------------------------------------------ the Blackthorn Gym's slabs (plan 20 · J11, part c)

    // Where the player stood as a slab set off with them on it
    private (int X, int Y) slabFrom;

    /// <summary>
    /// A button of the Blackthorn Gym's slabs under the player is pressed (HeartGold's <c>ov04_02255090</c>, which the
    /// field asks before anything ahead is spoken to): the slab turns or slides, its grinding heard as it sets off.
    /// </summary>
    private bool TryWorkSlab()
    {
        if (flow.Map.Puzzle is not BlackthornPlatforms slabs || slabs.Moving != null) return false;
        if (!slabs.Press(flow.Map, flow.Player.GridX, flow.Player.GridY)) return false;
        slabFrom = (flow.Player.GridX, flow.Player.GridY);
        AudioManager.PlaySound("lift");
        return true;
    }

    /// <summary>The slab goes on with the player standing on it, bumps where it meets the room and comes back, and stops.</summary>
    private void PlaySlabs(BlackthornPlatforms slabs, float dt)
    {
        var motion = slabs.Moving!;
        float before = motion.Time;
        slabs.Update(dt);
        if (motion.BumpsAt(before, motion.Time)) AudioManager.PlaySound("bump");
        float along = slabs.Moving == null ? (motion.Bumped ? 0f : motion.Tiles) : motion.Along;
        flow.Player.CarryTo(slabFrom.X + motion.DX * along, slabFrom.Y + motion.DZ * along, flow.Player.HeightOn(flow.Map), dt);
        if (slabs.Moving == null && !motion.Refused) AudioManager.PlaySound("lift_stop");
    }

    private void PlayClockTurn(float dt)
    {
        var turn = flow.ClockTurn!;
        bool stopped = turn.HandsStopped, draining = turn.Draining;
        turn.Update(dt);
        // The camera goes where the clock's turn looks: its middle, then the fountain that drains
        if (turn.Look != clockLook)
        {
            clockLook = turn.Look;
            world.PanCamera(turn.Look.X + 0.5f, turn.Look.Y + 0.5f, EternaClock.Turn.CameraSeconds);
            flow.CameraSent = true;
        }
        // The hands tick round, chime as they stop, and the fountain's water rushes away
        if (!turn.HandsStopped && flow.Map.Puzzle is EternaClock { Turning: not null } && ++flow.ClockTicks % 12 == 0) AudioManager.PlaySound("cursor");
        if (turn.HandsStopped && !stopped) AudioManager.PlaySound("select");
        if (turn.Draining && !draining) AudioManager.PlaySound("surf");
        if (turn.IsDone)
        {
            flow.ClockTurn = null;
            clockLook = null;
        }
    }

    private sealed partial class FieldHost
    {
        public void Defeat(string trainerId)
        {
            foreach (var map in MapDatabase.MapNames.Select(MapDatabase.Get))
                foreach (var npc in map.Everyone)
                    if (npc.TrainerData?.Id == trainerId) npc.HasBattled = true;
        }

        public void PressButton(PastoriaWater.Button button)
        {
            if (game.flow.Map.Puzzle is not PastoriaWater water) return;
            water.Press(button);
            AudioManager.PlaySound("select");
            if (water.Moving) AudioManager.PlaySound("surf");
        }

        public void PressGearButton(SunyshoreGears.Button kind)
        {
            if (game.flow.Map.Puzzle is not SunyshoreGears gears) return;
            gears.Press(kind);
            AudioManager.PlaySound("gears");
        }

        public bool TriggerLift()
        {
            if (game.flow.Map.Puzzle is not LeagueLift lift || !lift.Trigger()) return false;
            AudioManager.PlaySound("lift");
            return true;
        }

        public bool LiftNotUsed() => game.flow.Map.Puzzle is not LeagueLift lift || lift.NotUsedWhenEntered;

        public void TurnClock(int from, int to)
        {
            if (game.flow.Map.Puzzle is not EternaClock clock) return;
            game.flow.ClockTurn = clock.StartTurn(from, to);
            game.clockLook = null;
            game.flow.ClockTicks = 0;
            clock.Apply(game.flow.Map, game.flow.Story);
        }
    }
}
