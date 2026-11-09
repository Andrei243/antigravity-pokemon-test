using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The picture split in two (plan 45 · BC3; style guide, "The battle's manners", "The split"): the foe's side seen from
/// the front in the upper left, the player's in the lower right, each its own camera, and how far the two halves have
/// come in from their corners (0, none of them; 1, meeting at the band). A pure function of the animator's clock.
/// </summary>
internal readonly record struct SplitShot(CameraShot Foe, CameraShot Own, float In)
{
    /// <summary>Whether the halves cover the whole picture: nothing of the director's own shot shows between them.</summary>
    public bool Whole => In >= 1f;
}

/// <summary>
/// The split's band and the two halves it cuts the picture into (plan 45 · BC3): GPU-free geometry in the 1920×1080
/// layout units the interface lays out in. The band is a slant from the picture's bottom to its top, leaning to the
/// right; above it and to its left is the foe's half, below it and to its right the player's, so the HP boxes, where
/// they have always stood (the foe's at the top left, the player's at the bottom right), lie over their own halves.
/// </summary>
internal static class SplitBand
{
    public const float Width = IdleFrame.Width, Height = IdleFrame.Height;

    /// <summary>Where the band's middle line meets the picture's bottom edge and its top edge, layout units across.</summary>
    public const float BottomX = 760f, TopX = 1160f;

    /// <summary>The band's whole width (layout units): a rim of the interface's frame colour either side of a white core with a line of the selection colour down its middle.</summary>
    public const float BandWidth = 30f, CoreWidth = 18f, LineWidth = 4f;

    /// <summary>The middle of the picture, which the band's line passes through.</summary>
    public static readonly Vector2 Middle = new(Width / 2f, Height / 2f);

    /// <summary>The band's line from the bottom to the top, a unit long.</summary>
    public static readonly Vector2 Along = Vector2.Normalize(new Vector2(TopX - BottomX, -Height));

    /// <summary>The unit across the band toward the foe's half (up and to the left).</summary>
    public static readonly Vector2 TowardFoe = new(Along.Y, -Along.X);

    /// <summary>How far the band leans from upright, degrees.</summary>
    public static float LeanDegrees => MathF.Atan((TopX - BottomX) / Height) * 180f / MathF.PI;

    /// <summary>How far across the band the furthest corner of the picture stands, with the band's half-width: where a half's edge begins.</summary>
    public static float Reach
    {
        get
        {
            float most = 0f;
            foreach (var c in Corners) most = MathF.Max(most, MathF.Abs(Across(c)));
            return most + BandWidth / 2f + 2f;
        }
    }

    private static readonly Vector2[] Corners = { new(0, 0), new(Width, 0), new(Width, Height), new(0, Height) };

    /// <summary>How far a point of the picture stands across the band's line, toward the foe's half positive.</summary>
    public static float Across(Vector2 p) => Vector2.Dot(p - Middle, TowardFoe);

    /// <summary>How far each half's edge stands from the band's line when the halves have come <paramref name="amount"/> of the way in.</summary>
    public static float EdgeOffset(float amount) => (1f - Math.Clamp(amount, 0f, 1f)) * Reach;

    /// <summary>
    /// A half of the picture when the halves have come <paramref name="amount"/> of the way in: the picture's rectangle
    /// cut along the half's edge, a convex polygon (screen order) or none at all.
    /// </summary>
    public static List<Vector2> Region(bool foe, float amount)
    {
        float edge = EdgeOffset(amount);
        // The foe's half: across ≥ edge; the player's: across ≤ -edge
        float Side(Vector2 p) => foe ? Across(p) - edge : -Across(p) - edge;
        var result = new List<Vector2>(6);
        for (int i = 0; i < Corners.Length; i++)
        {
            var a = Corners[i];
            var b = Corners[(i + 1) % Corners.Length];
            float sa = Side(a), sb = Side(b);
            if (sa >= 0f) result.Add(a);
            if ((sa >= 0f) != (sb >= 0f)) result.Add(a + (b - a) * (sa / (sa - sb)));
        }
        return result.Count >= 3 ? result : new List<Vector2>();
    }

    /// <summary>Whether a point lies in a half (on its side of the half's edge).</summary>
    public static bool InRegion(bool foe, float amount, Vector2 p, float margin = 0f)
    {
        float edge = EdgeOffset(amount) + margin;
        return foe ? Across(p) >= edge : -Across(p) >= edge;
    }

    /// <summary>The smallest box of whole layout units round a polygon, inside the picture; empty for none.</summary>
    public static Rectangle Box(List<Vector2> polygon)
    {
        if (polygon.Count == 0) return new Rectangle(0, 0, 0, 0);
        float x0 = float.MaxValue, y0 = float.MaxValue, x1 = float.MinValue, y1 = float.MinValue;
        foreach (var p in polygon)
        {
            x0 = MathF.Min(x0, p.X); y0 = MathF.Min(y0, p.Y);
            x1 = MathF.Max(x1, p.X); y1 = MathF.Max(y1, p.Y);
        }
        x0 = MathF.Max(0f, MathF.Floor(x0)); y0 = MathF.Max(0f, MathF.Floor(y0));
        x1 = MathF.Min(Width, MathF.Ceiling(x1)); y1 = MathF.Min(Height, MathF.Ceiling(y1));
        return new Rectangle(x0, y0, MathF.Max(0f, x1 - x0), MathF.Max(0f, y1 - y0));
    }

    /// <summary>The area of a polygon, square layout units.</summary>
    public static float Area(List<Vector2> polygon)
    {
        float sum = 0f;
        for (int i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            sum += a.X * b.Y - b.X * a.Y;
        }
        return MathF.Abs(sum) / 2f;
    }

    /// <summary>
    /// Where a half's subjects' middle may stand in the picture, the first that holds them taken: the foe's low in the
    /// upper left, clear of its HP box (lower and further left for a horde's or a triple's column of boxes); the
    /// player's high in the lower right, clear of its own (higher for a double's two).
    /// </summary>
    public static Vector2[] Anchors(bool foe) => foe
        ? new[] { new Vector2(560f, 560f), new Vector2(540f, 620f), new Vector2(460f, 690f), new Vector2(400f, 730f) }
        : new[] { new Vector2(1420f, 420f), new Vector2(1450f, 370f), new Vector2(1500f, 320f), new Vector2(1540f, 280f) };
}

internal sealed partial class BattleCamera
{
    // ------------------------------------------------------------------ the split (plan 45 · BC3)

    /// <summary>How long the halves take to come in from their corners and to go back out, seconds of the animator's clock.</summary>
    public const float SplitIn = 0.3f, SplitOut = 0.3f;

    /// <summary>How long a split is held between coming in and going out: the leads', a last Pokémon's.</summary>
    public const float SplitHoldLeads = 1.4f, SplitHoldLast = 1.6f;

    /// <summary>How much of a half's subject's height each half frames, in a 26° lens.</summary>
    public const float SplitFrame = 3.2f;

    /// <summary>Whether a manner splits at a moment.</summary>
    public static bool SplitsAt(CameraManner manner, SplitMoment moment) => moment.Reason switch
    {
        SplitReason.Leads => (manner.Splits & CameraSplits.Leads) != 0,
        SplitReason.LastPokemon => (manner.Splits & CameraSplits.LastPokemon) != 0 || (moment.Boss && (manner.Splits & CameraSplits.BossLast) != 0),
        _ => false,
    };

    /// <summary>When a split of a moment goes out (it is held its time, or cut short by what waits on the player once it is in) and when it is gone.</summary>
    public static (float Out, float End) SplitWindow(SplitMoment moment)
    {
        float hold = moment.Reason == SplitReason.Leads ? SplitHoldLeads : SplitHoldLast;
        float outAt = moment.Start + SplitIn + hold;
        if (moment.CutShortAt >= 0f) outAt = Math.Min(outAt, Math.Max(moment.CutShortAt, moment.Start + SplitIn));
        return (outAt, outAt + SplitOut);
    }

    /// <summary>How far a split's halves have come in at <paramref name="time"/>: 0 before it and once it is gone.</summary>
    public static float SplitAmount(SplitMoment moment, float time)
    {
        var (outAt, end) = SplitWindow(moment);
        if (time < moment.Start || time >= end) return 0f;
        if (time < moment.Start + SplitIn) return Smooth((time - moment.Start) / SplitIn);
        if (time < outAt) return 1f;
        return 1f - Smooth((time - outAt) / SplitOut);
    }

    /// <summary>
    /// The split on screen now (plan 45 · BC3), or null: the newest moment the manner splits at whose window holds the
    /// animator's clock. Never under a manner that splits at nothing (Classic), under REDUCE MOTION (<see cref="Motion"/>
    /// under a half), or with the Battle Scene off (<paramref name="scene"/>).
    /// </summary>
    public static SplitShot? SplitNow(BattleAnimator anim, FxPlaces places, CameraManner? manner, bool scene = true)
    {
        var m = MannerNow(manner);
        if (!scene || m.Splits == CameraSplits.None || Motion < 0.5f) return null;
        var moments = anim.SplitMoments;
        for (int i = moments.Count - 1; i >= 0; i--)
        {
            var moment = moments[i];
            if (!SplitsAt(m, moment)) continue;
            float amount = SplitAmount(moment, anim.Time);
            if (amount <= 0f) continue;
            return SplitOf(anim, places, m, amount);
        }
        return null;
    }

    /// <summary>The split framed for who stands where now, its halves <paramref name="amount"/> of the way in.</summary>
    public static SplitShot SplitOf(BattleAnimator anim, FxPlaces places, CameraManner? manner, float amount)
    {
        var m = manner ?? CameraManners.Classic;
        var foes = SideOf(anim, BattleSide.Enemy);
        var own = SideOf(anim, BattleSide.Player);
        var panels = IdleFrame.Panels(anim.SlotsOf(BattleSide.Enemy), anim.SlotsOf(BattleSide.Player));
        return new SplitShot(FoeSplitShot(places, foes, m, panels), OwnSplitShot(places, own, foes, m, panels), Math.Clamp(amount, 0f, 1f));
    }

    /// <summary>The foe's half: the foes from the front, close, placed in their half clear of the panels (<see cref="Placed"/>).</summary>
    public static CameraShot FoeSplitShot(FxPlaces places, List<(BattleSide Side, int Slot)> foes, CameraManner manner, List<Rectangle> panels)
    {
        var (middle, h, spread) = Group(places, foes);
        float size = Math.Max(h * SplitFrame, spread * 3.4f) / manner.Reach;
        var shot = Frame(middle, size, -0.18f + FaceTurn, 0.08f - manner.Low, IdleFov, FoeShotLimit);
        return Placed(shot, places, foes, true, panels);
    }

    /// <summary>The player's half: the player's own from the front, between the platforms looking south, placed in their half clear of the panels.</summary>
    public static CameraShot OwnSplitShot(FxPlaces places, List<(BattleSide Side, int Slot)> own, List<(BattleSide Side, int Slot)> foes, CameraManner manner,
        List<Rectangle> panels)
    {
        var d = TowardFoe(places, own, foes);
        var (middle, h, spread) = Group(places, own);
        float size = Math.Max(h * SplitFrame, spread * 3.4f) / manner.Reach;
        var from = Vector3.Transform(d, Matrix4x4.CreateRotationY(-FaceTurn));
        float lift = OwnFaceLift - manner.Low;
        var dir = Vector3.Normalize(from * MathF.Cos(lift) + Vector3.UnitY * MathF.Sin(lift));
        float dist = size / (2f * MathF.Tan(IdleFov * MathF.PI / 360f));
        return Placed(new CameraShot(middle + dir * dist, middle, IdleFov), places, own, false, panels);
    }

    /// <summary>
    /// A half's shot turned where it stands until its subjects' middle is seen at the first of the half's anchors
    /// (<see cref="SplitBand.Anchors"/>) where every one of them stands wholly in the half, a band's half-width clear of
    /// the band, off the HP boxes and above the menu; its lens widened two degrees at a time up to
    /// <see cref="WidestFov"/> where no anchor does it; the first anchor at the widest lens where nothing does.
    /// </summary>
    public static CameraShot Placed(CameraShot shot, FxPlaces places, List<(BattleSide Side, int Slot)> group, bool foe, List<Rectangle> panels)
    {
        var middle = Group(places, group).Middle;
        var anchors = SplitBand.Anchors(foe);
        for (float fov = shot.Fov; ; fov = Math.Min(WidestFov, fov + 2f))
        {
            foreach (var anchor in anchors)
            {
                var tried = Aimed(shot with { Fov = fov }, middle, anchor);
                if (HalfHolds(tried, places, group, foe, panels)) return tried;
            }
            if (fov >= WidestFov) return Aimed(shot with { Fov = fov }, middle, anchors[0]);
        }
    }

    /// <summary>Whether every one of a group stands wholly in its half through a shot: a band's half-width clear of the band, off the panels, above the menu.</summary>
    public static bool HalfHolds(CameraShot shot, FxPlaces places, List<(BattleSide Side, int Slot)> group, bool foe, List<Rectangle> panels)
    {
        foreach (var (side, slot) in group)
        {
            if (IdleFrame.BoxOf(shot, places, side, slot) is not { } r || !IdleFrame.Holds(r, panels)) return false;
            if (!SplitBand.InRegion(foe, 1f, new Vector2(r.X, r.Y), SplitBand.BandWidth / 2f)
                || !SplitBand.InRegion(foe, 1f, new Vector2(r.X + r.Width, r.Y), SplitBand.BandWidth / 2f)
                || !SplitBand.InRegion(foe, 1f, new Vector2(r.X, r.Y + r.Height), SplitBand.BandWidth / 2f)
                || !SplitBand.InRegion(foe, 1f, new Vector2(r.X + r.Width, r.Y + r.Height), SplitBand.BandWidth / 2f)) return false;
        }
        return true;
    }

    /// <summary>
    /// The shot turned where it stands until <paramref name="subject"/> is seen at <paramref name="at"/> of the picture
    /// (layout units): its look point moved across and up by what is left over, a few rounds.
    /// </summary>
    public static CameraShot Aimed(CameraShot shot, Vector3 subject, Vector2 at)
    {
        for (int round = 0; round < 8; round++)
        {
            var (seen, front) = IdleFrame.Project(shot, subject);
            if (!front) break;
            var error = at - seen;
            if (error.LengthSquared() < 0.25f) break;
            var forward = Vector3.Normalize(shot.Target - shot.Position);
            var right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
            var up = Vector3.Cross(right, forward);
            float dist = Vector3.Distance(shot.Position, shot.Target);
            float perUnit = IdleFrame.Height / 2f / (dist * MathF.Tan(shot.Fov * MathF.PI / 360f));
            shot = shot with { Target = shot.Target - right * (error.X / perUnit) + up * (error.Y / perUnit) };
        }
        return shot;
    }
}
