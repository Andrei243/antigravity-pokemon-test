using System;
using System.Collections.Generic;
using System.Numerics;
using Raylib_cs;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// The Battle Royal's four quarters (plan 45 · BC7; style guide, "The battle's manners", "Formats and the rest"): GPU-free
/// geometry in the 1920×1080 layout units the interface lays out in. The picture is cut into four upright columns of
/// 480 units, left to right the three foes (by their places) and then the player, each a camp seen from the front. A
/// quarter comes in from its own middle line (<c>amount</c> 0 is a line, 1 is its whole column) and the director's own
/// shot shows between them until they meet. Quarters 0 and 2 are drawn into the foe's target of the halves and 1 and 3
/// into the player's, so no two quarters of a target overlap.
/// </summary>
internal static class RoyalBand
{
    /// <summary>The camps of a Royal: the player's and three others'.</summary>
    public const int Camps = 4;

    public const float Width = SplitBand.Width, Height = SplitBand.Height;

    /// <summary>A quarter's width.</summary>
    public const float Column = Width / Camps;

    /// <summary>The quarter that is the player's: the last, at the right.</summary>
    public const int Own = Camps - 1;

    /// <summary>The middle line of a quarter.</summary>
    public static float Middle(int quarter) => Column * (quarter + 0.5f);

    /// <summary>A quarter's left and right edges when whole.</summary>
    public static float Left(int quarter) => Column * quarter;
    public static float Right(int quarter) => Column * (quarter + 1);

    /// <summary>Which of the halves' two targets a quarter is drawn into: true, the foe's (the quarters 0 and 2), else the player's.</summary>
    public static bool InFoeTarget(int quarter) => quarter % 2 == 0;

    /// <summary>A quarter when it has come <paramref name="amount"/> of the way in: its column narrowed to that share about its middle line, the whole picture's height; none at nought.</summary>
    public static List<Vector2> Region(int quarter, float amount)
    {
        float half = Column / 2f * Math.Clamp(amount, 0f, 1f);
        if (half <= 0f) return new List<Vector2>();
        float x0 = Middle(quarter) - half, x1 = Middle(quarter) + half;
        return new List<Vector2> { new(x0, 0f), new(x1, 0f), new(x1, Height), new(x0, Height) };
    }

    /// <summary>Whether a point lies in a quarter's whole column, a margin inside its edges.</summary>
    public static bool InColumn(int quarter, Vector2 p, float margin = 0f) => p.X >= Left(quarter) + margin && p.X <= Right(quarter) - margin;

    /// <summary>
    /// Where a quarter's subject's middle may stand in the picture, the first that holds it taken: the foes' quarters low,
    /// clear of the foes' column of HP boxes at the top left; the first two lower than the third; the player's high,
    /// clear of its own HP box.
    /// </summary>
    public static Vector2[] Anchors(int quarter)
    {
        float x = Middle(quarter);
        return quarter switch
        {
            0 or 1 => new[] { new Vector2(x, 600f), new Vector2(x, 560f), new Vector2(x, 640f) },
            2 => new[] { new Vector2(x, 470f), new Vector2(x, 420f), new Vector2(x, 520f) },
            _ => new[] { new Vector2(x, 420f), new Vector2(x, 370f), new Vector2(x, 330f) },
        };
    }
}

internal sealed partial class BattleCamera
{
    // ------------------------------------------------------------------ the Battle Royal in four quarters (plan 45 · BC7)

    /// <summary>
    /// Whether a Battle Royal stands whole: the three others' places and the player's all out. Only then is the picture
    /// split in quarters; with a camp gone it is split in halves as any battle is.
    /// </summary>
    public static bool RoyalStands(BattleAnimator anim)
    {
        if (!anim.Royal || anim.SlotsOf(BattleSide.Enemy) < 3 || anim.SlotsOf(BattleSide.Player) < 1) return false;
        for (int slot = 0; slot < 3; slot++)
            if (!anim[BattleSide.Enemy, slot].Present) return false;
        return anim[BattleSide.Player, 0].Present;
    }

    /// <summary>The Royal's four quarters framed for who stands where now, each <paramref name="amount"/> of the way in.</summary>
    public static SplitShot RoyalOf(BattleAnimator anim, FxPlaces places, CameraManner? manner, float amount)
    {
        var m = manner ?? CameraManners.Classic;
        var own = new List<(BattleSide Side, int Slot)> { (BattleSide.Player, 0) };
        var foes = SideOf(anim, BattleSide.Enemy);
        var panels = IdleFrame.Panels(anim.SlotsOf(BattleSide.Enemy), anim.SlotsOf(BattleSide.Player));
        var camps = new CameraShot[RoyalBand.Camps];
        for (int i = 0; i < RoyalBand.Camps - 1; i++)
        {
            var camp = new List<(BattleSide Side, int Slot)> { (BattleSide.Enemy, i) };
            camps[i] = PlacedIn(FoeFacing(places, camp, m), places, camp, i, panels);
        }
        camps[RoyalBand.Own] = PlacedIn(OwnFacing(places, own, foes, m), places, own, RoyalBand.Own, panels);
        return new SplitShot(camps[0], camps[RoyalBand.Own], Math.Clamp(amount, 0f, 1f), camps);
    }

    /// <summary>
    /// A quarter's shot turned where it stands until its subject is seen at the first of the quarter's anchors
    /// (<see cref="RoyalBand.Anchors"/>) where it stands wholly in its column, a band's half-width clear of the edges, off the
    /// HP boxes and above the menu; its lens widened two degrees at a time up to <see cref="WidestFov"/> where none does.
    /// </summary>
    public static CameraShot PlacedIn(CameraShot shot, FxPlaces places, List<(BattleSide Side, int Slot)> group, int quarter, List<Rectangle> panels)
    {
        var middle = Group(places, group).Middle;
        var anchors = RoyalBand.Anchors(quarter);
        for (float fov = shot.Fov; ; fov = Math.Min(WidestFov, fov + 2f))
        {
            foreach (var anchor in anchors)
            {
                var tried = Aimed(shot with { Fov = fov }, middle, anchor);
                if (QuarterHolds(tried, places, group, quarter, panels)) return tried;
            }
            if (fov >= WidestFov) return Aimed(shot with { Fov = fov }, middle, anchors[0]);
        }
    }

    /// <summary>Whether every one of a group stands wholly in a quarter through a shot: a band's half-width inside the column's edges, off the panels, above the menu.</summary>
    public static bool QuarterHolds(CameraShot shot, FxPlaces places, List<(BattleSide Side, int Slot)> group, int quarter, List<Rectangle> panels)
    {
        float inset = SplitBand.BandWidth / 2f;
        foreach (var (side, slot) in group)
        {
            if (IdleFrame.BoxOf(shot, places, side, slot) is not { } r || !IdleFrame.Holds(r, panels)) return false;
            if (!RoyalBand.InColumn(quarter, new Vector2(r.X, r.Y), inset) || !RoyalBand.InColumn(quarter, new Vector2(r.X + r.Width, r.Y), inset)) return false;
        }
        return true;
    }

    // ------------------------------------------------------------------ an ally hopping in (plan 45 · BC7)

    /// <summary>How much of the foes' height the ally's shot frames, a send-out's.</summary>
    public const float AllyFrame = 3.6f;

    /// <summary>
    /// The shot of an ally answering a call for help while it hops onto its place: the foes' side that is out, from the
    /// front, framed as a send-out's. Null while nobody lands (a rotation battle's front seat hops on its own shot).
    /// </summary>
    public static CameraShot? AllyNow(BattleAnimator anim, FxPlaces places)
    {
        if (anim.Turntables) return null;
        bool landing = false;
        for (int slot = 0; slot < anim.SlotsOf(BattleSide.Enemy); slot++)
            if (anim[BattleSide.Enemy, slot] is { Present: true, ArriveAge: >= 0f }) landing = true;
        if (!landing) return null;
        return TargetShot(places, SideOf(anim, BattleSide.Enemy), AllyFrame);
    }
}
