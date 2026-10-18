using System;
using System.Collections.Generic;
using System.Linq;
using PokemonPlatinumEngine.Battle.Sim;
using PokemonPlatinumEngine.Data;
using PokemonPlatinumEngine.Models;

namespace PokemonPlatinumEngine.Battle;

// The balls of the later games (plan 06 · R28a; docs/mechanics/rulings.md, "The later games' held items, medicine and
// balls"). Pokémon Showdown has no catching, so each bonus is the newest main games' own, as the games publish them:
// Platinum's balls with the bonuses Generation 7 on gives them, Johto's balls of HeartGold and SoulSilver as Generation
// 7 on has them, and the Dream, Beast and Hisui's balls. The bonus is still in tenths and the catch still Platinum's own
// arithmetic (Formulas.CatchRate), which these rules keep.
public static partial class CatchCalculator
{
    /// <summary>The eleven Ultra Beasts, five times likelier in a Beast Ball and ten times less in any other one.</summary>
    public static readonly IReadOnlySet<string> UltraBeasts = new HashSet<string>
    {
        "Nihilego", "Buzzwole", "Pheromosa", "Xurkitree", "Celesteela", "Kartana", "Guzzlord", "Poipole", "Naganadel", "Stakataka", "Blacephalon"
    };

    /// <summary>The balls that always hold under the later rules: Legends: Arceus's Origin Ball (its Master Ball) and the Park Ball (the park's own).</summary>
    private static bool AlwaysHolds(string ball) => ball is "Origin Ball" or "Park Ball";

    /// <summary>
    /// A ball's bonus in tenths under the later rules. Platinum's balls take the newest games' bonuses (a Net Ball 35, a
    /// Dusk Ball 30, a Quick Ball 50, a Repeat Ball 35, a Timer Ball 1229/4096 more each turn to four times, a Nest
    /// Ball 41 less the level), and every ball but the Beast Ball catches an Ultra Beast at a tenth.
    /// </summary>
    public static int LaterBallTenths(string ball, Pokemon target, int turn, BattleConditions conditions, Pokemon? mine)
    {
        bool beast = UltraBeasts.Contains(target.Species.Name);
        if (ball == "Beast Ball") return beast ? 50 : 1;
        if (beast) return 1;
        return ball switch
        {
            "Ultra Ball" => 20,
            "Great Ball" or "Safari Ball" or "Sport Ball" => 15,
            "Net Ball" when target.HasType(PokemonType.Water) || target.HasType(PokemonType.Bug) => 35,
            "Dive Ball" when conditions.Terrain == BattleTerrain.Water => 35,
            "Nest Ball" when target.Level < 30 => Math.Max(10, 41 - target.Level),
            "Repeat Ball" when conditions.HasCaught(target.Species) => 35,
            "Timer Ball" => Math.Min(40, 10 + turn * 3),
            "Dusk Ball" when conditions.Night || conditions.Terrain == BattleTerrain.Cave => 30,
            "Quick Ball" when turn < 1 => 50,
            "Dream Ball" when target.Status == StatusCondition.Sleep || target.AbilityName == "Comatose" => 40,
            "Fast Ball" when target.Species.BaseSpeed >= 100 => 40,
            "Level Ball" when mine != null => LevelBall(mine.Level, target.Level),
            "Lure Ball" when conditions.Fishing => 40,
            "Moon Ball" when EvolvesByMoonStone(target.Species) => 40,
            "Love Ball" when mine != null && LoveMatch(mine, target) => 80,
            // Hisui's: the weight and the wing balls, which their own game throws by distance; here the bonus alone
            "Leaden Ball" or "Wing Ball" => 15,
            "Gigaton Ball" or "Jet Ball" => 20,
            _ => 10
        };
    }

    /// <summary>The Level Ball (Generation 7 on): twice, four or eight times as the player's Pokémon's level is above, twice or four times the target's.</summary>
    private static int LevelBall(int mine, int theirs) =>
        mine > theirs * 4 ? 80 : mine > theirs * 2 ? 40 : mine > theirs ? 20 : 10;

    /// <summary>The Love Ball (Generation 7 on): the same species as the player's Pokémon and the other gender.</summary>
    private static bool LoveMatch(Pokemon mine, Pokemon target) =>
        mine.Species == target.Species && mine.Gender != Gender.Genderless && target.Gender != Gender.Genderless && mine.Gender != target.Gender;

    /// <summary>The Moon Ball (Generation 7 on): a species that evolves with a Moon Stone.</summary>
    private static bool EvolvesByMoonStone(PokemonSpecies species) => species.Evolutions?.Any(e => e.Item == "Moon Stone") == true;

    /// <summary>
    /// Johto's Heavy Ball (Generation 7 on) adds to the species' catch rate by the target's weight: 20 less under 100
    /// kg, nothing to 200, 20 more to 300 and 30 from there; never under 1.
    /// </summary>
    public static int HeavyBallRate(string ball, Pokemon target, int speciesRate)
    {
        if (ball != "Heavy Ball") return speciesRate;
        float kg = target.Weight;
        int bonus = kg >= 300f ? 30 : kg >= 200f ? 20 : kg >= 100f ? 0 : -20;
        return Math.Max(1, speciesRate + bonus);
    }

    /// <summary>
    /// What a ball does to the Pokémon it has just caught: a Heal Ball makes it whole (its HP, its condition and its
    /// PP), a Friend Ball sets its friendship at 200. A Luxury Ball's friendship is the field's (Models/Friendship.cs).
    /// </summary>
    public static void AfterTheCatch(Pokemon caught, ItemData ball)
    {
        switch (ball.Name)
        {
            case "Heal Ball":
                caught.CurrentHP = caught.MaxHP;
                caught.Status = StatusCondition.None;
                caught.SleepTurns = 0;
                caught.ToxicCounter = 0;
                foreach (var move in caught.Moves) move.RestorePP();
                break;
            case "Friend Ball":
                caught.Friendship = Math.Max(caught.Friendship, 200);
                break;
        }
    }

    /// <summary>Whether a ball's catch runs in full (the coverage report asks): every ball but the Strange Ball, which only Legends: Arceus's way of being brought over makes.</summary>
    public static bool KnowsBall(string ball) => ball != "Strange Ball";
}
