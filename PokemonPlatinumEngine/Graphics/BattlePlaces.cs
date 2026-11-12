using System;
using System.Numerics;
using PokemonPlatinumEngine.Battle;

namespace PokemonPlatinumEngine.Graphics;

/// <summary>
/// Where each place of a battle stands on its side's platform and how much its Pokémon is scaled there (style guide,
/// "Stage", "Places on a platform"; plan 14 · B4). GPU-free: the renderer, the effects and the tests read the same
/// numbers. One place stands at the platform's middle; two stand left and right of it, as Platinum has them; three to
/// five (a horde's wild side) stand on an arc across the platform whose middle lies furthest from the other side, so
/// every one of them is seen from the overview over the ones before it.
/// </summary>
internal static class BattlePlaces
{
    /// <summary>How far across its platform's radius an arc's ends stand.</summary>
    public const float ArcReach = 0.9f;

    /// <summary>How far back, in the platform's radius, an arc's middle stands from its ends.</summary>
    public const float ArcDepth = 0.5f;

    /// <summary>
    /// How much a Pokémon is scaled on a platform it shares with <paramref name="slots"/> - 1 others: whole alone,
    /// 0.78 beside one, 0.6 of three, 0.52 of four and 0.45 of five.
    /// </summary>
    public static float Fit(int slots) => slots switch
    {
        <= 1 => 1f,
        2 => 0.78f,
        3 => 0.6f,
        4 => 0.52f,
        _ => 0.45f
    };

    /// <summary>
    /// Where a place stands: the middle of its side's platform alone; of two, left and right of the middle (the
    /// player's first on the left, the foe's first on the right, as in Platinum); of three to five, on the arc, the
    /// first at the end the first of two stands on and the rest in order across to the other end. The player's side has
    /// three only in a triple battle (plan 14 · B6), and those stand in a straight row across the arc's ends.
    /// </summary>
    public static Vector3 Spot(BattleSide side, int slot, int slots)
    {
        bool player = side == BattleSide.Player;
        var spot = player ? BattleStage.PlayerSpot : BattleStage.EnemySpot;
        if (slots < 2) return spot;
        float radius = player ? BattleStage.PlayerPlatformRadius : BattleStage.EnemyPlatformRadius;
        float sign = player ? -1f : 1f;
        if (slots == 2) return spot + Vector3.UnitX * sign * (slot == 0 ? 1f : -1f) * radius * 0.5f;
        // Across the arc from the first's end (t = 1) to the other (t = -1); back is away from the other side
        float t = 1f - 2f * Math.Clamp(slot, 0, slots - 1) / (slots - 1);
        // The player's own three of a triple battle (plan 14 · B6) stand in a row across their platform, so that seen
        // from behind none is in front of another and each faces the foe across from it
        if (player) return spot + Vector3.UnitX * sign * t * radius * ArcReach;
        float back = -1f;
        return spot + new Vector3(sign * t * radius * ArcReach, 0f, back * radius * ArcDepth * (1f - t * t));
    }

    /// <summary>
    /// Where a place stands on a side opened out <paramref name="spread"/> of the way to its places (plan 14 · B5): an SOS
    /// battle's wild side stands as one place, its Pokémon whole in the middle, until its first ally comes, and opens out
    /// to two as the ally lands. At a spread of 1 it is <see cref="Spot(BattleSide, int, int)"/>, to the bit.
    /// </summary>
    public static Vector3 Spot(BattleSide side, int slot, int slots, float spread) =>
        spread >= 1f ? Spot(side, slot, slots) : Vector3.Lerp(Spot(side, slot, 1), Spot(side, slot, slots), Math.Clamp(spread, 0f, 1f));

    /// <summary>
    /// The Battle Royal's ring (plan 14, finishing B13; style guide, "Stage", "A Battle Royal's ring"): the other side's
    /// three each stand on a platform of their own round the field with the player's, seen from the overview left to
    /// right as the third, the second and the first: the first on the arena's own platform (the foe's right, where a
    /// single's foe stands), the second on a small platform of the ring's own further back in the middle of the picture,
    /// and the third on another nearer, toward the player's left and clear of the player's own Pokémon.
    /// </summary>
    public static readonly Vector3[] RingSpots = { BattleStage.EnemySpot, new(-3.6f, 0f, -3f), new(-6.6f, 0f, 4f) };

    /// <summary>The radius of the ring's two platforms of its own.</summary>
    public const float RingPlatformRadius = 1.5f;

    /// <summary>How much a Pokémon of the ring is scaled: each stands alone on its platform, a little under a single's size.</summary>
    public const float RingFit = 0.65f;

    /// <summary>The ring's middle: the four platforms' spots taken together, which the two at the sides turn toward.</summary>
    public static readonly Vector3 RingMiddle = (RingSpots[0] + RingSpots[1] + RingSpots[2] + BattleStage.PlayerSpot) / 4f;

    /// <summary>Whether a side's places stand in the ring: the other side of a Battle Royal with its three.</summary>
    public static bool InTheRing(BattleSide side, int slots, bool royal) => royal && side == BattleSide.Enemy && slots == 3;

    /// <summary>Where a place stands, in the ring when the battle is a Royal (<see cref="RingSpots"/>), else as <see cref="Spot(BattleSide, int, int)"/>.</summary>
    public static Vector3 Spot(BattleSide side, int slot, int slots, bool royal) =>
        InTheRing(side, slots, royal) ? RingSpots[Math.Clamp(slot, 0, 2)] : Spot(side, slot, slots);

    /// <summary>How much a Pokémon is scaled, in the ring when the battle is a Royal (<see cref="RingFit"/>).</summary>
    public static float Fit(int slots, bool royal, BattleSide side) => InTheRing(side, slots, royal) ? RingFit : Fit(slots);

    /// <summary>
    /// How far a Pokémon of the ring is turned from how it faces on the arena's platform: half way toward the ring's
    /// middle as it stands, which is nothing for the first, on the arena's own platform.
    /// </summary>
    public static float RingTurn(int slot)
    {
        float Toward(Vector3 from) => MathF.Atan2(RingMiddle.X - from.X, RingMiddle.Z - from.Z);
        return 0.5f * (Toward(RingSpots[Math.Clamp(slot, 0, 2)]) - Toward(BattleStage.EnemySpot));
    }

    /// <summary>How much a Pokémon is scaled on a side opened out <paramref name="spread"/> of the way (plan 14 · B5): <see cref="Fit(int)"/> at a spread of 1.</summary>
    public static float Fit(int slots, float spread) =>
        spread >= 1f ? Fit(slots) : Fit(1) + (Fit(slots) - Fit(1)) * Math.Clamp(spread, 0f, 1f);
}

/// <summary>
/// An ally's hop onto its place in an SOS battle (plan 14 · B5; style guide, "Stage", "An ally answers"), GPU-free: a
/// function of the seconds since it began. It comes from behind its place, away from the other side, in one arc, a
/// little smaller at first, and lands at <see cref="Battle.BattleAnimator.ArriveTime"/>.
/// </summary>
internal static class BattleAllies
{
    /// <summary>How far behind its place it starts, in its sprite frame's width.</summary>
    public const float HopFrom = 0.4f;

    /// <summary>How high the arc rises at its top, in its sprite frame's width.</summary>
    public const float HopHeight = 0.25f;

    /// <summary>Of the way still to come (1 at the start, 0 on landing), the arc's height (0 at both ends, 1 at its top) and its size.</summary>
    public static (float Back, float Height, float Grow) Hop(float age)
    {
        float t = Math.Clamp(age / Battle.BattleAnimator.ArriveTime, 0f, 1f);
        float eased = 1f - (1f - t) * (1f - t) * (1f - t);
        return (1f - eased, 4f * t * (1f - t), 0.85f + 0.15f * eased);
    }
}
