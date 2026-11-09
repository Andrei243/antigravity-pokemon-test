using System;
using System.Collections.Generic;
using System.Numerics;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

internal sealed partial class BattleCamera
{
    // ------------------------------------------------------------------ push in, pull out, reactions (plan 45 · BC4)

    /// <summary>How far the attacker's shot dollies in over the wind-up at a whole push, in its height (3 to 2.4).</summary>
    public const float PushIn = 0.6f;

    /// <summary>How tight the target's shot starts at a whole push and how far it pulls out by the move's end, in its height (3 to 2.6, then out to 3.6).</summary>
    public const float PullTight = 0.4f, PullOut = 0.6f;

    /// <summary>How much longer a heavy blow (one with a hit-stop, <see cref="Directions"/>) holds the target tight before it pulls out, seconds.</summary>
    public const float HeavyHold = 0.25f;

    /// <summary>How long a reaction holds the one hit, and how much of its height it frames.</summary>
    public const float ReactTime = 0.6f, ReactFrame = 2.5f;

    /// <summary>How far past <see cref="Clearance"/> a pushed or a reaction shot keeps from the player's platform.</summary>
    public const float PushRoom = 0.25f;

    /// <summary>A manner's push now: its share of Galar's whole push, by <see cref="Motion"/>; nought for none.</summary>
    public static float PushOf(CameraManner? manner) =>
        manner is { Push: > 0f } m ? Math.Clamp(m.Push, 0f, 1f) * Math.Clamp(Motion, 0f, 1f) : 0f;

    /// <summary>The attacker's shot pushed in over the wind-up: from 3 × its height to 3 − <see cref="PushIn"/> × the push, kept clear of the player's platform.</summary>
    public static CameraShot PushedAttacker(FxPlaces places, EffectCue move, float cut, float push)
    {
        float scale = 3.0f - PushIn * push * Smooth(move.Age / cut);
        return Clear(s => Close(places, move.FromSide, move.FromSlot, s), scale);
    }

    /// <summary>
    /// The target's shot pulled out: tight at the cut (its usual frame less <see cref="PullTight"/> × the push), held so
    /// to the impact (and <see cref="HeavyHold"/> more for a heavy blow), then out to its usual frame plus
    /// <see cref="PullOut"/> × the push by the move's end, so the set piece is in the picture; kept clear of the player's platform.
    /// </summary>
    /// <param name="low">How much lower the shot stands as the target is hit, radians of pitch taken off (plan 45 · BC6: a manner's <see cref="CameraManner.Low"/>, Kalos's and Alola's).</param>
    public static CameraShot PulledTarget(FxPlaces places, List<(BattleSide Side, int Slot)> targets, EffectCue move, float push, float low = 0f)
    {
        float usual = move.Category == MoveCategory.Status ? 3.4f : 3.0f;
        float tight = usual - PullTight * push, wide = usual + PullOut * push;
        float from = BattleAnimator.ImpactTime + (Directions.For(move) is { Heavy: true } ? HeavyHold : 0f);
        float scale = tight + (wide - tight) * Smooth((move.Age - from) / Math.Max(0.05f, MoveShotEnd - from));
        if (low <= 0f) return Clear(s => TargetShot(places, targets, s), scale);
        return Clear(s => LoweredBy(TargetShot(places, targets, s), low), scale);
    }

    /// <summary>A shot swung down round what it looks at by <paramref name="low"/> radians of pitch, never under a quarter unit over the ground.</summary>
    public static CameraShot LoweredBy(CameraShot shot, float low)
    {
        var d = shot.Position - shot.Target;
        float flat = MathF.Sqrt(d.X * d.X + d.Z * d.Z), len = d.Length();
        if (flat < 1e-4f) return shot;
        float pitch = MathF.Atan2(d.Y, flat) - low;
        var to = new Vector3(d.X / flat * MathF.Cos(pitch), MathF.Sin(pitch), d.Z / flat * MathF.Cos(pitch)) * len;
        var at = shot.Target + to;
        if (at.Y < 0.25f) at.Y = 0.25f;
        return shot with { Position = at };
    }

    /// <summary>
    /// A shot at <paramref name="scale"/>, or framed a little wider (a tenth at a time) until it stands
    /// <see cref="Clearance"/> and <see cref="PushRoom"/> clear of the player's platform: a push never brings the
    /// camera nearer the platform than the director's eases may pass it.
    /// </summary>
    public static CameraShot Clear(Func<float, CameraShot> at, float scale)
    {
        var platform = BattleStage.PlayerSpot + new Vector3(0, 1.2f, 0);
        var shot = at(scale);
        for (int i = 0; i < 60 && Vector3.Distance(shot.Position, platform) < Clearance + PushRoom; i++)
            shot = at(scale += 0.1f);
        return shot;
    }

    /// <summary>When a move's own shots are over, seconds into its cue: a Max Move's let-go, a signature move's end, or an ordinary move's.</summary>
    public static float ShotsEnd(EffectCue cue) =>
        cue.MaxMove ? MaxLetGo : SignatureOf(cue) != null ? Directions.ShotEnd : MoveShotEnd;

    /// <summary>
    /// The reaction (plan 45 · BC4): the one the newest super-effective or critical blow landed on, from when the
    /// move's own shots are over for <see cref="ReactTime"/>, while it stands and no other move has begun; null otherwise.
    /// A pure function of the animator's clock: it holds no line, since the battle reads none of it.
    /// </summary>
    public static (CameraShot Shot, ShotKind Kind, EffectCue? Cue)? ReactNow(BattleAnimator anim, FxPlaces places, EffectCue? newestMove)
    {
        if (anim.LastStrike is not { } strike) return null;
        // Another move begun since (any cue of another start) has the camera
        if (newestMove != null && MathF.Abs(anim.Time - newestMove.Age - strike.Start) > 0.05f) return null;
        float from = strike.Start + ShotsEnd(strike.Cue);
        if (anim.Time < from || anim.Time >= from + ReactTime) return null;
        var cue = strike.Cue;
        var v = anim[cue.ToSide, cue.ToSlot];
        if (!v.Present || v.FaintAge >= 0f || v.Away) return null;
        return (ReactShot(places, cue.ToSide, cue.ToSlot), ShotKind.React, cue);
    }

    /// <summary>The one hit, close: the foe from the front turned a little more than its own shots, the player's own from its right as a send-out's; kept clear of the player's platform.</summary>
    public static CameraShot ReactShot(FxPlaces places, BattleSide side, int slot) =>
        Clear(s => Close(places, side, slot, s, side == BattleSide.Player ? 0.35f : 0.12f), ReactFrame);
}
