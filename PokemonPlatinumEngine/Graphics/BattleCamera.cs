using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>Where the battle camera stands, what it looks at and its vertical field of view (degrees).</summary>
internal readonly record struct CameraShot(Vector3 Position, Vector3 Target, float Fov)
{
    public static CameraShot Lerp(CameraShot a, CameraShot b, float t) =>
        new(Vector3.Lerp(a.Position, b.Position, t), Vector3.Lerp(a.Target, b.Target, t), a.Fov + (b.Fov - a.Fov) * t);

    public Camera3D ToCamera() => new(Position, Target, Vector3.UnitY, Fov, CameraProjection.Perspective);
}

/// <summary>What the director is showing.</summary>
internal enum ShotKind { Intro, Overview, SendOut, Attacker, Target, Self, Critical, Throw, Ball, BallClose, Faint }

/// <summary>
/// The battle's camera director (plan 04 · G8). The overview is the DS-like framing; the director sweeps in at the
/// start, moves in on a Pokémon being sent out, follows the attacker as it winds up and cuts to the target just
/// before the hit, punches in on a critical hit, follows a thrown ball and closes on it while it wobbles, and
/// watches a Pokémon faint. It eases between shots (cutting from attacker to target, into a critical punch-in, and
/// wherever easing would fly past the player's side), all as a function of the animator's state, so tests can check
/// every shot without a window.
/// </summary>
internal sealed class BattleCamera
{
    /// <summary>Time constant of the easing between shots, seconds.</summary>
    public const float Smoothing = 0.18f;

    /// <summary>The overview's field of view and pitch; see <see cref="BattleStage"/> for its position.</summary>
    public const float OverviewFov = BattleStage.FovYDeg;

    /// <summary>When the attacker's shot cuts to its target, seconds into the move (just before the impact).</summary>
    public const float CutTime = 0.24f;

    /// <summary>When a move's shots end and the camera eases back.</summary>
    public const float MoveShotEnd = 0.95f;

    /// <summary>How close a move between shots may pass to the player's platform before the director cuts instead.</summary>
    public const float Clearance = 3f;

    /// <summary>
    /// The furthest south (+Z) a shot of the foe's side may stand: in front of the player's platform, so a big foe is
    /// framed with a wider lens (up to <see cref="WidestFov"/>) instead of from among the player's side.
    /// </summary>
    public static readonly float FoeShotLimit = BattleStage.PlayerSpot.Z - 2.5f;

    /// <summary>The widest lens a shot may take on to frame a big foe, degrees.</summary>
    public const float WidestFov = 40f;

    public CameraShot Current { get; private set; }
    public ShotKind Kind { get; private set; } = ShotKind.Intro;
    private bool started;
    private ShotKind lastKind;
    private EffectCue? lastCue;

    public void Reset() => started = false;

    /// <summary>Moves the camera toward what the director wants and returns it, shaken by <paramref name="shake"/> (0..2).</summary>
    /// <param name="choosing">The player is choosing a command: the camera goes back to the overview.</param>
    public Camera3D Update(float dt, BattleAnimator anim, FxPlaces places, float shake, bool choosing = false)
    {
        var (shot, kind, cue) = Direct(anim, places, choosing);
        // Cut from the attacker to its target, into a critical hit's punch-in, onto the intro's own path, and
        // wherever easing would fly the camera past the player's side of the field; ease everything else
        bool cut = !started || kind == ShotKind.Intro
            || (kind == ShotKind.Target && lastKind == ShotKind.Attacker && cue == lastCue)
            || (kind == ShotKind.Critical && lastKind != ShotKind.Critical)
            || CrossesPlayerSide(Current.Position, shot.Position);
        Current = cut ? shot : CameraShot.Lerp(Current, shot, 1f - MathF.Exp(-Math.Max(0f, dt) / Smoothing));
        started = true;
        lastKind = kind;
        lastCue = cue;
        Kind = kind;

        var camera = Current.ToCamera();
        if (shake > 0f)
        {
            float t = anim.Time;
            float reach = Vector3.Distance(Current.Position, Current.Target) / 26f;
            var jolt = new Vector3(MathF.Sin(t * 71f), MathF.Sin(t * 53f + 1f), 0) * 0.06f * shake * Math.Clamp(reach, 0.35f, 1f);
            camera.Position += jolt;
            camera.Target += jolt;
        }
        return camera;
    }

    /// <summary>
    /// Whether a straight move between two camera positions would carry the camera past the player's platform, where
    /// the trainer or the player's Pokémon would loom up in front of the lens on the way: from behind the platform to
    /// in front of it (or back), or within <see cref="Clearance"/> of it.
    /// </summary>
    public static bool CrossesPlayerSide(Vector3 a, Vector3 b)
    {
        float plane = BattleStage.PlayerSpot.Z;
        if ((a.Z - plane) * (b.Z - plane) < 0f) return true;
        var p = BattleStage.PlayerSpot + new Vector3(0, 1.2f, 0);
        var ab = b - a;
        float len = ab.LengthSquared();
        float t = len < 1e-6f ? 0f : Math.Clamp(Vector3.Dot(p - a, ab) / len, 0f, 1f);
        return Vector3.Distance(a + ab * t, p) < Clearance;
    }

    // ------------------------------------------------------------------ shots

    /// <summary>Over to the left, where the player runs in to throw a ball.</summary>
    private static CameraShot ThrowShot(float time)
    {
        var o = Overview(time);
        var shift = new Vector3(-1.5f, 0.1f, 1.2f);
        return o with { Position = o.Position + shift, Target = o.Target + shift * 0.6f };
    }

    /// <summary>The settled framing, with a slow drift about the middle of the field.</summary>
    public static CameraShot Overview(float time) => Orbit(0.012f * MathF.Sin(time * 0.35f), 1f);

    /// <summary>The opening sweep: in from the side and further out, settling into the overview over 1.8 s.</summary>
    public static CameraShot Intro(float time)
    {
        float t = Math.Clamp(time / 1.8f, 0f, 1f);
        float intro = (1f - t) * (1f - t) * (1f - t);
        return Orbit(0.55f * intro + 0.012f * MathF.Sin(time * 0.35f), 1f + 0.35f * intro);
    }

    private static CameraShot Orbit(float angle, float zoom)
    {
        float pitch = BattleStage.PitchDeg * MathF.PI / 180f;
        var forward = new Vector3(0, -MathF.Sin(pitch), -MathF.Cos(pitch));
        var position = BattleStage.CameraPosition;
        var target = position + forward * 20f;
        var pivot = (BattleStage.EnemySpot + BattleStage.PlayerSpot) / 2f;
        var turn = Matrix4x4.CreateRotationY(angle);
        return new CameraShot(pivot + Vector3.Transform(position - pivot, turn) * zoom, pivot + Vector3.Transform(target - pivot, turn), OverviewFov);
    }

    /// <summary>
    /// A shot of <paramref name="focus"/> that fits <paramref name="size"/> world units in the frame's height, seen
    /// from the overview's side turned by <paramref name="yaw"/> and raised by <paramref name="lift"/> (radians). A
    /// shot that would stand further south than <paramref name="limitZ"/> comes closer and widens its lens instead.
    /// </summary>
    public static CameraShot Frame(Vector3 focus, float size, float yaw = 0f, float lift = 0f, float fov = OverviewFov,
        float limitZ = float.PositiveInfinity)
    {
        var from = BattleStage.CameraPosition - focus;
        from.Y = 0f;
        from = Vector3.Normalize(from);
        from = Vector3.Transform(from, Matrix4x4.CreateRotationY(yaw));
        float pitch = BattleStage.PitchDeg * MathF.PI / 180f + lift;
        var dir = Vector3.Normalize(from * MathF.Cos(pitch) + Vector3.UnitY * MathF.Sin(pitch));
        float dist = size / (2f * MathF.Tan(fov * MathF.PI / 360f));
        if (dir.Z > 0.01f && focus.Z + dir.Z * dist > limitZ)
        {
            dist = Math.Max(1f, (limitZ - focus.Z) / dir.Z);
            fov = Math.Min(WidestFov, 2f * MathF.Atan(size / (2f * dist)) * 180f / MathF.PI);
        }
        return new CameraShot(focus + dir * dist, focus, fov);
    }

    /// <summary>A Pokémon's own shot: the foe from the front, the player's from its right, a little in front.</summary>
    public static CameraShot Close(FxPlaces places, BattleSide side, int slot, float scale, float yaw = 0f)
    {
        float h = places.Height(side, slot);
        var focus = places.Feet(side, slot) + Vector3.UnitY * h * 0.5f;
        // The foe's shots look down a little more than the overview and stand in front of the player's platform
        return side == BattleSide.Enemy
            ? Frame(focus, h * scale, -0.18f + yaw, 0.1f, limitZ: FoeShotLimit)
            : Frame(focus, h * scale, -0.75f + yaw, 0.06f);
    }

    /// <summary>The shot the director wants now, what kind it is, and the move cue it follows (if any).</summary>
    /// <param name="choosing">The player is choosing a command, which is always done from the overview.</param>
    public static (CameraShot Shot, ShotKind Kind, EffectCue? Cue) Direct(BattleAnimator anim, FxPlaces places, bool choosing = false)
    {
        if (choosing && anim.Time >= 1.8f) return (Overview(anim.Time), ShotKind.Overview, null);

        // A thrown ball takes the camera with it
        if (anim.Ball is { } ball && !(ball.Caught && ball.Age > BattleAnimator.BallThrowTime(ball.Shakes)))
            return BallShot(anim, ball, places);

        // The newest move
        EffectCue? move = null;
        for (int i = anim.Cues.Count - 1; i >= 0; i--)
            if (anim.Cues[i].Kind == CueKind.Move) { move = anim.Cues[i]; break; }
        if (move != null && move.Age < MoveShotEnd)
        {
            if (move.OnSelf) return (Close(places, move.FromSide, move.FromSlot, 2.9f), ShotKind.Self, move);
            float cut = move.Category == MoveCategory.Status ? 0.3f : CutTime;
            if (move.Age < cut) return (Close(places, move.FromSide, move.FromSlot, 3.0f), ShotKind.Attacker, move);
            var targets = TargetsOf(anim, move);
            var target = TargetShot(places, targets, move.Category == MoveCategory.Status ? 3.4f : 3.0f);
            if (move.Critical && move.Age >= BattleAnimator.ImpactTime && move.Age < BattleAnimator.ImpactTime + 0.4f)
                return (TargetShot(places, targets, 2.1f), ShotKind.Critical, move);
            return (target, ShotKind.Target, move);
        }

        // A Pokémon coming out of its ball
        for (int side = 0; side < 2; side++)
        {
            float best = -1f;
            int slots = 0, first = -1;
            for (int slot = 0; slot < anim.Slots; slot++)
            {
                float age = anim[(BattleSide)side, slot].SendOutAge;
                if (age < 0f) continue;
                slots++;
                if (first < 0) first = slot;
                best = Math.Max(best, age);
            }
            if (slots == 0 || best >= BattleAnimator.SendOutTime - 0.1f) continue;
            var s = (BattleSide)side;
            if (slots == 1) return (Close(places, s, first, 3.6f, s == BattleSide.Player ? 0.35f : 0f), ShotKind.SendOut, null);
            var both = new List<(BattleSide, int)> { (s, 0), (s, 1) };
            return (TargetShot(places, both, 3.6f), ShotKind.SendOut, null);
        }

        // A Pokémon fainting
        for (int side = 0; side < 2; side++)
            for (int slot = 0; slot < anim.Slots; slot++)
            {
                var v = anim[(BattleSide)side, slot];
                if (v.FaintAge >= 0f && v.FaintDelay <= 0f)
                    return (Close(places, (BattleSide)side, slot, 3.4f, (BattleSide)side == BattleSide.Player ? 0.4f : 0f), ShotKind.Faint, null);
            }

        if (anim.Time < 1.8f) return (Intro(anim.Time), ShotKind.Intro, null);
        return (Overview(anim.Time), ShotKind.Overview, null);
    }

    /// <summary>Every place the newest move is landing on (a move that hits both foes has a cue for each).</summary>
    private static List<(BattleSide Side, int Slot)> TargetsOf(BattleAnimator anim, EffectCue move)
    {
        var list = new List<(BattleSide, int)>();
        foreach (var c in anim.Cues)
            if (c.Kind == CueKind.Move && c.Move == move.Move && c.FromSide == move.FromSide && c.FromSlot == move.FromSlot
                && MathF.Abs(c.Age - move.Age) < 0.05f && !c.OnSelf && !list.Contains((c.ToSide, c.ToSlot)))
                list.Add((c.ToSide, c.ToSlot));
        if (list.Count == 0) list.Add((move.ToSide, move.ToSlot));
        return list;
    }

    /// <summary>A shot of one place or of several (framed together, from the side of the first).</summary>
    private static CameraShot TargetShot(FxPlaces places, List<(BattleSide Side, int Slot)> targets, float scale)
    {
        if (targets.Count == 1) return Close(places, targets[0].Side, targets[0].Slot, scale);
        var focus = Vector3.Zero;
        float h = 0f, spread = 0f;
        foreach (var (side, slot) in targets)
        {
            focus += places.Body(side, slot);
            h = Math.Max(h, places.Height(side, slot));
        }
        focus /= targets.Count;
        foreach (var (side, slot) in targets) spread = Math.Max(spread, Vector3.Distance(places.Body(side, slot), focus));
        bool allPlayer = targets.TrueForAll(t => t.Side == BattleSide.Player);
        bool allEnemy = targets.TrueForAll(t => t.Side == BattleSide.Enemy);
        if (!allPlayer && !allEnemy) return Overview(0f);
        return Frame(focus, Math.Max(h * scale, spread * 3.2f), allEnemy ? -0.18f : -0.75f, 0.04f,
            limitZ: allEnemy ? FoeShotLimit : float.PositiveInfinity);
    }

    /// <summary>Follows a thrown ball: the trainer's throw, the flight to the foe, close on it while it wobbles.</summary>
    private static (CameraShot, ShotKind, EffectCue?) BallShot(BattleAnimator anim, BallThrowView ball, FxPlaces places)
    {
        var (phase, progress, _) = BattleAnimator.BallStage(ball.Age, ball.Shakes);
        switch (phase)
        {
            case BallPhase.RunIn:
                return (ThrowShot(anim.Time), ShotKind.Throw, null);
            case BallPhase.Flight:
            {
                // From behind the thrower, turning to where the ball is going and zooming in as it gets there (not
                // so far that its arc leaves the frame): from in front of the player's platform the ball would come
                // from behind the camera
                var from = ThrowShot(anim.Time);
                float h = places.Height(BattleSide.Enemy, ball.Slot);
                var to = BattleBall.CaptureOpen(places, ball.Slot);
                float fit = 2f * MathF.Atan(h * 4.5f / (2f * Vector3.Distance(from.Position, to))) * 180f / MathF.PI;
                float zoom = progress * progress * (3f - 2f * progress);
                return (new CameraShot(from.Position, to, OverviewFov + (Math.Clamp(fit, 15f, OverviewFov) - OverviewFov) * zoom), ShotKind.Throw, null);
            }
            case BallPhase.Open:
            case BallPhase.Drop:
            case BallPhase.Burst:
                return (Close(places, BattleSide.Enemy, ball.Slot, 3.0f), ShotKind.Ball, null);
            default:
            {
                // Close on the ball lying on the platform
                var feet = places.Feet(BattleSide.Enemy, ball.Slot);
                return (Frame(feet + Vector3.UnitY * 0.35f, 2.0f, -0.12f, 0.08f, limitZ: FoeShotLimit), ShotKind.BallClose, null);
            }
        }
    }
}
