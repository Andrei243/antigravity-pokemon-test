using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

internal sealed partial class BattleCamera
{
    // ------------------------------------------------------------------ opening and closing (plan 45 · BC5)

    /// <summary>How long a manner's opening takes before the overview, seconds of the battle's clock: the sweep's own length.</summary>
    public const float OpenTime = 1.8f;

    /// <summary>A wild opening: how long the lens holds close on the foe before it pulls out, and how much of its height it frames.</summary>
    public const float WildOpenHold = 0.5f, WildOpenFrame = 2.6f;

    /// <summary>A trainer's opening: how much of their height the shot frames as it begins and as it has pushed in (over <see cref="TrainerOpenPush"/> s).</summary>
    public const float TrainerOpenFrame = 1.9f, TrainerOpenTight = 1.6f, TrainerOpenPush = 3f;

    /// <summary>The crane: how far off the line between the platforms it stands (world units), its pitch (radians) and its lens.</summary>
    public const float CraneReach = 30f, CranePitch = 0.95f, CraneFov = 34f;

    /// <summary>How long the crane takes to come down onto the trainer at a boss's opening, and to rise after the win's shot at the closing.</summary>
    public const float CraneTime = 2.6f;

    /// <summary>How long the win's face-on shot holds before a boss's crane rises from it.</summary>
    public const float CloseHold = 2.6f;

    /// <summary>The win's shot: its lens, its look down (radians), how much of the tallest subject's height it frames, and how far it pushes in over <see cref="ClosePush"/> s.</summary>
    public const float CloseFov = 28f, CloseLift = 0.24f, CloseFrame = 2.4f, CloseIn = 0.08f, ClosePush = 4f;

    /// <summary>How far right of and under the win's subjects its aim stands, in the shot's frame: they stand a little left and high.</summary>
    public const float CloseAside = 0.12f, CloseUnder = 0.06f;

    /// <summary>The loss's shot: how much of the foes' height it frames.</summary>
    public const float LossFrame = 3.0f;

    /// <summary>Where the player's trainer stands at a win (plan 45 · BC5): behind their platform and to its east, so a big Pokémon doesn't hide them.</summary>
    public static readonly Vector3 WinnerSpot = BattleStage.PlayerSpot + new Vector3(1.05f, 0f, 1.25f);

    /// <summary>How tall the player's trainer stands (their platform's scale), world units.</summary>
    public static float WinnerHeight => TrainerHeight * BattleRenderer.CallerScale(true);

    /// <summary>
    /// A manner's opening (plan 45 · BC5), where the battle's own shots leave the camera free in its first moments: a
    /// wild Pokémon close through the overview's own lens, pulled out to the overview over <see cref="OpenTime"/>; a
    /// trainer on their platform from the front while they stand there (from the crane down onto them, at a boss's
    /// battle under a manner with the crane); then the overview. Null where the manner doesn't open, under REDUCE
    /// MOTION, or once the opening is over.
    /// </summary>
    public static (CameraShot Shot, ShotKind Kind, EffectCue? Cue)? OpeningNow(BattleAnimator anim, FxPlaces places, CameraManner manner)
    {
        if (!manner.Open || Motion <= 0f) return null;
        float t = anim.Time;
        bool trainer = anim.EnemyTrainerLook != null;
        if (trainer)
        {
            // The trainer stands on their platform until they throw: their shot, pushed in a little as the line is read
            if (anim.EnemyTrainer == null || anim.EnemyTrainerExit >= 0f || AnyFoeOut(anim)) return null;
            var shot = TrainerOpenShot(anim, places, t);
            if (manner.Crane && anim.BossBattle && t < CraneTime)
                shot = CameraShot.Lerp(CraneShot(anim, places, manner), shot, Smooth(t / CraneTime));
            return (shot, ShotKind.Opening, null);
        }
        if (t >= OpenTime) return null;
        return (WildOpenShot(anim, places, t), ShotKind.Opening, null);
    }

    private static bool AnyFoeOut(BattleAnimator anim)
    {
        for (int slot = 0; slot < anim.SlotsOf(BattleSide.Enemy); slot++)
            if (anim[BattleSide.Enemy, slot].Present) return true;
        return false;
    }

    /// <summary>
    /// The wild opening at <paramref name="time"/>: from the overview's own stand, its lens narrowed onto the foes
    /// (<see cref="WildOpenFrame"/> × their height) and held <see cref="WildOpenHold"/> s, then widened and turned back to
    /// the overview by <see cref="OpenTime"/>. The camera never moves, so nothing passes the player's platform.
    /// </summary>
    public static CameraShot WildOpenShot(BattleAnimator anim, FxPlaces places, float time)
    {
        var overview = Overview(time);
        var (middle, h, spread) = Group(places, SideOf(anim, BattleSide.Enemy));
        float size = Math.Max(h * WildOpenFrame, spread * 3.2f);
        float dist = Math.Max(1f, Vector3.Distance(overview.Position, middle));
        float fov = Math.Clamp(2f * MathF.Atan(size / (2f * dist)) * 180f / MathF.PI, 6f, OverviewFov);
        var close = new CameraShot(overview.Position, middle, fov);
        return CameraShot.Lerp(close, overview, Smooth((time - WildOpenHold) / (OpenTime - WildOpenHold)));
    }

    /// <summary>
    /// The trainer (or two) on their platform from the front as a speaker is (0.18 rad round, a little lifted), their
    /// whole height in the shot and pushed in from <see cref="TrainerOpenFrame"/> to <see cref="TrainerOpenTight"/> × it
    /// over <see cref="TrainerOpenPush"/> s, standing in front of the player's platform.
    /// </summary>
    public static CameraShot TrainerOpenShot(BattleAnimator anim, FxPlaces places, float time)
    {
        bool two = anim.EnemyTrainer2 != null;
        float h = TrainerHeight * (two ? 2.1f : 2.3f);
        var feet = places.Feet(BattleSide.Enemy, 0);
        var focus = new Vector3(BattleStage.EnemySpot.X, feet.Y + h * 0.5f, BattleStage.EnemySpot.Z);
        float frame = TrainerOpenFrame + (TrainerOpenTight - TrainerOpenFrame) * Smooth(time / TrainerOpenPush);
        return Frame(focus, h * frame * (two ? 1.35f : 1f), -0.18f, 0.06f, limitZ: FoeShotLimit);
    }

    /// <summary>
    /// The crane (plan 45 · BC5, Galar's Leaders and Champions): high over the stage from the east of the line between
    /// the platforms, looking down at its middle, both sides and the stage's ground in the picture. It stands north of
    /// the player's platform, so an ease to or from a foe's shot never passes it.
    /// </summary>
    public static CameraShot CraneShot(BattleAnimator anim, FxPlaces places, CameraManner manner)
    {
        var own = SideOf(anim, BattleSide.Player);
        var foes = SideOf(anim, BattleSide.Enemy);
        var d = TowardFoe(places, own, foes);
        var right = Vector3.Normalize(Vector3.Cross(d, Vector3.UnitY));
        var middle = (BattleStage.EnemySpot + BattleStage.PlayerSpot) / 2f + Vector3.UnitY * places.Feet(BattleSide.Enemy, 0).Y;
        // From the west: the foes stand on the picture's left under their HP box and the player's own on its right over theirs
        var dir = Vector3.Normalize(-right * MathF.Cos(CranePitch) + Vector3.UnitY * MathF.Sin(CranePitch));
        return new CameraShot(middle + dir * CraneReach * manner.Reach, middle, CraneFov);
    }

    /// <summary>
    /// A manner's closing (plan 45 · BC5), once the battle has ended (<see cref="BattleAnimator.Closing"/>): a win holds the
    /// player's standing Pokémon from the front with their trainer come back behind it (<see cref="WinShot"/>), and at a
    /// boss's battle under a manner with the crane rises to the crane after <see cref="CloseHold"/>; a loss holds the
    /// foe's side cheering; a catch keeps the ball's own shots (null). Null where the manner doesn't close or under
    /// REDUCE MOTION. A function of the animator's clock alone: it holds no line.
    /// </summary>
    public static (CameraShot Shot, ShotKind Kind, EffectCue? Cue)? ClosingNow(BattleAnimator anim, FxPlaces places, CameraManner manner)
    {
        if (!manner.Close || Motion <= 0f || anim.Closing is not { } closing) return null;
        float age = anim.Time - closing.Start;
        if (age < 0f) return null;
        switch (closing.Kind)
        {
            case BattleClosing.Won:
            {
                var shot = WinShot(anim, places, age);
                if (manner.Crane && anim.BossBattle && age > CloseHold)
                {
                    float up = Smooth((age - CloseHold) / CraneTime);
                    shot = up >= 1f ? CraneShot(anim, places, manner) : CameraShot.Lerp(shot, CraneShot(anim, places, manner), up);
                }
                return (shot, ShotKind.Closing, null);
            }
            case BattleClosing.Lost:
            {
                var foes = Standing(anim, BattleSide.Enemy);
                if (foes.Count == 0) return null;
                var (middle, h, spread) = Group(places, foes);
                var shot = Frame(middle, Math.Max(h * LossFrame, spread * 3.4f), -0.18f + FaceTurn * 0.5f, 0.08f, IdleFov, FoeShotLimit);
                var panels = IdleFrame.Panels(anim.SlotsOf(BattleSide.Enemy), anim.SlotsOf(BattleSide.Player));
                return (Fitted(shot, places, foes, panels) ?? shot, ShotKind.Closing, null);
            }
            default:
                return null;
        }
    }

    /// <summary>The places of a side whose Pokémon stands there, in sight and not going down.</summary>
    private static List<(BattleSide Side, int Slot)> Standing(BattleAnimator anim, BattleSide side)
    {
        var list = new List<(BattleSide, int)>();
        for (int slot = 0; slot < anim.SlotsOf(side); slot++)
        {
            var v = anim[side, slot];
            if (v.Present && !v.Away && v.FaintAge < 0f && v.RecallAge < 0f && v.CaptureAge < 0f) list.Add((side, slot));
        }
        return list;
    }

    /// <summary>
    /// The win's shot (the first face-on shot of the player's trainer): from between the platforms looking south at the
    /// player's standing Pokémon with their trainer behind it at <see cref="WinnerSpot"/>, turned a little and looking
    /// down <see cref="CloseLift"/>, framing <see cref="CloseFrame"/> × the tallest of them and pushing in by
    /// <see cref="CloseIn"/> over <see cref="ClosePush"/> s; widened a degree at a time (up to <see cref="WidestFov"/>)
    /// until everyone stands in the picture's free part.
    /// </summary>
    public static CameraShot WinShot(BattleAnimator anim, FxPlaces places, float age)
    {
        var own = Standing(anim, BattleSide.Player);
        var foes = SideOf(anim, BattleSide.Enemy);
        var d = TowardFoe(places, SideOf(anim, BattleSide.Player), foes);
        var trainerFeet = WinnerSpot + Vector3.UnitY * places.Feet(BattleSide.Player, 0).Y;
        float th = WinnerHeight;
        var middle = trainerFeet + Vector3.UnitY * th * 0.5f;
        float h = th, spread = 0.6f;
        if (own.Count > 0)
        {
            var (pm, ph, ps) = Group(places, own);
            middle = (middle + pm) / 2f;
            h = Math.Max(h, ph);
            spread = Math.Max(spread, Vector3.Distance(pm with { Y = 0 }, trainerFeet with { Y = 0 }) * 0.5f + ps);
        }
        float size = Math.Max(h * CloseFrame, spread * 3.2f) * (1f - CloseIn * Smooth(age / ClosePush));
        var from = Vector3.Transform(d, Matrix4x4.CreateRotationY(-FaceTurn));
        var dir = Vector3.Normalize(from * MathF.Cos(CloseLift) + Vector3.UnitY * MathF.Sin(CloseLift));
        float dist = size / (2f * MathF.Tan(CloseFov * MathF.PI / 360f));
        // Aimed a little right of and under the two, so they stand left of the player's HP box and above the message box
        var look = Vector3.Normalize(-dir);
        var aside = Vector3.Normalize(Vector3.Cross(look, Vector3.UnitY));
        var aim = middle + aside * size * CloseAside - Vector3.UnitY * size * CloseUnder;
        var shot = new CameraShot(middle + dir * dist, aim, CloseFov);
        var panels = IdleFrame.Panels(anim.SlotsOf(BattleSide.Enemy), anim.SlotsOf(BattleSide.Player));
        for (float fov = CloseFov; ; fov = Math.Min(WidestFov, fov + 1f))
        {
            var tried = shot with { Fov = fov };
            if ((fov >= WidestFov) || (WinnerBox(tried, places) is { } box && IdleFrame.Holds(box, panels)
                && own.TrueForAll(s => IdleFrame.BoxOf(tried, places, s.Side, s.Slot) is { } b && IdleFrame.Holds(b, panels))))
                return tried;
        }
    }

    /// <summary>The player's trainer's box in the picture through a shot at the win, as <see cref="IdleFrame.BoxOf"/> gives a Pokémon's; null behind the lens.</summary>
    public static Rectangle? WinnerBox(CameraShot shot, FxPlaces places)
    {
        var feet = WinnerSpot + Vector3.UnitY * places.Feet(BattleSide.Player, 0).Y;
        var probe = new FxPlaces();
        probe.Set(BattleSide.Player, 0, feet, WinnerHeight, WinnerHeight * 0.22f);
        return IdleFrame.BoxOf(shot, probe, BattleSide.Player, 0);
    }
}
