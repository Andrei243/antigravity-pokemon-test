using PokemonPlatinumEngine.Core;
using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Battle;

/// <summary>Words the battle messages share, each a key of the string table (plan 17 · N2).</summary>
public static class BattleText
{
    /// <summary>A stat as a line names it: "Attack", "Sp. Atk", "accuracy".</summary>
    public static string StatName(StatType stat) => Text.Of($"battle.stat.{stat}");

    /// <summary>The condition as a noun: "cured its burn".</summary>
    public static string StatusName(StatusCondition status) => Text.Of($"battle.condition.{status}");

    /// <summary>"Foe Starly was burned!"</summary>
    public static string Inflicted(string name, StatusCondition status) => Text.Of($"battle.inflicted.{status}", ("pokemon", name));

    /// <summary>"Turtwig is already burned!"</summary>
    public static string AlreadyHas(string name, StatusCondition status) => Text.Of($"battle.alreadyHas.{status}", ("pokemon", name));

    /// <summary>"rose!", "sharply rose!", "harshly fell!"...</summary>
    public static string StageChange(int amount)
    {
        string change = amount switch
        {
            >= 3 => "roseDrastically",
            2 => "roseSharply",
            1 => "rose",
            -1 => "fell",
            -2 => "fellHarshly",
            _ => "fellSeverely"
        };
        return Text.Of($"battle.stage.{change}");
    }
}
