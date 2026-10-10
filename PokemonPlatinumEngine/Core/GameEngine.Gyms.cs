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
    // The Eterna Gym's clock turning on, which the script that turned it waits for
    private EternaClock.Turn? clockTurn;
    private (int X, int Y)? clockLook;
    private int clockTicks;

    /// <summary>The room's puzzle as the story has it now, and what it has set moving.</summary>
    private void KeepPuzzleInForce(float dt)
    {
        if (currentMap.Puzzle is not { } puzzle) return;
        puzzle.Apply(currentMap, story);
        if (clockTurn != null) PlayClockTurn(dt);
        if (bagRun != null) PlayBagRun(dt);
        if (puzzle is PastoriaWater { Rising: not null } water) PlayWaterRise(water, dt);
    }

    /// <summary>The player has come into a room by a door or a warp: its puzzle is laid out as the original lays it out on arrival.</summary>
    private void ArrivePuzzle()
    {
        if (currentMap.Puzzle is not { } puzzle) return;
        puzzle.Arrive(currentMap, story, Dice.Shared);
        puzzle.Apply(currentMap, story);
    }

    /// <summary>Whether something a puzzle set moving is still being played out (the script waits for it).</summary>
    private bool PuzzleMoving => clockTurn != null || currentMap.Puzzle is PastoriaWater { Rising: not null };

    // ------------------------------------------------------------------ the Pastoria Gym's water

    // Whether the water was flowing last frame, for its sound
    private bool waterFlowing;

    /// <summary>The Pastoria Gym's water on its way to a button's height (<see cref="PastoriaWater.Rise"/>), with its sound.</summary>
    private void PlayWaterRise(PastoriaWater water, float dt)
    {
        var rise = water.Rising!;
        rise.Update(dt);
        // The rush of water starts as the buttons have settled (the original's SEQ_SE_DP_FW056) and stops with it
        if (rise.Flowing && !waterFlowing) AudioManager.PlaySound("surf");
        waterFlowing = rise.Flowing;
        if (rise.IsDone) waterFlowing = false;
    }

    /// <summary>Whether a puzzle has the field to itself for now, as the original's tasks do: a punching bag on its run.</summary>
    private bool PuzzleHoldsField => bagRun != null;

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
        if (currentMap.Puzzle is not VeilstoneBags || !VeilstoneBags.IsBag(npc) || bagRun != null) return false;
        var kick = VeilstoneBags.KickBag(currentMap, npc, player.Facing, toppleNow: false);
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
            cameraSent = true;
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
            VeilstoneBags.Topple(currentMap, stack);
            AudioManager.PlaySound("boulder");
        }
        if (cameraSent && !runner.IsRunning)
        {
            world.ReleaseCamera(0.8f);
            cameraSent = false;
        }
        bagRun = null;
    }

    private void PlayClockTurn(float dt)
    {
        var turn = clockTurn!;
        bool stopped = turn.HandsStopped, draining = turn.Draining;
        turn.Update(dt);
        // The camera goes where the clock's turn looks: its middle, then the fountain that drains
        if (turn.Look != clockLook)
        {
            clockLook = turn.Look;
            world.PanCamera(turn.Look.X + 0.5f, turn.Look.Y + 0.5f, EternaClock.Turn.CameraSeconds);
            cameraSent = true;
        }
        // The hands tick round, chime as they stop, and the fountain's water rushes away
        if (!turn.HandsStopped && currentMap.Puzzle is EternaClock { Turning: not null } && ++clockTicks % 12 == 0) AudioManager.PlaySound("cursor");
        if (turn.HandsStopped && !stopped) AudioManager.PlaySound("select");
        if (turn.Draining && !draining) AudioManager.PlaySound("surf");
        if (turn.IsDone)
        {
            clockTurn = null;
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

        public void TurnClock(int from, int to)
        {
            if (game.currentMap.Puzzle is not EternaClock clock) return;
            game.clockTurn = clock.StartTurn(from, to);
            game.clockLook = null;
            game.clockTicks = 0;
            clock.Apply(game.currentMap, game.story);
        }

        public void PressWaterButton(PastoriaWater.Button button)
        {
            if (game.currentMap.Puzzle is not PastoriaWater water) return;
            // The buttons go down and come up with a click, and the water follows (PastoriaGym_UpdateButtonAnimations)
            AudioManager.PlaySound("select");
            water.Press(button);
            game.waterFlowing = false;
        }
    }
}
