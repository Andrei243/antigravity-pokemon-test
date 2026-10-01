using PokemonPlatinumEngine.Data;

namespace PokemonPlatinumEngine.Battle;

/// <summary>Words the battle messages share.</summary>
public static class BattleText
{
    public static string StatName(StatType stat) => stat switch
    {
        StatType.Attack => "Attack",
        StatType.Defense => "Defense",
        StatType.SpAttack => "Sp. Atk",
        StatType.SpDefense => "Sp. Def",
        StatType.Speed => "Speed",
        StatType.Accuracy => "accuracy",
        StatType.Evasion => "evasiveness",
        _ => "HP"
    };

    /// <summary>The condition as a noun: "cured its burn".</summary>
    public static string StatusName(StatusCondition status) => status switch
    {
        StatusCondition.Poison or StatusCondition.Toxic => "poison",
        StatusCondition.Burn => "burn",
        StatusCondition.Paralyze => "paralysis",
        StatusCondition.Sleep => "sleep",
        StatusCondition.Freeze => "freeze",
        _ => "condition"
    };

    /// <summary>"Foe Starly was burned!"</summary>
    public static string Inflicted(string name, StatusCondition status) => status switch
    {
        StatusCondition.Poison => $"{name} was poisoned!",
        StatusCondition.Toxic => $"{name} was badly poisoned!",
        StatusCondition.Burn => $"{name} was burned!",
        StatusCondition.Paralyze => $"{name} is paralyzed! It may be unable to move!",
        StatusCondition.Sleep => $"{name} fell asleep!",
        StatusCondition.Freeze => $"{name} was frozen solid!",
        _ => $"{name} is fine."
    };

    /// <summary>"Turtwig is already burned!"</summary>
    public static string AlreadyHas(string name, StatusCondition status) => status switch
    {
        StatusCondition.Poison or StatusCondition.Toxic => $"{name} is already poisoned!",
        StatusCondition.Burn => $"{name} is already burned!",
        StatusCondition.Paralyze => $"{name} is already paralyzed!",
        StatusCondition.Sleep => $"{name} is already asleep!",
        StatusCondition.Freeze => $"{name} is already frozen!",
        _ => "But it failed!"
    };

    /// <summary>"rose!", "sharply rose!", "harshly fell!"...</summary>
    public static string StageChange(int amount) => amount switch
    {
        >= 3 => "rose drastically!",
        2 => "sharply rose!",
        1 => "rose!",
        -1 => "fell!",
        -2 => "harshly fell!",
        _ => "severely fell!"
    };
}
