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
    /// first at the end the first of two stands on and the rest in order across to the other end.
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
        float back = player ? 1f : -1f;
        return spot + new Vector3(sign * t * radius * ArcReach, 0f, back * radius * ArcDepth * (1f - t * t));
    }
}
