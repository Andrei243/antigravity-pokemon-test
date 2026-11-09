using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The part of the 1920×1080 picture the menu's panels leave free (plan 45 · BC2; style guide, "The battle's manners",
/// "The idle cycle"): the picture inside a margin, above the commands and the move cards, and off every HP box the
/// battle's format draws. An idle shot holds its subjects here. GPU-free: rectangles in layout units, as
/// <c>ModernUi.DrawBattle</c> lays its panels out.
/// </summary>
internal static class IdleFrame
{
    /// <summary>The picture's width and height in layout units.</summary>
    public const float Width = 1920f, Height = 1080f;

    /// <summary>How far in from the picture's edges a subject keeps.</summary>
    public const float Margin = 24f;

    /// <summary>Where the commands, the move cards and the message box begin: nothing of a subject below it.</summary>
    public const float MenuTop = 836f;

    /// <summary>The HP boxes of a battle with these many places a side, each a rectangle a subject keeps off.</summary>
    public static List<Rectangle> Panels(int foes, int own)
    {
        var panels = new List<Rectangle>();
        // The foes': a single's box at the top left with its tray; a double's (and an SOS battle's) two compact boxes
        // stacked there; a horde's and a triple's small boxes down the left
        if (foes <= 1) panels.Add(new Rectangle(40, 36, 620, 180));
        else if (foes == 2) panels.Add(new Rectangle(40, 24, 660, 260));
        else panels.Add(new Rectangle(36, 20, 640, 28 + (foes - 1) * 96 + 110));
        // The player's own: a single's box over the commands on the right; a double's two; a triple's three
        if (own <= 1) panels.Add(new Rectangle(1228, 640, 660, 184));
        else if (own == 2) panels.Add(new Rectangle(1228, 576, 692, 270));
        else panels.Add(new Rectangle(1300, 536, 620, 300));
        return panels;
    }

    /// <summary>Whether a box of the picture lies wholly in its free part: inside the margin, above the menu, off every panel.</summary>
    public static bool Holds(Rectangle box, List<Rectangle> panels)
    {
        if (box.X < Margin || box.Y < Margin || box.X + box.Width > Width - Margin || box.Y + box.Height > MenuTop) return false;
        foreach (var p in panels)
            if (box.X < p.X + p.Width && box.X + box.Width > p.X && box.Y < p.Y + p.Height && box.Y + box.Height > p.Y) return false;
        return true;
    }

    /// <summary>
    /// Where a point stands in the picture through a shot (layout units, y down), and whether it is in front of the
    /// lens. The scene is 16:9, as the virtual screen is.
    /// </summary>
    public static (Vector2 At, bool InFront) Project(CameraShot shot, Vector3 point)
    {
        var view = Matrix4x4.CreateLookAt(shot.Position, shot.Target, Vector3.UnitY);
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(shot.Fov * MathF.PI / 180f, Width / Height, 0.1f, 1000f);
        var clip = Vector4.Transform(new Vector4(point, 1f), view * proj);
        if (clip.W <= 0.01f) return (Vector2.Zero, false);
        float x = clip.X / clip.W, y = clip.Y / clip.W;
        return (new Vector2((x + 1f) * 0.5f * Width, (1f - y) * 0.5f * Height), true);
    }

    /// <summary>
    /// A Pokémon's box in the picture through a shot: from its feet to its height, as wide as its body either side of
    /// its middle; null when any of it stands behind the lens.
    /// </summary>
    public static Rectangle? BoxOf(CameraShot shot, FxPlaces places, BattleSide side, int slot)
    {
        var feet = places.Feet(side, slot);
        float h = places.Height(side, slot), w = places.HalfWidth(side, slot);
        var forward = Vector3.Normalize(shot.Target - shot.Position);
        var right = Vector3.Cross(forward, Vector3.UnitY);
        right = right.LengthSquared() < 1e-6f ? Vector3.UnitX : Vector3.Normalize(right);
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        foreach (float up in new[] { 0f, h })
            foreach (float across in new[] { -w, w })
                foreach (float deep in new[] { -w, w })
                {
                    var (at, front) = Project(shot, feet + Vector3.UnitY * up + right * across + forward * deep);
                    if (!front) return null;
                    x0 = MathF.Min(x0, at.X); y0 = MathF.Min(y0, at.Y);
                    x1 = MathF.Max(x1, at.X); y1 = MathF.Max(y1, at.Y);
                }
        return new Rectangle(x0, y0, x1 - x0, y1 - y0);
    }

    /// <summary>Whether a box lies wholly outside the picture (or behind the lens): out of the shot altogether.</summary>
    public static bool Outside(Rectangle? box) =>
        box is not { } b || b.X + b.Width <= 0f || b.X >= Width || b.Y + b.Height <= 0f || b.Y >= Height;
}

internal sealed partial class BattleCamera
{
    // ------------------------------------------------------------------ the menu filmed (plan 45 · BC2)

    /// <summary>How long a breathing overview takes to ease in from the plain one, seconds of the menu's open time.</summary>
    public const float BreathIn = 1.5f;

    /// <summary>How much closer the breathing overview comes at the top of a breath, a share of its reach.</summary>
    public const float BreathZoom = 0.035f;

    /// <summary>The lens of the idle cycle's close shots and how much of their subject's height each frames.</summary>
    public const float IdleFov = 26f, FoeFaceFrame = 2.6f, OwnFaceFrame = 2.8f;

    /// <summary>The shoulder shot's lens, its stand behind and beside the player's Pokémon (in its heights, at least a unit), and its eye height (in the same).</summary>
    public const float ShoulderFov = 20f, ShoulderBack = 2.2f, ShoulderSide = 0.8f, ShoulderEye = 1.05f;

    /// <summary>How much further than <see cref="Clearance"/> the shoulder shot keeps from the player's platform.</summary>
    public const float ShoulderRoom = 0.4f;

    /// <summary>How far round from straight on the foe's face and the player's own front are turned, radians.</summary>
    public const float FaceTurn = 0.28f;

    /// <summary>How far above level the player's own front looks down at it, radians: enough that the ground stands behind it.</summary>
    public const float OwnFaceLift = 0.3f;

    /// <summary>The side shot: how far it stands off the line between the platforms (world units), and its lift.</summary>
    public const float SideReach = 22f, SideLift = 0.16f;

    /// <summary>How far the side shot is turned from square across the field toward the player's side, radians.</summary>
    public const float SideTurn = 0.45f;

    /// <summary>
    /// The menu's shot under a manner that films it (plan 45 · BC2): Sinnoh's breathing overview, or a cycle's shot at the
    /// menu's open time; null where the manner leaves the menu on the overview or no menu is open on the commands or the
    /// move cards (a target, BAG, POKéMON or a question bring the overview back).
    /// </summary>
    public static (CameraShot Shot, ShotKind Kind, EffectCue? Cue)? IdleNow(BattleAnimator anim, FxPlaces places, CameraManner manner)
    {
        float age = anim.MenuAge;
        if (age < 0f) return null;
        if (manner.Cycle.Count == 0)
            return manner.Drift > 0f ? (Breathing(anim.Time, age, manner), ShotKind.Overview, null) : null;

        var shots = CycleFor(anim, places, manner);
        if (shots.Count < 2) return null;
        float step = manner.CycleHold + manner.CycleEase;
        int i = (int)MathF.Floor(age / step);
        float into = age - i * step;
        var (a, aKind) = shots[i % shots.Count];
        if (into < manner.CycleHold) return (a, aKind, null);
        var (b, bKind) = shots[(i + 1) % shots.Count];
        float t = Smooth((into - manner.CycleHold) / manner.CycleEase);
        // A shot of the player's own front is cut to and from, and so is any ease that would pass the player's platform
        // (the corridors' rule): halfway through the ease's time
        if (!Eases(a, aKind, b, bKind)) return t < 0.5f ? (a, aKind, null) : (b, bKind, null);
        return (CameraShot.Lerp(a, b, t), t < 0.5f ? aKind : bKind, null);
    }

    /// <summary>Whether the cycle eases from one shot to the next rather than cutting: never to or from the player's own front, never past the player's platform.</summary>
    public static bool Eases(CameraShot a, ShotKind aKind, CameraShot b, ShotKind bKind) =>
        aKind != ShotKind.OwnFront && bKind != ShotKind.OwnFront && !CrossesPlayerSide(a.Position, b.Position);

    /// <summary>
    /// Sinnoh's breathing overview: the overview drifting round the field by the manner's <see cref="CameraManner.Drift"/>
    /// on a slow sine of the menu's open time and coming a little closer at the top of each breath, eased in from the
    /// plain overview over <see cref="BreathIn"/>, so a menu opened and closed at once is the overview.
    /// </summary>
    public static CameraShot Breathing(float time, float menuAge, CameraManner manner)
    {
        float ramp = Smooth(menuAge / BreathIn);
        float phase = 2f * MathF.PI * menuAge / Math.Max(1f, manner.DriftPeriod);
        float angle = 0.012f * MathF.Sin(time * 0.35f) + manner.Drift * ramp * MathF.Sin(phase);
        float zoom = 1f - BreathZoom * ramp * 0.5f * (1f - MathF.Cos(phase));
        return Orbit(angle, zoom);
    }

    /// <summary>
    /// The cycle's shots for this battle, in the manner's order: each one framed for who stands where, widened up to
    /// <see cref="WidestFov"/> where its subjects don't fit the picture's free part, and left out where widening doesn't
    /// do it. The overview is always the first.
    /// </summary>
    public static List<(CameraShot Shot, ShotKind Kind)> CycleFor(BattleAnimator anim, FxPlaces places, CameraManner manner)
    {
        var list = new List<(CameraShot, ShotKind)>();
        foreach (var idle in manner.Cycle)
        {
            if (idle == IdleShot.Overview) { list.Add((Overview(anim.Time), ShotKind.Overview)); continue; }
            if (IdleShotOf(idle, anim, places, manner) is { } shot)
                list.Add((shot, idle == IdleShot.OwnFace ? ShotKind.OwnFront : ShotKind.Idle));
        }
        if (list.Count == 0 || list[0].Item2 != ShotKind.Overview) list.Insert(0, (Overview(anim.Time), ShotKind.Overview));
        return list;
    }

    /// <summary>The places of a side that stand there now (at least the first).</summary>
    private static List<(BattleSide Side, int Slot)> SideOf(BattleAnimator anim, BattleSide side)
    {
        var list = new List<(BattleSide, int)>();
        for (int slot = 0; slot < anim.SlotsOf(side); slot++) list.Add((side, slot));
        if (list.Count == 0) list.Add((side, 0));
        return list;
    }

    /// <summary>
    /// One of the cycle's shots, framed and held to the picture's free part (<see cref="IdleFrame"/>), or null where it
    /// can't be: a pure function of who stands where.
    /// </summary>
    public static CameraShot? IdleShotOf(IdleShot idle, BattleAnimator anim, FxPlaces places, CameraManner manner)
    {
        var foes = SideOf(anim, BattleSide.Enemy);
        var own = SideOf(anim, BattleSide.Player);
        var panels = IdleFrame.Panels(anim.SlotsOf(BattleSide.Enemy), anim.SlotsOf(BattleSide.Player));
        (CameraShot Shot, List<(BattleSide, int)> Subjects)? framed = idle switch
        {
            IdleShot.Shoulder => (ShoulderShot(places, own, foes, manner), foes),
            IdleShot.FoeFace => (FoeFaceShot(places, foes, manner), foes),
            IdleShot.OwnFace => (OwnFaceShot(places, own, foes, manner), own),
            IdleShot.Side => (SideShot(places, own, foes, manner), Both(own, foes)),
            _ => null,
        };
        if (framed is not { } f) return null;
        return Fitted(f.Shot, places, f.Subjects, panels);
    }

    private static List<(BattleSide, int)> Both(List<(BattleSide Side, int Slot)> a, List<(BattleSide Side, int Slot)> b)
    {
        var all = new List<(BattleSide, int)>(a);
        all.AddRange(b);
        return all;
    }

    /// <summary>
    /// The shot as it is, if its subjects all stand in the picture's free part; else its lens widened a degree at a time
    /// up to <see cref="WidestFov"/> until they do; else null.
    /// </summary>
    public static CameraShot? Fitted(CameraShot shot, FxPlaces places, List<(BattleSide Side, int Slot)> subjects, List<Rectangle> panels)
    {
        for (float fov = shot.Fov; ; fov = Math.Min(WidestFov, fov + 1f))
        {
            var tried = shot with { Fov = fov };
            if (subjects.TrueForAll(s => IdleFrame.BoxOf(tried, places, s.Side, s.Slot) is { } box && IdleFrame.Holds(box, panels)))
                return tried;
            if (fov >= WidestFov) return null;
        }
    }

    /// <summary>The middle of a group's bodies, their tallest height and how far the furthest stands from that middle.</summary>
    private static (Vector3 Middle, float Height, float Spread) Group(FxPlaces places, List<(BattleSide Side, int Slot)> group)
    {
        var middle = Vector3.Zero;
        float h = 0f;
        foreach (var (side, slot) in group)
        {
            middle += places.Body(side, slot);
            h = Math.Max(h, places.Height(side, slot));
        }
        middle /= group.Count;
        float spread = 0f;
        foreach (var (side, slot) in group)
            spread = Math.Max(spread, Vector3.Distance(places.Body(side, slot) with { Y = middle.Y }, middle) + places.HalfWidth(side, slot));
        return (middle, h, spread);
    }

    /// <summary>The flat way from the player's side to the foe's: what "toward the foe" means for the idle shots.</summary>
    private static Vector3 TowardFoe(FxPlaces places, List<(BattleSide, int)> own, List<(BattleSide, int)> foes)
    {
        var d = Group(places, foes).Middle - Group(places, own).Middle;
        d.Y = 0f;
        return d.LengthSquared() < 1e-6f ? -Vector3.UnitZ : Vector3.Normalize(d);
    }

    /// <summary>
    /// Over the shoulder: low behind the player's Pokémon and to its right, its back at the picture's lower left and the
    /// foe beyond it in the picture's upper middle.
    /// </summary>
    public static CameraShot ShoulderShot(FxPlaces places, List<(BattleSide Side, int Slot)> own, List<(BattleSide Side, int Slot)> foes, CameraManner manner)
    {
        var d = TowardFoe(places, own, foes);
        var right = Vector3.Normalize(Vector3.Cross(d, Vector3.UnitY));
        var (ownMiddle, ownH, ownSpread) = Group(places, own);
        var ownFeet = ownMiddle - Vector3.UnitY * ownH * 0.5f;
        var (foeMiddle, foeH, _) = Group(places, foes);
        float back = Math.Max(1f, ownH * ShoulderBack) * manner.Reach;
        float aside = Math.Max(0.6f, ownSpread + ownH * ShoulderSide) * manner.Reach;
        float eye = Math.Max(ownH * 0.3f, ownH * ShoulderEye - manner.Low * back);
        var at = ownFeet - d * back + right * aside + Vector3.UnitY * eye;
        // Never nearer the player's platform than the director's eases may pass it: stepped further back where it would be
        var platform = BattleStage.PlayerSpot + new Vector3(0, 1.2f, 0);
        for (int i = 0; i < 40 && Vector3.Distance(at, platform) < Clearance + ShoulderRoom; i++) at -= d * 0.1f;
        // Looking a little under the foe's middle, so it stands in the picture's upper middle
        var look = foeMiddle - Vector3.UnitY * foeH * 0.3f;
        return new CameraShot(at, look, ShoulderFov);
    }

    /// <summary>The foe's face: the foes from the front, close and turned a little, standing in front of the player's platform.</summary>
    public static CameraShot FoeFaceShot(FxPlaces places, List<(BattleSide Side, int Slot)> foes, CameraManner manner)
    {
        var (middle, h, spread) = Group(places, foes);
        float size = Math.Max(h * FoeFaceFrame, spread * 3.2f) / manner.Reach;
        return Frame(middle, size, -0.18f + FaceTurn, 0.08f - manner.Low, IdleFov, FoeShotLimit);
    }

    /// <summary>
    /// The player's own Pokémon from the front: between the two platforms looking south at it, turned a little and
    /// looking down enough that the ground stands behind it. Only ever cut to and from (<see cref="ShotKind.OwnFront"/>).
    /// </summary>
    public static CameraShot OwnFaceShot(FxPlaces places, List<(BattleSide Side, int Slot)> own, List<(BattleSide Side, int Slot)> foes, CameraManner manner)
    {
        var d = TowardFoe(places, own, foes);
        var (middle, h, spread) = Group(places, own);
        float size = Math.Max(h * OwnFaceFrame, spread * 3.2f) / manner.Reach;
        var from = Vector3.Transform(d, Matrix4x4.CreateRotationY(-FaceTurn));
        float lift = OwnFaceLift - manner.Low;
        var dir = Vector3.Normalize(from * MathF.Cos(lift) + Vector3.UnitY * MathF.Sin(lift));
        float dist = size / (2f * MathF.Tan(IdleFov * MathF.PI / 360f));
        return new CameraShot(middle + dir * dist, middle, IdleFov);
    }

    /// <summary>The side: both platforms from the east of the line between them, the two facing each other across the picture.</summary>
    public static CameraShot SideShot(FxPlaces places, List<(BattleSide Side, int Slot)> own, List<(BattleSide Side, int Slot)> foes, CameraManner manner)
    {
        var d = TowardFoe(places, own, foes);
        var right = Vector3.Normalize(Vector3.Cross(d, Vector3.UnitY));
        var (ownMiddle, ownH, _) = Group(places, own);
        var (foeMiddle, foeH, _) = Group(places, foes);
        var middle = (ownMiddle + foeMiddle) / 2f;
        middle.Y = Math.Max(ownMiddle.Y, foeMiddle.Y);
        float lift = SideLift - manner.Low;
        // Turned a little behind the player's side, so the foes stand further off with the trees behind them
        var flat = Vector3.Normalize(right * MathF.Cos(SideTurn) - d * MathF.Sin(SideTurn));
        var dir = Vector3.Normalize(flat * MathF.Cos(lift) + Vector3.UnitY * MathF.Sin(lift));
        return new CameraShot(middle + dir * SideReach * manner.Reach, middle, OverviewFov);
    }
}
