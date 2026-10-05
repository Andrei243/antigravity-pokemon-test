using System;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle.Sim;

/// <summary>
/// Platinum's own arithmetic, taken from the decompilation's battle code (<c>src/battle/</c>). It works in whole
/// numbers and rounds down after every step, and the order of the steps is part of the rule: two bonuses of a
/// half are not one bonus of five quarters. Everything here is a pure function, so a test can hold a number
/// against the original's by hand.
/// </summary>
public static class Formulas
{
    // sStatStageBoosts (battle_lib.c): a stat at stage −6 … +6 is × numerator / denominator
    private static readonly (int Num, int Den)[] StatStages =
    {
        (10, 40), (10, 35), (10, 30), (10, 25), (10, 20), (10, 15), (10, 10), (15, 10), (20, 10), (25, 10), (30, 10), (35, 10), (40, 10)
    };

    // HitRateByStage (include/data/hit_rate_stages.h): accuracy minus evasion, −6 … +6
    private static readonly (int Num, int Den)[] HitRates =
    {
        (33, 100), (36, 100), (43, 100), (50, 100), (60, 100), (75, 100), (1, 1), (133, 100), (166, 100), (2, 1), (233, 100), (133, 50), (3, 1)
    };

    /// <summary>A stat after its stage (−6 to +6).</summary>
    public static int Staged(int stat, int stage)
    {
        var (num, den) = StatStages[Math.Clamp(stage, -6, 6) + 6];
        return stat * num / den;
    }

    /// <summary>
    /// A value times a bonus, as the original writes its bonuses: in hundredths, rounded down
    /// (<c>× 150 / 100</c>, <c>× (100 + power) / 100</c>). The hooks of abilities and items give their bonus as a
    /// plain factor (1.5, 1.2, 0.5); this is where it becomes the whole-number step.
    /// </summary>
    public static int Scale(int value, float factor) => factor == 1f ? value : (int)((long)value * (int)MathF.Round(factor * 100f) / 100);

    /// <summary>
    /// <c>BattleSystem_Divide</c>: a division that never rounds a hit away to nothing (anything but zero stays at
    /// least one).
    /// </summary>
    public static int Divide(int dividend, int divisor)
    {
        if (dividend == 0) return 0;
        int result = dividend / divisor;
        return result == 0 ? Math.Sign(dividend) : result;
    }

    /// <summary>
    /// The heart of <c>BattleSystem_CalcMoveDamage</c>: the attacking stat times the power times
    /// (2 × level / 5 + 2), over the defending stat, over 50; halved by a burn, cut to three quarters when the
    /// move hits more than one Pokémon; plus 2.
    /// </summary>
    public static int BaseDamage(int level, int power, int attack, int defense, bool burned, bool spread)
    {
        int damage = attack * power * (level * 2 / 5 + 2) / Math.Max(1, defense) / 50;
        if (burned) damage /= 2;
        if (spread) damage = damage * 3 / 4;
        return damage + 2;
    }

    /// <summary>
    /// <c>BattleSystem_CalcDamageVariance</c>: a hit is 100 hundredths of itself less the roll (0 to 15), and never
    /// less than one.
    /// </summary>
    public static int Variance(int damage, int roll)
    {
        if (damage == 0) return 0;
        return Math.Max(1, damage * (100 - roll) / 100);
    }

    /// <summary>
    /// The hit rate of a move before abilities and items (<c>BattleControllerPlayer_CheckMoveHitAccuracy</c>):
    /// its accuracy by the user's accuracy stage less the target's evasion stage. A move hits when a roll of 100
    /// comes out below the final rate.
    /// </summary>
    public static int HitRate(int accuracy, int accuracyStage, int evasionStage)
    {
        var (num, den) = HitRates[Math.Clamp(accuracyStage - evasionStage, -6, 6) + 6];
        return accuracy * num / den;
    }

    /// <summary>
    /// <c>Battler_CanEscape</c>: a Pokémon as fast as the foe always gets away. A slower one gets away when
    /// speed × 128 / the foe's speed + 30 for each earlier try, kept to one byte as the original keeps it, is more
    /// than a roll of 256.
    /// </summary>
    public static bool Escapes(int speed, int foeSpeed, int earlierTries, int roll)
    {
        if (speed >= foeSpeed) return true;
        int escape = (speed * 128 / Math.Max(1, foeSpeed) + earlierTries * 30) & 0xFF;
        return escape > roll;
    }

    /// <summary>
    /// <c>BtlCmd_CalcExpGain</c>: the foe's base EXP times its level over 7, shared among the Pokémon that fought
    /// it and are still standing. With an Exp. Share in the party half goes to those who fought and half to the
    /// holders. Nobody's share is less than one.
    /// </summary>
    public static (int Fought, int Shared) ExpShares(int baseExp, int level, int fought, int holders)
    {
        int exp = baseExp * level / 7;
        if (holders > 0)
            return (fought > 0 ? Math.Max(1, exp / 2 / fought) : 0, Math.Max(1, exp / 2 / holders));
        return (fought > 0 ? Math.Max(1, exp / fought) : 0, 0);
    }

    /// <summary>What one Pokémon gets of it: half as much again with a Lucky Egg, and again from a trainer's Pokémon.</summary>
    public static int ExpFor(int share, bool luckyEgg, bool trainerBattle)
    {
        if (luckyEgg) share = share * 150 / 100;
        if (trainerBattle) share = share * 150 / 100;
        return share;
    }

    /// <summary>
    /// The first half of <c>BattleScript_CalcCatchShakes</c>: the catch rate from the species' rate, the ball
    /// (in tenths), how hurt the Pokémon is and its condition. 255 or more is a certain catch.
    /// </summary>
    public static int CatchRate(int speciesRate, int ballTenths, int maxHp, int hp, StatusCondition status)
    {
        long rate = (long)(speciesRate * ballTenths / 10) * (maxHp * 3 - hp * 2) / (maxHp * 3);
        if (status is StatusCondition.Sleep or StatusCondition.Freeze) rate *= 2;
        if (status is StatusCondition.Poison or StatusCondition.Toxic or StatusCondition.Burn or StatusCondition.Paralyze) rate = rate * 15 / 10;
        return (int)Math.Min(rate, int.MaxValue);
    }

    /// <summary>
    /// The second half: what each of the four shakes is rolled against (a roll of 65,536 below it holds).
    /// (255 × 65,536 / rate), its square root taken twice, each time rounded down, then 1,048,560 over that.
    /// </summary>
    public static int ShakeRate(int catchRate)
    {
        uint root = WholeRoot(WholeRoot((0xFFu << 16) / (uint)Math.Max(1, catchRate)));
        return (int)((0xFFFFu << 4) / Math.Max(1u, root));
    }

    // The console's divider gives a square root in whole numbers
    private static uint WholeRoot(uint n) => (uint)Math.Floor(Math.Sqrt(n));

    /// <summary>
    /// A ball's strength in tenths (<c>sBasicBallMod</c> and the special balls of
    /// <c>BattleScript_CalcCatchShakes</c>). A Master Ball is not here: it catches whatever the rolls say.
    /// </summary>
    public static int BallTenths(string ball, Pokemon target, int turn, BattleConditions conditions) => ball switch
    {
        "Ultra Ball" => 20,
        "Great Ball" or "Safari Ball" => 15,
        "Net Ball" when HasType(target, PokemonType.Water) || HasType(target, PokemonType.Bug) => 30,
        "Dive Ball" when conditions.Terrain == BattleTerrain.Water => 35,
        "Nest Ball" when target.Level < 40 => Math.Max(10, 40 - target.Level),
        "Repeat Ball" when conditions.HasCaught(target.Species) => 30,
        "Timer Ball" => Math.Min(40, 10 + turn),
        "Dusk Ball" when conditions.Night || conditions.Terrain == BattleTerrain.Cave => 35,
        "Quick Ball" when turn < 1 => 40,
        _ => 10
    };

    private static bool HasType(Pokemon p, PokemonType type) => p.Species.PrimaryType == type || p.Species.SecondaryType == type;
}

/// <summary>Where a battle is fought, as far as a rule asks (the Dive and Dusk Balls today).</summary>
public enum BattleTerrain { Land, Water, Cave }

/// <summary>What a battle's rules need to know of the world outside it.</summary>
public sealed class BattleConditions
{
    public BattleTerrain Terrain { get; init; }

    /// <summary>It is night or late night by the game's clock.</summary>
    public bool Night { get; init; }

    /// <summary>Whether the player has caught one of a species before (the Repeat Ball).</summary>
    public Func<PokemonSpecies, bool> HasCaught { get; init; } = _ => false;
}
