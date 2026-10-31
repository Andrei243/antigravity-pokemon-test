using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Audio;
using PokemonPlatinumEngine.Graphics;
using PokemonPlatinumEngine.Overworld;
using PokemonPlatinumEngine.Story;

namespace PokemonPlatinumEngine.Core;

/// <summary>
/// The original's set pieces in the game (plan 09 · L12; <see cref="SetPieces"/>): what a script's <c>anim</c>,
/// <c>shake</c> and <c>flicker</c> set going in the place the player stands in. The pieces themselves are functions of
/// the field's clock, kept by the renderer's <see cref="SetPieceStage"/>; here is what the place has to play them on,
/// and their sounds.
/// </summary>
public partial class GameEngine
{
    // What the pieces playing were heard to do this frame: a list of the engine's own, so a frame makes none
    private readonly List<(SetPieceCue Cue, int? X)> pieceCues = new();

    /// <summary>
    /// Plays a set piece where the player stands and says how long the script waits for it: nothing where the place
    /// has nothing to play it on, so no script is ever left waiting.
    /// </summary>
    private float PlaySetPiece(SetPiece piece, int count)
    {
        double now = world.Life.Now;
        switch (piece)
        {
            case SetPiece.Heal:
                // A room with no machine has the heal's fanfare, as a healing always had, and nothing to wait for
                if (flow.Map.Props.FirstOrDefault(p => p.Type == PropType.HealingMachine) is not { } machine)
                {
                    AudioManager.PlayFanfare(MusicRole.FanfareHeal);
                    return 0f;
                }
                return world.Pieces.Play(piece, count, now, (machine.X, machine.Y));

            case SetPiece.HallOfFame:
                if (!GymPieces.HasMachine(flow.Map)) return 0f;
                return world.Pieces.Play(piece, count, now, (flow.Map.Width / 2, flow.Map.RoomCorner().Back));

            case SetPiece.PcOn:
                // The PC before the player, or the nearest that is used; with none there is its sound alone
                if (TerminalNear() is not { } terminal)
                {
                    AudioManager.PlaySound("pc_on");
                    return 0f;
                }
                return world.Pieces.Play(piece, 0, now, (terminal.GridX, terminal.GridY));

            case SetPiece.PcOff:
            {
                // The PC that is on goes off; with none on there is its sound alone
                float takes = world.Pieces.Play(piece, 0, now);
                if (takes <= 0f) AudioManager.PlaySound("pc_off");
                return takes;
            }

            default:
                // A lift's ride is the car's own: it hums and shudders wherever it is, and its indicator runs where it has one
                return world.Pieces.Play(piece, count, now, (flow.Player.GridX, flow.Player.GridY));
        }
    }

    /// <summary>The PC that is used nearest the player on this map (the one faced is the nearest), or null where there is none.</summary>
    private NPC? TerminalNear()
    {
        var (dx, dy) = FieldMovement.Delta(flow.Player.Facing);
        int fx = flow.Player.GridX + dx, fy = flow.Player.GridY + dy;
        NPC? nearest = null;
        int best = int.MaxValue;
        foreach (var npc in flow.Map.NPCs.Near(flow.Player.GridX, flow.Player.GridY, Wandering.Reach, new List<NPC>()))
        {
            if (!npc.IsPCTerminal) continue;
            int far = Math.Abs(npc.GridX - fx) + Math.Abs(npc.GridY - fy);
            if (far < best) (nearest, best) = (npc, far);
        }
        return nearest;
    }

    /// <summary>Someone is shaken or flickers, if they are on the map (nobody is the player); says how long the script waits.</summary>
    private float MoveSomeone(NPC? who, int times, bool flicker)
    {
        if (who != null && !flow.Map.NPCs.Contains(who)) return 0f;
        return flicker ? world.Pieces.Flicker(who, times, world.Life.Now) : world.Pieces.Shake(who, times, world.Life.Now);
    }

    /// <summary>
    /// A frame of the set pieces: what they are heard to do now (a ball laid, the heal's fanfare as the balls light, a
    /// PC's sounds, a lift's hum and chime), each from its own side of the screen; those that are over are let go; and
    /// the Hall of Fame's machine shows the balls its piece has laid.
    /// </summary>
    private void RunTheSetPieces()
    {
        var pieces = world.Pieces;
        double now = world.Life.Now;
        pieceCues.Clear();
        pieces.TakeCues(now, pieceCues);
        foreach (var (cue, x) in pieceCues)
        {
            if (cue.Fanfare) AudioManager.PlayFanfare(MusicRole.FanfareHeal);
            else if (cue.Sound != null) AudioManager.PlaySound(cue.Sound, x is { } tile ? PanAt(tile) : 0f);
        }
        pieces.Advance(now);
        KeepTheMachinesBalls();
    }

    private sealed partial class FieldHost
    {
        public float Anim(SetPiece piece, int count)
        {
            float takes = game.PlaySetPiece(piece, count);
            if (!Skipping) return takes;
            // In a hurry the piece is at its end at once, unheard (plan 12 · Q20): what it leaves is left (a PC's
            // screen on or dark, the balls in the Hall of Fame's sockets), and a tray's balls and a lift's ride are over
            // (The time it would have taken is still told: it passes for whoever is walking)
            game.world.Pieces.Settle(game.world.Life.Now);
            return takes;
        }

        // (Someone shaken or flickering stands as they stood once it is over: a hurry drops both, and tells their time)
        public float Shake(NPC? who, int times) =>
            !Skipping ? game.MoveSomeone(who, times, flicker: false) : Dropped(who) ? 0f : SetPieces.ShakeLength(times);

        public float Flicker(NPC? who, int times) =>
            !Skipping ? game.MoveSomeone(who, times, flicker: true) : Dropped(who) ? 0f : SetPieces.FlickerLength(times);

        // A photograph (plan 20 · J13): the screenshot key's own picture of the next frame drawn, with its shutter and toast
        public void Photograph() => game.AskForScreenshot();

        // (Nobody of that name on the map: the piece takes no time, played or not)
        private bool Dropped(NPC? who) => who != null && !game.flow.Map.NPCs.Contains(who);
    }
}
