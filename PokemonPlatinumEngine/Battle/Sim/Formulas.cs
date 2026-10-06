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
    public static int BaseDamage(int level, int power, int attack, int defense, bool burned, bool spread) =>
        BaseDamage(level, power, attack, defense, burned, spread, screened: false, sharedScreen: false, BattleWeather.None, PokemonType.Normal,
            dimmedBeam: false, flashFire: false);

    /// <summary>
    /// The same with the field in it, each in the original's place: after a burn's halving a screen halves the
    /// hit (two thirds when two Pokémon stand behind it); after the cut for hitting several, rain halves Fire and
    /// adds half to Water and the sun does the opposite; Solar Beam is halved under any sky but a clear or a
    /// sunny one; Flash Fire adds half to a Fire move; and only then the 2.
    /// </summary>
    public static int BaseDamage(int level, int power, int attack, int defense, bool burned, bool spread, bool screened, bool sharedScreen,
        BattleWeather weather, PokemonType moveType, bool dimmedBeam, bool flashFire)
    {
        int damage = attack * power * (level * 2 / 5 + 2) / Math.Max(1, defense) / 50;
        if (burned) damage /= 2;
        if (screened) damage = sharedScreen ? damage * 2 / 3 : damage / 2;
        if (spread) damage = damage * 3 / 4;
        if (weather == BattleWeather.Rain)
        {
            if (moveType == PokemonType.Fire) damage /= 2;
            else if (moveType == PokemonType.Water) damage = damage * 15 / 10;
        }
        if (dimmedBeam) damage /= 2;
        if (weather == BattleWeather.Sun)
        {
            if (moveType == PokemonType.Fire) damage = damage * 15 / 10;
            else if (moveType == PokemonType.Water) damage /= 2;
        }
        if (flashFire) damage = damage * 15 / 10;
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

    /// <summary>
    /// What one Pokémon gets of it: half as much again with a Lucky Egg, again from a trainer's Pokémon, and again
    /// for a Pokémon that came from another trainer (<c>BattleSystem_PokemonIsOT</c>; seven tenths more for one
    /// from a game in another language, which this game has none of).
    /// </summary>
    public static int ExpFor(int share, bool luckyEgg, bool trainerBattle, bool traded = false)
    {
        if (luckyEgg) share = share * 150 / 100;
        if (trainerBattle) share = share * 150 / 100;
        if (traded) share = share * 150 / 100;
        return share;
    }

    /// <summary>
    /// The modern rules' EXP (Generation 7 on): the foe's base EXP times its level over 5, halved for a Pokémon
    /// that didn't fight, scaled by <c>((2L + 10) / (L + Lp + 10))^2.5</c> (L the foe's level, Lp the gainer's),
    /// plus one; then half as much again for one from another trainer, again with a Lucky Egg, and a fifth more
    /// for one past the level it would evolve at. Each step is rounded down.
    /// </summary>
    public static int ScaledExp(int baseExp, int foeLevel, int level, bool fought, bool traded, bool luckyEgg, bool pastEvolution)
    {
        double scale = Math.Pow((2.0 * foeLevel + 10) / (foeLevel + level + 10), 2.5);
        int exp = (int)Math.Floor(baseExp * foeLevel / 5.0 / (fought ? 1 : 2) * scale) + 1;
        if (traded) exp = exp * 150 / 100;
        if (luckyEgg) exp = exp * 150 / 100;
        if (pastEvolution) exp = exp * 120 / 100;
        return exp;
    }

    /// <summary>
    /// <c>BattleSystem_CalcMoneyPenalty</c>: what losing costs. The team's highest level, times 4, times the badges'
    /// step (2, 4, 6, 9, 12, 16, 20, 25, 30 for none to eight), and never more than the player has.
    /// </summary>
    public static int MoneyPenalty(int highestLevel, int badges, int money)
    {
        int[] steps = { 2, 4, 6, 9, 12, 16, 20, 25, 30 };
        int penalty = highestLevel * 4 * steps[Math.Clamp(badges, 0, steps.Length - 1)];
        return Math.Min(penalty, Math.Max(0, money));
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

    private static bool HasType(Pokemon p, PokemonType type) => p.HasType(type);
}

/// <summary>Where a battle is fought, as far as a rule asks (the Dive and Dusk Balls today).</summary>
/// <summary>
/// The ground a battle is fought on, in the original's terms (<c>enum BattleTerrain</c>): what the Dive and
/// Dusk Balls, Camouflage, Nature Power and Secret Power go by. The game picks it from the tile under the player
/// and the area's battle background (<c>GameEngine.TerrainAt</c>). <see cref="Special"/> stands for the
/// League's rooms, the Distortion World and the Battle Frontier, which the original lists one by one; a puddle
/// and a bridge are in its tables but nothing in Platinum picks them.
/// </summary>
public enum BattleTerrain { Plain, Sand, Grass, Puddle, Mountain, Cave, Snow, Water, Ice, Building, GreatMarsh, Bridge, Special }

/// <summary>What a battle's rules need to know of the world outside it.</summary>
public sealed class BattleConditions
{
    public BattleTerrain Terrain { get; init; }

    /// <summary>It is night or late night by the game's clock.</summary>
    public bool Night { get; init; }

    /// <summary>Whether the player has caught one of a species before (the Repeat Ball).</summary>
    public Func<PokemonSpecies, bool> HasCaught { get; init; } = _ => false;

    /// <summary>The weather of the place: a battle fought under it opens with it, and it stays.</summary>
    public BattleWeather Weather { get; init; }

    /// <summary>The place bends the order of things: the battle opens with five turns of Trick Room.</summary>
    public bool TrickRoom { get; init; }

    /// <summary>How many badges the player has: what a traded Pokémon obeys by, and what losing costs (plan 06 · R10).</summary>
    public int Badges { get; init; }

    /// <summary>The player's money, which a loss takes a share of.</summary>
    public int Money { get; init; }

    /// <summary>Who the player is, as a Pokémon's original trainer is marked: one marked otherwise is someone else's. Null takes every Pokémon for the player's own.</summary>
    public TrainerMark? Player { get; init; }
}
